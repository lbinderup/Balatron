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
        private const int TileWidth = 142;
        private const int TileHeight = 190;
        private const int Columns = 10;
        private const int Rows = 16;

        private static readonly BitmapImage SpriteSheet = LoadSpriteSheet();

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<(int Column, int Row)>> TileAssignments
            = JokerSpriteTileMap.Assignments;

        public static IReadOnlyList<ImageSource> GetSpriteLayers(string centerKey)
        {
            if (SpriteSheet == null || string.IsNullOrWhiteSpace(centerKey))
                return Array.Empty<ImageSource>();

            var tiles = ResolveTiles(centerKey).ToList();
            var layers = new List<ImageSource>();

            foreach (var tile in tiles)
            {
                var rect = GetTileRectangle(tile);
                if (rect.Width <= 0 || rect.Height <= 0)
                    continue;

                layers.Add(new CroppedBitmap(SpriteSheet, rect));
            }

            return layers;
        }

        private static IEnumerable<int> ResolveTiles(string centerKey)
        {
            if (TileAssignments.TryGetValue(centerKey, out var coordinates))
                return coordinates.Select(coord => coord.Column + (coord.Row * Columns));

            return Enumerable.Empty<int>();
        }

        private static Int32Rect GetTileRectangle(int tileIndex)
        {
            var col = tileIndex % Columns;
            var row = tileIndex / Columns;
            var x = col * TileWidth;
            var y = row * TileHeight;
            return new Int32Rect(x, y, TileWidth, TileHeight);
        }

        private static BitmapImage LoadSpriteSheet()
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri("pack://application:,,,/Balatron;component/Resources/joker_art.png", UriKind.Absolute);
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
