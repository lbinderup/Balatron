using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Balatron.Services
{
    public static class JokerSpriteService
    {
        private const int Columns = 10;
        private const int Rows = 16;

        private static readonly SpriteSheet Sheet1x = new("jokers_art.png", Columns, Rows);
        private static readonly SpriteSheet Sheet2x = new("jokers_2x_art.png", Columns, Rows);

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<(int Column, int Row)>> TileAssignments
            = JokerSpriteTileMap.Assignments;

        /// <summary>
        /// Joker art layers for a center key. <paramref name="scale"/> picks the
        /// 1x or 2x sheet — use 1x for the compact cards so the art lands 1:1.
        /// </summary>
        public static IReadOnlyList<ImageSource> GetSpriteLayers(string centerKey, int scale = 2)
        {
            if (string.IsNullOrWhiteSpace(centerKey))
                return Array.Empty<ImageSource>();

            var sheet = scale >= 2 ? Sheet2x : Sheet1x;
            if (!TileAssignments.TryGetValue(centerKey, out var coordinates))
                return Array.Empty<ImageSource>();

            return coordinates
                .Select(coord => sheet.GetTile(coord.Column, coord.Row))
                .Where(tile => tile != null)
                .ToList();
        }
    }

    /// <summary>A uniform grid of sprites, cropped and cached on demand.</summary>
    internal sealed class SpriteSheet
    {
        private readonly BitmapImage _sheet;
        private readonly int _tileWidth;
        private readonly int _tileHeight;
        private readonly Dictionary<(int, int), ImageSource> _cache = new();

        public SpriteSheet(string resourceName, int columns, int rows)
        {
            _sheet = Load(resourceName);
            if (_sheet == null)
                return;
            _tileWidth = _sheet.PixelWidth / columns;
            _tileHeight = _sheet.PixelHeight / rows;
        }

        public ImageSource GetTile(int col, int row)
        {
            if (_sheet == null || _tileWidth <= 0 || _tileHeight <= 0)
                return null;

            if (_cache.TryGetValue((col, row), out var cached))
                return cached;

            var rect = new Int32Rect(col * _tileWidth, row * _tileHeight, _tileWidth, _tileHeight);
            if (rect.X < 0 || rect.Y < 0
                || rect.X + rect.Width > _sheet.PixelWidth
                || rect.Y + rect.Height > _sheet.PixelHeight)
                return null;

            var tile = new CroppedBitmap(_sheet, rect);
            tile.Freeze();
            _cache[(col, row)] = tile;
            return tile;
        }

        private static BitmapImage Load(string resourceName)
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(
                    $"pack://application:,,,/Balatron;component/Resources/{resourceName}", UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}
