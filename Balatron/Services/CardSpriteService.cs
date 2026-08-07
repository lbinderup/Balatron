using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Balatron.Services
{
    /// <summary>
    /// Sprite lookup for the non-joker art sheets. Every sheet ships as a 1x
    /// and a 2x variant; pass the scale that matches the display box so the
    /// art lands on a whole-pixel ratio (1x for the compact cards, 2x for the
    /// full-size ones).
    /// </summary>
    public static class CardSpriteService
    {
        private sealed class SheetPair
        {
            private readonly SpriteSheet _oneX;
            private readonly SpriteSheet _twoX;

            public SheetPair(string baseName, int columns, int rows)
            {
                _oneX = new SpriteSheet($"{baseName}_art.png", columns, rows);
                _twoX = new SpriteSheet($"{baseName}_2x_art.png", columns, rows);
            }

            public ImageSource Tile(int col, int row, int scale) =>
                (scale >= 2 ? _twoX : _oneX).GetTile(col, row);
        }

        private static readonly SheetPair Tarots = new("tarots", 10, 6);
        private static readonly SheetPair Enhancers = new("enhancers", 7, 5);
        private static readonly SheetPair Stickers = new("stickers", 5, 3);
        private static readonly SheetPair Boosters = new("boosters", 4, 9);
        private static readonly SheetPair Vouchers = new("vouchers", 9, 4);
        private static readonly SheetPair Tags = new("tags", 6, 5);
        private static readonly SheetPair Deck = new("deck", 13, 4);

        /// <summary>Tarot / Planet / Spectral centers → tile in the tarots atlas.</summary>
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

        /// <summary>Tag sprite positions, straight from the game's tag definitions.</summary>
        private static readonly IReadOnlyDictionary<string, (int Col, int Row)> TagTiles =
            new Dictionary<string, (int, int)>(StringComparer.Ordinal)
            {
                ["tag_uncommon"] = (0, 0), ["tag_rare"] = (1, 0), ["tag_negative"] = (2, 0),
                ["tag_foil"] = (3, 0), ["tag_coupon"] = (4, 0), ["tag_double"] = (5, 0),
                ["tag_holo"] = (0, 1), ["tag_polychrome"] = (1, 1), ["tag_investment"] = (2, 1),
                ["tag_voucher"] = (3, 1), ["tag_top_up"] = (4, 1), ["tag_juggle"] = (5, 1),
                ["tag_boss"] = (0, 2), ["tag_standard"] = (1, 2), ["tag_charm"] = (2, 2),
                ["tag_meteor"] = (3, 2), ["tag_buffoon"] = (4, 2), ["tag_orbital"] = (5, 2),
                ["tag_skip"] = (0, 3), ["tag_handy"] = (1, 3), ["tag_garbage"] = (2, 3),
                ["tag_ethereal"] = (3, 3), ["tag_economy"] = (4, 3), ["tag_d_six"] = (5, 3),
            };

        public static ImageSource GetConsumableSprite(string centerKey, int scale = 2) =>
            centerKey != null && ConsumableTiles.TryGetValue(centerKey, out var pos)
                ? Tarots.Tile(pos.Col, pos.Row, scale)
                : null;

        /// <summary>Base layer for a playing card: its enhancement art, or the plain card.</summary>
        public static ImageSource GetPlayingCardBase(string enhancementKey, int scale = 2)
        {
            var key = enhancementKey != null && EnhancementTiles.ContainsKey(enhancementKey)
                ? enhancementKey
                : "c_base";
            var pos = EnhancementTiles[key];
            return Enhancers.Tile(pos.Col, pos.Row, scale);
        }

        /// <summary>
        /// The rank/suit pips for a card key like "H_K", drawn over the base.
        /// Stone cards have no face.
        /// </summary>
        public static ImageSource GetPlayingCardFace(string cardKey, int scale = 2)
        {
            if (string.IsNullOrEmpty(cardKey) || cardKey.Length < 3)
                return null;

            var row = cardKey[0] switch { 'H' => 0, 'C' => 1, 'D' => 2, 'S' => 3, _ => -1 };
            var col = cardKey.Substring(2) switch
            {
                "2" => 0, "3" => 1, "4" => 2, "5" => 3, "6" => 4, "7" => 5, "8" => 6, "9" => 7,
                "T" => 8, "J" => 9, "Q" => 10, "K" => 11, "A" => 12,
                _ => -1
            };
            return row < 0 || col < 0 ? null : Deck.Tile(col, row, scale);
        }

        public static ImageSource GetSealSprite(string sealName, int scale = 2) =>
            sealName != null && SealTiles.TryGetValue(sealName, out var pos)
                ? Enhancers.Tile(pos.Col, pos.Row, scale)
                : null;

        public static IReadOnlyList<ImageSource> GetStickerSprites(bool eternal, bool perishable, bool rental, int scale = 2)
        {
            var layers = new List<ImageSource>();
            Add(Stickers.Tile(0, 0, scale), eternal);
            Add(Stickers.Tile(0, 2, scale), perishable);
            Add(Stickers.Tile(1, 2, scale), rental);
            return layers;

            void Add(ImageSource tile, bool active)
            {
                if (active && tile != null) layers.Add(tile);
            }
        }

        public static ImageSource GetPackSprite(string centerKey, int scale = 2)
        {
            if (centerKey == null)
                return null;
            if (PackTiles.TryGetValue(centerKey, out var pos))
                return Boosters.Tile(pos.Col, pos.Row, scale);

            // Unknown variant suffix: fall back to the first art of the pack kind.
            var def = Prediction.BalatroItems.PackFromCenterKey(centerKey);
            return def != null && PackTiles.TryGetValue(def.KeyPrefix + "_1", out var fallback)
                ? Boosters.Tile(fallback.Col, fallback.Row, scale)
                : null;
        }

        public static ImageSource GetVoucherSprite(string centerKey, int scale = 2) =>
            centerKey != null && VoucherTiles.TryGetValue(centerKey, out var pos)
                ? Vouchers.Tile(pos.Col, pos.Row, scale)
                : null;

        public static ImageSource GetTagSprite(string tagKey, int scale = 2) =>
            tagKey != null && TagTiles.TryGetValue(tagKey, out var pos)
                ? Tags.Tile(pos.Col, pos.Row, scale)
                : null;
    }
}
