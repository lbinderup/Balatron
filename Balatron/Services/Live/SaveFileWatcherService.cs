using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace Balatron.Services.Live
{
    /// <summary>
    /// Watches %AppData%\Balatro\&lt;profile&gt;\save.jkr for changes. Balatro
    /// autosaves after every meaningful action (rerolls, buys, round ends), so
    /// the save file works as a near-realtime feed of game state.
    /// Events are raised on a threadpool thread — marshal to the UI dispatcher.
    /// </summary>
    public sealed class SaveFileWatcherService : IDisposable
    {
        private readonly string _balatroRoot;
        private FileSystemWatcher _watcher;
        private Timer _debounceTimer;
        private string _pendingPath;
        private readonly object _gate = new();
        private readonly Dictionary<string, IReadOnlySet<string>> _profileUnlockCache = new(StringComparer.OrdinalIgnoreCase);

        public event Action<GameStateSnapshot, IReadOnlySet<string>> SnapshotUpdated;
        public event Action<string> StatusChanged;

        public SaveFileWatcherService()
        {
            _balatroRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Balatro");
        }

        public bool BalatroFolderExists => Directory.Exists(_balatroRoot);

        public void Start()
        {
            if (!BalatroFolderExists)
            {
                StatusChanged?.Invoke($"Balatro save folder not found: {_balatroRoot}");
                return;
            }

            _watcher = new FileSystemWatcher(_balatroRoot, "save.jkr")
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
            };
            _watcher.Changed += OnSaveTouched;
            _watcher.Created += OnSaveTouched;
            _watcher.Renamed += (s, e) => ScheduleReload(e.FullPath);
            _watcher.EnableRaisingEvents = true;

            // Initial load: most recently written profile save.
            var latest = FindLatestSave();
            if (latest != null)
                ScheduleReload(latest, immediate: true);
            else
                StatusChanged?.Invoke("Waiting for a Balatro save…");
        }

        private string FindLatestSave()
        {
            try
            {
                return Directory.EnumerateFiles(_balatroRoot, "save.jkr", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private void OnSaveTouched(object sender, FileSystemEventArgs e) => ScheduleReload(e.FullPath);

        private void ScheduleReload(string path, bool immediate = false)
        {
            lock (_gate)
            {
                _pendingPath = path;
                _debounceTimer ??= new Timer(_ => Reload(), null, Timeout.Infinite, Timeout.Infinite);
                // Balatro writes the file in bursts; wait for it to settle.
                _debounceTimer.Change(immediate ? 0 : 350, Timeout.Infinite);
            }
        }

        private void Reload()
        {
            string path;
            lock (_gate)
            {
                path = _pendingPath;
            }

            if (path == null)
                return;

            try
            {
                var text = ReadSaveTextWithRetry(path);
                var root = LuaParser.Parse(text);
                var snapshot = GameStateSnapshot.Parse(root, path);
                var unlocks = LoadProfileUnlocks(Path.GetDirectoryName(path));
                SnapshotUpdated?.Invoke(snapshot, unlocks);
            }
            catch (FileNotFoundException)
            {
                StatusChanged?.Invoke("Save file disappeared (run ended?).");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Could not read save: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads and decompresses a .jkr file while the game may still be
        /// writing it. Retries on sharing violations / truncated streams.
        /// </summary>
        public static string ReadSaveTextWithRetry(string path, int attempts = 6)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete);
                    using var deflate = new DeflateStream(stream, CompressionMode.Decompress);
                    using var reader = new StreamReader(deflate, Encoding.ASCII);
                    var text = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(text))
                        throw new IOException("Empty save file.");
                    return text;
                }
                catch (Exception) when (attempt < attempts)
                {
                    Thread.Sleep(120);
                }
            }
        }

        /// <summary>
        /// Reads the profile's meta.jkr "unlocked" table (achievement-style
        /// unlocks). Returns null when unavailable — callers should then assume
        /// a fully unlocked profile.
        /// </summary>
        private IReadOnlySet<string> LoadProfileUnlocks(string profileDir)
        {
            if (profileDir == null)
                return null;

            if (_profileUnlockCache.TryGetValue(profileDir, out var cached))
                return cached;

            var metaPath = Path.Combine(profileDir, "meta.jkr");
            if (!File.Exists(metaPath))
                return null;

            try
            {
                var root = LuaParser.Parse(ReadSaveTextWithRetry(metaPath));
                var unlockedNode = root.Children.FirstOrDefault(c => c.Key == "unlocked");
                if (unlockedNode == null)
                    return null;

                var set = new HashSet<string>(StringComparer.Ordinal);
                foreach (var child in unlockedNode.Children)
                {
                    if (string.Equals(child.Value?.Trim(), "true", StringComparison.OrdinalIgnoreCase))
                        set.Add(child.Key);
                }

                _profileUnlockCache[profileDir] = set;
                return set;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            lock (_gate)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = null;
            }
        }
    }
}
