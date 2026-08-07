using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Balatron.Models;
using Balatron.Services.Live;
using Balatron.Services.Prediction;

namespace Balatron.Views
{
    public sealed class RerollGroupViewModel
    {
        public string Title { get; init; }
        public IReadOnlyList<PeekCardViewModel> Cards { get; init; }
    }

    public sealed class PackOfferViewModel
    {
        public string Title { get; init; }
        public string ChoiceLabel { get; init; }
        public System.Windows.Media.ImageSource Sprite { get; init; }
        public ICommand PeekCommand { get; init; }
    }


    public partial class LivePeekWindow : Window
    {
        private static LivePeekWindow _instance;

        public static LivePeekWindow Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LivePeekWindow();
                    _instance.Closed += (s, e) => _instance = null;
                }
                return _instance;
            }
        }

        private readonly SaveFileWatcherService _watcher = new();
        private GameStateSnapshot _snapshot;
        private IReadOnlySet<string> _profileUnlocks;
        private int _rerollDepth = 4;
        private PackPeekWindow _packPeek;
        private string _packPeekKind;

        public ObservableCollection<PeekCardViewModel> Owned { get; } = new();
        public ObservableCollection<PeekCardViewModel> Tags { get; } = new();
        public ObservableCollection<PeekCardViewModel> ShopNow { get; } = new();
        public ObservableCollection<PackOfferViewModel> Packs { get; } = new();
        public ObservableCollection<RerollGroupViewModel> Rerolls { get; } = new();
        public ObservableCollection<PeekCardViewModel> VoucherLines { get; } = new();

        private LivePeekWindow()
        {
            InitializeComponent();
            OwnedList.ItemsSource = Owned;
            TagsList.ItemsSource = Tags;
            ShopNowList.ItemsSource = ShopNow;
            PacksList.ItemsSource = Packs;
            RerollsList.ItemsSource = Rerolls;
            VoucherLinesList.ItemsSource = VoucherLines;

            _watcher.SnapshotUpdated += (snapshot, unlocks) =>
                Dispatcher.BeginInvoke(new Action(() => ApplySnapshot(snapshot, unlocks)));
            _watcher.StatusChanged += message =>
                Dispatcher.BeginInvoke(new Action(() => SetStatus(message, healthy: false)));

            Closed += (s, e) => _watcher.Dispose();
            _watcher.Start();
        }

        private void ApplySnapshot(GameStateSnapshot snapshot, IReadOnlySet<string> unlocks)
        {
            _snapshot = snapshot;
            _profileUnlocks = unlocks;

            HeaderText.Text =
                $"Seed {snapshot.Seed}   ·   Ante {snapshot.Ante}   ·   Round {snapshot.Round}   ·   ${snapshot.Dollars:0}   ·   {snapshot.StateName}";

            var profile = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(snapshot.SourcePath));
            SetStatus($"Watching profile {profile} ({snapshot.DeckName}) · updated {snapshot.LoadedAt:HH:mm:ss}", healthy: true);

            RebuildFromSnapshot();
        }

        private void RebuildFromSnapshot()
        {
            if (_snapshot == null)
                return;

            var engine = new PredictionEngine(_snapshot, _profileUnlocks);

            Owned.Clear();
            foreach (var joker in _snapshot.OwnedJokers)
            {
                engine.TryPredictJokerOutcome(joker.CenterKey, out var jokerText, out var jokerCards);
                Owned.Add(PeekCardViewModel.FromOwnedJoker(joker, jokerText, jokerCards));
            }
            foreach (var consumable in _snapshot.OwnedConsumables)
            {
                engine.TryPredictOutcome(consumable.CenterKey, out var useText, out var useCards, out var useDestroyed);
                Owned.Add(PeekCardViewModel.FromOwnedConsumable(consumable, useText, useCards, useDestroyed));
            }
            OwnedEmptyText.Visibility = Owned.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateMinimumWidth();

            // Tags belong to the ante's blinds, so they're worth showing even
            // outside the shop. Defeated blinds have already spent theirs.
            Tags.Clear();
            foreach (var blind in new[] { "Small", "Big" })
            {
                if (!_snapshot.BlindTags.TryGetValue(blind, out var tagKey) || string.IsNullOrEmpty(tagKey))
                    continue;
                if (_snapshot.BlindStates.TryGetValue(blind, out var state) && state == "Defeated")
                    continue;

                engine.TryPredictTagOutcome(tagKey, out var tagText, out var tagCards);
                Tags.Add(PeekCardViewModel.FromTag(tagKey, $"Skip {blind}", tagText, tagCards));
            }
            TagsEmptyText.Visibility = Tags.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            ShopNow.Clear();
            foreach (var card in _snapshot.ShopCards)
            {
                string outcomeText = null;
                System.Collections.Generic.IReadOnlyList<PredictedCard> outcomeCards = null;
                System.Collections.Generic.IReadOnlyList<PredictedCard> destroyedCards = null;
                if (card.CenterKey != null && card.CenterKey.StartsWith("c_"))
                    engine.TryPredictOutcome(card.CenterKey, out outcomeText, out outcomeCards, out destroyedCards);
                ShopNow.Add(PeekCardViewModel.FromShopCard(card, outcomeText, outcomeCards, destroyedCards));
            }
            ShopEmptyText.Visibility = ShopNow.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            RebuildVoucherLines(engine);

            Packs.Clear();
            var offerDefs = _snapshot.PackOffers
                .Select(o => (Offer: o, Def: BalatroItems.PackFromCenterKey(o.CenterKey)))
                .ToList();

            foreach (var (offer, def) in offerDefs)
            {
                var kind = def?.Kind;
                Packs.Add(new PackOfferViewModel
                {
                    Title = offer.Label ?? def?.Name ?? offer.CenterKey,
                    ChoiceLabel = def != null ? $"Choose {def.Choices} of {def.CardCount}" : string.Empty,
                    Sprite = Services.CardSpriteService.GetPackSprite(offer.CenterKey),
                    PeekCommand = new RelayCommand(_ => ShowPackPeek(kind, activate: true), _ => kind != null)
                });
            }
            PacksEmptyText.Visibility = Packs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // An open peek belongs to the previous save state: re-target it at
            // the new one, or close it if that pack is no longer obtainable.
            if (_packPeek != null && _packPeekKind != null)
                ShowPackPeek(_packPeekKind, activate: false);

            var nextPacks = engine.PredictNextShopPacks();
            NextPacksText.Text = nextPacks.Count > 0
                ? $"Next shop's packs: {string.Join(", ", nextPacks.Select(p => p.Name))}"
                : string.Empty;

            Rerolls.Clear();
            foreach (var reroll in engine.PredictRerolls(_rerollDepth))
            {
                Rerolls.Add(new RerollGroupViewModel
                {
                    Title = reroll.Index == 0 ? "Next reroll" : $"Reroll +{reroll.Index + 1}",
                    Cards = reroll.Slots.Select(PeekCardViewModel.FromPrediction).ToList()
                });
            }
        }

        /// <summary>
        /// Shows every currently offered pack of <paramref name="kind"/> in the
        /// single shared popup — one pack plainly, several as their shared card
        /// sequence. Closes the popup when the kind is no longer on offer.
        /// </summary>
        private void ShowPackPeek(string kind, bool activate)
        {
            if (kind == null || _snapshot == null)
                return;

            var offers = _snapshot.PackOffers
                .Select(o => (Offer: o, Def: BalatroItems.PackFromCenterKey(o.CenterKey)))
                .Where(x => x.Def != null && x.Def.Kind == kind)
                .ToList();

            if (offers.Count == 0)
            {
                _packPeek?.Close();
                return;
            }

            var engine = new PredictionEngine(_snapshot, _profileUnlocks);
            var window = GetPackPeekWindow();
            _packPeekKind = kind;

            if (offers.Count == 1)
            {
                var (offer, def) = offers[0];
                var contents = engine.PredictPackContents(offer.CenterKey)
                    .Select(PeekCardViewModel.FromPrediction)
                    .ToList();
                window.ShowPack($"{offer.Label ?? def.Name} · Choose {def.Choices} of {def.CardCount}", contents);
            }
            else
            {
                // Packs of one kind pull from a single shared RNG sequence, so
                // they have to be presented together.
                var sequence = engine.PredictPackSequence(offers.Select(x => x.Def).ToList())
                    .SelectMany(segment => segment)
                    .Select(PeekCardViewModel.FromPrediction)
                    .ToList();

                var headers = offers.Select((x, index) =>
                {
                    // The sequence depends on opening order: the trailing pack
                    // resamples cards the leading one already contains.
                    var orderedDefs = new List<PackDef> { x.Def };
                    orderedDefs.AddRange(offers.Where((_, j) => j != index).Select(y => y.Def));
                    var whenFirst = engine.PredictPackSequence(orderedDefs)
                        .SelectMany(segment => segment)
                        .Select(PeekCardViewModel.FromPrediction)
                        .ToList();

                    return new PackHeaderViewModel
                    {
                        Name = x.Offer.Label ?? x.Def.Name,
                        Sprite = Services.CardSpriteService.GetPackSprite(x.Offer.CenterKey),
                        CardCount = x.Def.CardCount,
                        Label = $"Choose {x.Def.Choices} of {x.Def.CardCount}",
                        SequenceWhenOpenedFirst = whenFirst
                    };
                }).ToList();

                window.ShowSequence($"{kind} Packs", headers, sequence);
            }

            window.Show();
            if (activate)
                window.Activate();
        }

        /// <summary>
        /// One shared pack popup: peeking again retargets it (keeping wherever
        /// the user dragged it) instead of stacking duplicates.
        /// </summary>
        private PackPeekWindow GetPackPeekWindow()
        {
            if (_packPeek == null)
            {
                _packPeek = new PackPeekWindow
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                _packPeek.Closed += (_, _) =>
                {
                    _packPeek = null;
                    _packPeekKind = null;
                };
            }
            return _packPeek;
        }

        private void RebuildVoucherLines(PredictionEngine engine)
        {
            VoucherLines.Clear();

            // The voucher slot only rerolls when the ante does, so it reads per
            // ante rather than per shop. This ante's is in the save (set even
            // while playing a blind); the next comes off the "Voucher"+ante stream.
            var currentKey = !string.IsNullOrEmpty(_snapshot.CurrentRoundVoucher)
                ? _snapshot.CurrentRoundVoucher
                : _snapshot.VoucherCenter;
            if (string.IsNullOrEmpty(currentKey))
                currentKey = engine.PredictShopVoucher(_snapshot.Ante)?.Key;
            if (!string.IsNullOrEmpty(currentKey))
                VoucherLines.Add(VoucherLine("This ante", currentKey));

            var next = engine.PredictShopVoucher(_snapshot.Ante + 1);
            if (next != null)
                VoucherLines.Add(VoucherLine("Next ante", next.Key));

            var hasVouchers = VoucherLines.Count > 0;
            VoucherPanel.Visibility = hasVouchers ? Visibility.Visible : Visibility.Collapsed;
            VouchersHeader.Visibility = hasVouchers ? Visibility.Visible : Visibility.Collapsed;
        }

        private static PeekCardViewModel VoucherLine(string header, string key) =>
            PeekCardViewModel.FromVoucher(key, header);

        private void SetStatus(string message, bool healthy)
        {
            StatusText.Text = message;
            StatusDot.Fill = healthy
                ? (Brush)FindResource("BalatroGreen")
                : (Brush)FindResource("BalatroRed");
        }

        private void MoreRerollsButton_Click(object sender, RoutedEventArgs e)
        {
            _rerollDepth += 3;
            RebuildFromSnapshot();
        }

        private void ResizeThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var direction = (string)((FrameworkElement)sender).Tag;

            if (direction.Contains('L'))
            {
                var width = Width - e.HorizontalChange;
                if (width >= MinWidth)
                {
                    Left += e.HorizontalChange;
                    Width = width;
                }
            }
            else if (direction.Contains('R'))
            {
                Width = Math.Max(MinWidth, Width + e.HorizontalChange);
            }

            if (direction.Contains('T'))
            {
                var height = Height - e.VerticalChange;
                if (height >= MinHeight)
                {
                    Top += e.VerticalChange;
                    Height = height;
                }
            }
            else if (direction.Contains('B'))
            {
                Height = Math.Max(MinHeight, Height + e.VerticalChange);
            }
        }

        /// <summary>
        /// Keep the window at least as wide as the owned jokers + consumables
        /// row needs on a single line, so that row never wraps.
        /// </summary>
        private void UpdateMinimumWidth()
        {
            const double cardWidth = 93;   // 87 card + 3 margin either side
            const double chrome = 74;      // window margin, border, scroll area
            var required = Owned.Count * cardWidth + chrome;

            MinWidth = Math.Max(380, Math.Min(required, SystemParameters.WorkArea.Width));
            if (Width < MinWidth)
                Width = MinWidth;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
                return;
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    }
}
