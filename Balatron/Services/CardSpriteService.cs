using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Balatron.Services
{
    /// <summary>
    /// Sprite lookup for the non-joker art sheets (all 71x95 tile grids):
    /// tarots/planets/spectrals, enhancements/seals, joker stickers,
    /// booster packs and vouchers.
    /// </summary>
    public static class CardSpriteService
    {
        private sealed class Atlas
        {
            private readonly BitmapImage _sheet;
            private readonly int _tileWidth;
            private readonly int _tileHeight;
            private readonly Dictionary<(int Col, int Row), ImageSource> _cache = new();

            public Atlas(string resourceName, int columns, int rows)
            {
                _sheet = LoadSheet(resourceName);
                if (_sheet != null)
                {
                    _tileWidth = _sheet.PixelWidth / columns;
                    _tileHeight = _sheet.PixelHeight / rows;
                }
            }

            public ImageSource GetTile(int col, int row)
            {
                if (_sheet == null)
                    return null;

                if (_cache.TryGetValue((col, row), out var cached))
                    return cached;

                var rect = new Int32Rect(col * _tileWidth, row * _tileHeight, _tileWidth, _tileHeight);
                if (rect.X + rect.Width > _sheet.PixelWidth || rect.Y + rect.Height > _sheet.PixelHeight)
                    return null;

                var tile = new CroppedBitmap(_sheet, rect);
                tile.Freeze();
                _cache[(col, row)] = tile;
                return tile;
            }

            private static BitmapImage LoadSheet(string resourceName)
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri($"pack://application:,,,/Balatron;component/Resources/{resourceName}", UriKind.Absolute);
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

        private static readonly Atlas Tarots = new("tarots_art.png", 10, 6);
        private static readonly Atlas Enhancers = new("enhancers_art.png", 7, 5);
        private static readonly Atlas Stickers = new("stickers_art.png", 5, 3);
        private static readonly Atlas Boosters = new("boosters_art.png", 4, 9);
        private static readonly Atlas Vouchers = new("vouchers_art.png", 9, 4);

        /// <summary>Tarot / Planet / Spectral centers → tile in the tarots atlas (game layout).</summary>
        private static readonly IReadOnlyDictionary<string, (int Col, int Row)> ConsumableTiles =
            new Dictionary<string, (int, int)>(StringComparer.Ordinal)
            {
                ["c_fool"] = (0, 0), ["c_magician"] = (1, 0), ["c_high_priestess"] = (2, 0),
                ["c_empress"] = (3, 0), ["c_emperor"] = (4, 0), ["c_heirophant"] = (5, 0),
                ["c_lovers"] = (6, 0), ["c_chariot"] = (7, 0), ["c_justice"] = (8, 0), ["c_hermit"] = (9, 0),
                ["c_wheel_of_fortune"] = (0, 1), ["c_strength"] = (1, 1), ["c_hanged_man"] = (2, 1),
                ["c_death"] = (3, 1), ["c_temperance"] = (4, 1), ["c_devil"] = (5, 1),
                ["c_tower"] = (6, 1), ["c_star"] = (7, 1), ["c_moon"] = (8, 1), ["c_sun"] = (9, 1),
                ["c_judgement"] = (0, 2), ["c_world"] = (1, 2), ["c_soul"] = (2, 2),
                ["c_eris"] = (3, 2), ["c_ceres"] = (8, 2), ["c_planet_x"] = (9, 2),
                ["c_mercury"] = (0, 3), ["c_venus"] = (1, 3), ["c_earth"] = (2, 3), ["c_mars"] = (3, 3),
                ["c_jupiter"] = (4, 3), ["c_saturn"] = (5, 3), ["c_uranus"] = (6, 3),
                ["c_neptune"] = (7, 3), ["c_pluto"] = (8, 3), ["c_black_hole"] = (9, 3),
                ["c_familiar"] = (0, 4), ["c_grim"] = (1, 4), ["c_incantation"] = (2, 4),
                ["c_talisman"] = (3, 4), ["c_aura"] = (4, 4), ["c_wraith"] = (5, 4),
                ["c_sigil"] = (6, 4), ["c_ouija"] = (7, 4), ["c_ectoplasm"] = (8, 4), ["c_immolate"] = (9, 4),
                ["c_ankh"] = (0, 5), ["c_deja_vu"] = (1, 5), ["c_hex"] = (2, 5),
                ["c_trance"] = (3, 5), ["c_medium"] = (4, 5), ["c_cryptid"] = (5, 5),
            };

        /// <summary>Playing-card bases (enhancements + plain card) in the enhancers atlas.</summary>
        private static readonly IReadOnlyDictionary<string, (int Col, int Row)> EnhancementTiles =
            new Dictionary<string, (int, int)>(StringComparer.Ordinal)
            {
                ["c_base"] = (1, 0),
                ["m_stone"] = (5, 0), ["m_gold"] = (6, 0),
                ["m_bonus"] = (1, 1), ["m_mult"] = (2, 1), ["m_wild"] = (3, 1),
                ["m_lucky"] = (4, 1), ["m_glass"] = (5, 1), ["m_steel"] = (6, 1),
            };

        private static readonly IReadOnlyDictionary<string, (int Col, int Row)> SealTiles =
            new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase)
            {
                ["Gold Seal"] = (2, 0),
                ["Purple Seal"] = (4, 4),
                ["Red Seal"] = (5, 4),
                ["Blue Seal"] = (6, 4),
            };

        private static readonly IReadOnlyDictionary<string, (int Col, int Row)> PackTiles =
            new Dictionary<string, (int, int)>(StringComparer.Ordinal)
            {
                ["p_arcana_normal_1"] = (0, 0), ["p_arcana_normal_2"] = (1, 0),
                ["p_arcana_normal_3"] = (2, 0), ["p_arcana_normal_4"] = (3, 0),
                ["p_celestial_normal_1"] = (0, 1), ["p_celestial_normal_2"] = (1, 1),
                ["p_celestial_normal_3"] = (2, 1), ["p_celestial_normal_4"] = (3, 1),
                ["p_arcana_jumbo_1"] = (0, 2), ["p_arcana_jumbo_2"] = (1, 2),
                ["p_arcana_mega_1"] = (2, 2), ["p_arcana_mega_2"] = (3, 2),
                ["p_celestial_jumbo_1"] = (0, 3), ["p_celestial_jumbo_2"] = (1, 3),
                ["p_celestial_mega_1"] = (2, 3), ["p_celestial_mega_2"] = (3, 3),
                ["p_spectral_normal_1"] = (0, 4), ["p_spectral_normal_2"] = (1, 4),
                ["p_spectral_jumbo_1"] = (2, 4), ["p_spectral_mega_1"] = (3, 4),
                ["p_standard_normal_1"] = (0, 6), ["p_standard_normal_2"] = (1, 6),
                ["p_standard_normal_3"] = (2, 6), ["p_standard_normal_4"] = (3, 6),
                ["p_standard_jumbo_1"] = (0, 7), ["p_standard_jumbo_2"] = (1, 7),
                ["p_standard_mega_1"] = (2, 7), ["p_standard_mega_2"] = (3, 7),
                ["p_buffoon_normal_1"] = (0, 8), ["p_buffoon_normal_2"] = (1, 8),
                ["p_buffoon_jumbo_1"] = (2, 8), ["p_buffoon_mega_1"] = (3, 8),
            };

        /// <summary>Vouchers: base tiers on rows 0/2, their upgrades directly below on rows 1/3.</summary>
        private static readonly IReadOnlyDictionary<string, (int Col, int Row)> VoucherTiles =
            new Dictionary<string, (int, int)>(StringComparer.Ordinal)
            {
                ["v_overstock_norm"] = (0, 0), ["v_tarot_merchant"] = (1, 0), ["v_planet_merchant"] = (2, 0),
                ["v_clearance_sale"] = (3, 0), ["v_hone"] = (4, 0), ["v_grabber"] = (5, 0),
                ["v_wasteful"] = (6, 0), ["v_blank"] = (7, 0),
                ["v_overstock_plus"] = (0, 1), ["v_tarot_tycoon"] = (1, 1), ["v_planet_tycoon"] = (2, 1),
                ["v_liquidation"] = (3, 1), ["v_glow_up"] = (4, 1), ["v_nacho_tong"] = (5, 1),
                ["v_recyclomancy"] = (6, 1), ["v_antimatter"] = (7, 1),
                ["v_reroll_surplus"] = (0, 2), ["v_seed_money"] = (1, 2), ["v_crystal_ball"] = (2, 2),
                ["v_telescope"] = (3, 2), ["v_magic_trick"] = (4, 2), ["v_hieroglyph"] = (5, 2),
                ["v_directors_cut"] = (6, 2), ["v_paint_brush"] = (7, 2),
                ["v_reroll_glut"] = (0, 3), ["v_money_tree"] = (1, 3), ["v_omen_globe"] = (2, 3),
                ["v_observatory"] = (3, 3), ["v_illusion"] = (4, 3), ["v_petroglyph"] = (5, 3),
                ["v_retcon"] = (6, 3), ["v_palette"] = (7, 3),
            };

        public static ImageSource GetConsumableSprite(string centerKey) =>
            centerKey != null && ConsumableTiles.TryGetValue(centerKey, out var pos)
                ? Tarots.GetTile(pos.Col, pos.Row)
                : null;

        /// <summary>Base layer for a playing card: its enhancement art, or the plain card.</summary>
        public static ImageSource GetPlayingCardBase(string enhancementKey)
        {
            var key = enhancementKey != null && EnhancementTiles.ContainsKey(enhancementKey)
                ? enhancementKey
                : "c_base";
            var pos = EnhancementTiles[key];
            return Enhancers.GetTile(pos.Col, pos.Row);
        }

        public static ImageSource GetSealSprite(string sealName) =>
            sealName != null && SealTiles.TryGetValue(sealName, out var pos)
                ? Enhancers.GetTile(pos.Col, pos.Row)
                : null;

        public static IReadOnlyList<ImageSource> GetStickerSprites(bool eternal, bool perishable, bool rental)
        {
            var layers = new List<ImageSource>();
            if (eternal) Add(layers, Stickers.GetTile(0, 0));
            if (perishable) Add(layers, Stickers.GetTile(0, 2));
            if (rental) Add(layers, Stickers.GetTile(1, 2));
            return layers;

            static void Add(List<ImageSource> list, ImageSource tile)
            {
                if (tile != null) list.Add(tile);
            }
        }

        public static ImageSource GetPackSprite(string centerKey)
        {
            if (centerKey == null)
                return null;
            if (PackTiles.TryGetValue(centerKey, out var pos))
                return Boosters.GetTile(pos.Col, pos.Row);

            // Unknown variant suffix: fall back to the first art of the pack kind.
            var def = Prediction.BalatroItems.PackFromCenterKey(centerKey);
            return def != null && PackTiles.TryGetValue(def.KeyPrefix + "_1", out var fallback)
                ? Boosters.GetTile(fallback.Col, fallback.Row)
                : null;
        }

        public static ImageSource GetVoucherSprite(string centerKey) =>
            centerKey != null && VoucherTiles.TryGetValue(centerKey, out var pos)
                ? Vouchers.GetTile(pos.Col, pos.Row)
                : null;
    }
}
