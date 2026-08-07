using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Balatron.Services;
using Balatron.Services.Live;
using Balatron.Services.Prediction;

namespace Balatron.Models
{
    /// <summary>
    /// Display model for one card in the Live Peek window — either a card
    /// currently in the shop (read from the save) or a predicted upcoming card.
    /// </summary>
    public sealed class PeekCardViewModel
    {
        public string Name { get; private init; }
        /// <summary>Small label above the art in the compact card (e.g. "Next shop").</summary>
        public string Caption { get; private init; }
        public string TypeLabel { get; private init; }
        public string Edition { get; private init; }
        public string Badges { get; private init; }
        public string SubText { get; private init; }
        public object Tooltip { get; private init; }
        /// <summary>2x art, for the full-size card (142x190 box).</summary>
        public IReadOnlyList<ImageSource> SpriteLayers { get; private init; }
        public bool HasSprite => SpriteLayers is { Count: > 0 };

        /// <summary>1x art, for the compact card (71x95 box).</summary>
        public IReadOnlyList<ImageSource> MiniSpriteLayers { get; private init; }
        public bool HasMiniSprite => MiniSpriteLayers is { Count: > 0 };
        /// <summary>True when this card has a predictable random outcome worth hovering for.</summary>
        public bool HasOutcome { get; private init; }
        /// <summary>Drawn crossed out — this card gets destroyed by the effect.</summary>
        public bool IsDestroyed { get; private set; }
        public string FaceText { get; private init; }
        public string OverlayText { get; private init; }
        public Brush OverlayForeground { get; private init; }
        public Brush Accent { get; private init; }
        public Brush FaceBackground { get; private init; }
        public Brush FaceForeground { get; private init; }

        private static Brush Freeze(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        /// <summary>Shared rarity accent brushes (1 common … 4 legendary).</summary>
        public static Brush RarityAccent(int rarity) => rarity switch
        {
            2 => UncommonBrush,
            3 => RareBrush,
            4 => LegendaryBrush,
            _ => CommonBrush
        };

        /// <summary>Accent for a non-joker card set ("Tarot", "Planet", "Spectral", "Voucher", playing card).</summary>
        public static Brush SetAccent(string set) => set switch
        {
            "Tarot" => TarotBrush,
            "Planet" => PlanetBrush,
            "Spectral" => SpectralBrush,
            "Voucher" => VoucherBrush,
            "Playing Card" => CardFaceBrush,
            _ => PanelBrush
        };

        public static Brush SuitForeground(bool isRed) => isRed ? RedSuit : DarkText;

        public static string RarityDisplayName(int rarity) => rarity switch
        {
            2 => "Uncommon",
            3 => "Rare",
            4 => "Legendary",
            _ => "Common"
        };

        private static readonly Brush CommonBrush = Freeze("#2D6BFF");
        private static readonly Brush UncommonBrush = Freeze("#32C24D");
        private static readonly Brush RareBrush = Freeze("#E84C3D");
        private static readonly Brush LegendaryBrush = Freeze("#AB5BB5");
        private static readonly Brush TarotBrush = Freeze("#A782D1");
        private static readonly Brush PlanetBrush = Freeze("#12A0CE");
        private static readonly Brush SpectralBrush = Freeze("#4584FA");
        private static readonly Brush VoucherBrush = Freeze("#E8A33D");
        private static readonly Brush TagBrush = Freeze("#5BC0BE");
        private static readonly Brush CardFaceBrush = Freeze("#F5F6FA");
        private static readonly Brush DarkText = Freeze("#1B1A22");
        private static readonly Brush RedSuit = Freeze("#C6273A");
        private static readonly Brush PanelBrush = Freeze("#3A3846");
        private static readonly Brush WhiteText = Freeze("#F5F6FA");

        private static List<string> BuildBadges(PredictedCard card)
        {
            var badges = new List<string>();
            if (card.Edition != null) badges.Add(card.Edition);
            if (card.Enhancement != null) badges.Add(card.Enhancement);
            if (card.Seal != null) badges.Add(card.Seal);
            if (card.Eternal) badges.Add("Eternal");
            if (card.Perishable) badges.Add("Perishable");
            if (card.Rental) badges.Add("Rental");
            if (card.Note != null) badges.Add(card.Note);
            return badges;
        }

        public static PeekCardViewModel FromPrediction(PredictedCard card)
        {
            var badges = BuildBadges(card);
            return card.Kind switch
            {
                PredictedKind.Joker => Joker(card, string.Join(" · ", badges)),
                PredictedKind.PlayingCard => PlayingCard(card, badges),
                PredictedKind.Voucher => FromVoucher(card.CenterKey, null),
                _ => Consumable(card, badges)
            };
        }

        private static PeekCardViewModel Destroyed(PredictedCard card)
        {
            var vm = FromPrediction(card);
            vm.IsDestroyed = true;
            return vm;
        }

        private static string NormalizeEdition(string edition) =>
            string.IsNullOrEmpty(edition) || edition == "None" ? null : edition;

        public static PeekCardViewModel FromShopCard(ShopCardInfo info,
            string outcomeText = null, IReadOnlyList<PredictedCard> outcomeCards = null,
            IReadOnlyList<PredictedCard> destroyedCards = null)
        {
            if (info.CenterKey != null && info.CenterKey.StartsWith("j_"))
            {
                BalatroItems.JokersByKey.TryGetValue(info.CenterKey, out var def);
                var card = new PredictedCard
                {
                    Kind = PredictedKind.Joker,
                    CenterKey = info.CenterKey,
                    Name = info.Label ?? def?.Name ?? info.CenterKey,
                    Text = def?.Text,
                    Rarity = def?.Rarity ?? 1,
                    Edition = NormalizeEdition(info.Edition),
                    Eternal = info.Eternal,
                    Perishable = info.Perishable,
                    Rental = info.Rental
                };
                return Joker(card, string.Join(" · ", BuildBadges(card)), $"${info.Cost}");
            }

            if (info.CenterKey != null && BalatroItems.ConsumablesByKey.TryGetValue(info.CenterKey, out var consumable))
            {
                var kind = BalatroItems.Tarots.Any(t => t.Key == info.CenterKey) ? PredictedKind.Tarot
                    : BalatroItems.Planets.Any(p => p.Key == info.CenterKey) ? PredictedKind.Planet
                    : PredictedKind.Spectral;
                var card = new PredictedCard
                {
                    Kind = kind,
                    CenterKey = consumable.Key,
                    Name = consumable.Name,
                    Text = consumable.Text,
                    Edition = NormalizeEdition(info.Edition),
                    OutcomeText = outcomeText,
                    OutcomeCards = outcomeCards,
                    DestroyedCards = destroyedCards
                };
                return Consumable(card, BuildBadges(card), $"${info.Cost}");
            }

            return new PeekCardViewModel
            {
                Name = info.Label ?? info.CenterKey ?? "?",
                TypeLabel = $"${info.Cost}",
                Badges = NormalizeEdition(info.Edition) ?? string.Empty,
                SubText = string.Empty,
                Tooltip = info.CenterKey,
                FaceText = "?",
                Accent = PanelBrush,
                FaceBackground = PanelBrush,
                FaceForeground = WhiteText
            };
        }

        /// <summary>A tag attached to one of this ante's blinds.</summary>
        public static PeekCardViewModel FromTag(string tagKey, string caption,
            string outcomeText, IReadOnlyList<PredictedCard> outcomeCards)
        {
            BalatroItems.TagsByKey.TryGetValue(tagKey ?? string.Empty, out var def);
            var name = def?.Name ?? tagKey ?? "Tag";
            var sprite = CardSpriteService.GetTagSprite(tagKey, 1);
            var tooltip = new PeekTooltipViewModel
            {
                Title = name,
                Subtitle = "Tag",
                Body = def?.Text,
                OutcomeText = outcomeText,
                OutcomeCards = outcomeCards?.Select(FromPrediction).ToList()
            };

            return new PeekCardViewModel
            {
                Name = name,
                Caption = caption,
                TypeLabel = "Tag",
                Badges = string.Empty,
                SubText = string.Empty,
                Tooltip = tooltip,
                HasOutcome = tooltip.HasOutcome,
                MiniSpriteLayers = sprite != null ? new[] { sprite } : null,
                SpriteLayers = sprite != null ? new[] { sprite } : null,
                FaceText = "◆",
                Accent = TagBrush,
                FaceBackground = TagBrush,
                FaceForeground = WhiteText
            };
        }

        /// <summary>A voucher on offer now or predicted for a later shop.</summary>
        public static PeekCardViewModel FromVoucher(string centerKey, string caption)
        {
            BalatroItems.VouchersByKey.TryGetValue(centerKey ?? string.Empty, out var def);
            var name = def?.Name ?? centerKey ?? "Voucher";
            var sprite = CardSpriteService.GetVoucherSprite(centerKey, 2);
            var miniSprite = CardSpriteService.GetVoucherSprite(centerKey, 1);

            return new PeekCardViewModel
            {
                Name = name,
                Caption = caption,
                TypeLabel = "Voucher",
                Badges = string.Empty,
                SubText = string.Empty,
                Tooltip = new PeekTooltipViewModel
                {
                    Title = name,
                    Subtitle = "Voucher",
                    Body = def?.Text
                },
                SpriteLayers = sprite != null ? new[] { sprite } : null,
                MiniSpriteLayers = miniSprite != null ? new[] { miniSprite } : null,
                FaceText = "V",
                Accent = VoucherBrush,
                FaceBackground = VoucherBrush,
                FaceForeground = WhiteText
            };
        }

        /// <summary>A joker you already own, with what its random effect would do next.</summary>
        public static PeekCardViewModel FromOwnedJoker(OwnedJokerInfo info,
            string outcomeText, IReadOnlyList<PredictedCard> outcomeCards,
            IReadOnlyList<PredictedCard> destroyedCards = null)
        {
            BalatroItems.JokersByKey.TryGetValue(info.CenterKey ?? string.Empty, out var def);
            var card = new PredictedCard
            {
                Kind = PredictedKind.Joker,
                CenterKey = info.CenterKey,
                Name = info.Label ?? def?.Name ?? info.CenterKey,
                Text = def?.Text,
                Rarity = def?.Rarity ?? 1,
                Edition = NormalizeEdition(info.Edition),
                Eternal = info.Eternal,
                OutcomeText = outcomeText,
                OutcomeCards = outcomeCards,
                DestroyedCards = destroyedCards
            };
            return Joker(card, string.Join(" · ", BuildBadges(card)));
        }

        /// <summary>A consumable you already own, with what using it now would do.</summary>
        public static PeekCardViewModel FromOwnedConsumable(OwnedConsumableInfo info,
            string outcomeText, IReadOnlyList<PredictedCard> outcomeCards,
            IReadOnlyList<PredictedCard> destroyedCards = null)
        {
            BalatroItems.ConsumablesByKey.TryGetValue(info.CenterKey ?? string.Empty, out var def);
            var kind = BalatroItems.Tarots.Any(t => t.Key == info.CenterKey) ? PredictedKind.Tarot
                : BalatroItems.Planets.Any(p => p.Key == info.CenterKey) ? PredictedKind.Planet
                : PredictedKind.Spectral;
            var card = new PredictedCard
            {
                Kind = kind,
                CenterKey = info.CenterKey,
                Name = def?.Name ?? info.Label ?? info.CenterKey,
                Text = def?.Text,
                OutcomeText = outcomeText,
                OutcomeCards = outcomeCards,
                DestroyedCards = destroyedCards
            };
            return Consumable(card, BuildBadges(card));
        }

        private static PeekCardViewModel Joker(PredictedCard card, string badges, string costLabel = null)
        {
            var accent = card.Rarity switch
            {
                2 => UncommonBrush,
                3 => RareBrush,
                4 => LegendaryBrush,
                _ => CommonBrush
            };
            var rarityName = card.Rarity switch { 2 => "Uncommon", 3 => "Rare", 4 => "Legendary", _ => "Common" };

            var layers = new List<ImageSource>(JokerSpriteService.GetSpriteLayers(card.CenterKey, 2));
            layers.AddRange(CardSpriteService.GetStickerSprites(card.Eternal, card.Perishable, card.Rental, 2));
            var miniLayers = new List<ImageSource>(JokerSpriteService.GetSpriteLayers(card.CenterKey, 1));
            miniLayers.AddRange(CardSpriteService.GetStickerSprites(card.Eternal, card.Perishable, card.Rental, 1));

            var tooltip = new PeekTooltipViewModel
            {
                Title = card.Name,
                Subtitle = string.IsNullOrEmpty(badges) ? $"{rarityName} Joker" : $"{rarityName} Joker · {badges}",
                Body = card.Text,
                OutcomeText = card.OutcomeText,
                OutcomeCards = card.OutcomeCards?.Select(FromPrediction).ToList(),
                DestroyedCards = card.DestroyedCards?.Select(Destroyed).ToList()
            };

            return new PeekCardViewModel
            {
                Name = card.Name,
                TypeLabel = costLabel != null ? $"{rarityName} · {costLabel}" : $"{rarityName} Joker",
                Edition = card.Edition,
                Badges = badges,
                SubText = string.Empty,
                Tooltip = tooltip,
                HasOutcome = tooltip.HasOutcome,
                SpriteLayers = layers,
                MiniSpriteLayers = miniLayers,
                FaceText = card.Name.Length > 0 ? card.Name.Substring(0, 1) : "J",
                Accent = accent,
                FaceBackground = PanelBrush,
                FaceForeground = WhiteText
            };
        }

        private static PeekCardViewModel Consumable(PredictedCard card, IReadOnlyList<string> badges, string costLabel = null)
        {
            var (accent, typeName, glyph) = card.Kind switch
            {
                PredictedKind.Tarot => (TarotBrush, "Tarot", "✦"),
                PredictedKind.Planet => (PlanetBrush, "Planet", "●"),
                _ => (SpectralBrush, "Spectral", "◈")
            };

            var sprite = CardSpriteService.GetConsumableSprite(card.CenterKey, 2);
            var miniSprite = CardSpriteService.GetConsumableSprite(card.CenterKey, 1);

            var tooltip = new PeekTooltipViewModel
            {
                Title = card.Name,
                Subtitle = typeName,
                Body = card.Text,
                OutcomeText = card.OutcomeText,
                OutcomeCards = card.OutcomeCards?.Select(FromPrediction).ToList(),
                DestroyedCards = card.DestroyedCards?.Select(Destroyed).ToList()
            };

            return new PeekCardViewModel
            {
                Name = card.Name,
                TypeLabel = costLabel != null ? $"{typeName} · {costLabel}" : typeName,
                Edition = card.Edition,
                Badges = string.Join(" · ", badges),
                SubText = sprite == null ? card.Text ?? string.Empty : string.Empty,
                Tooltip = tooltip,
                HasOutcome = tooltip.HasOutcome,
                SpriteLayers = sprite != null ? new[] { sprite } : null,
                MiniSpriteLayers = miniSprite != null ? new[] { miniSprite } : null,
                FaceText = glyph,
                Accent = accent,
                FaceBackground = accent,
                FaceForeground = WhiteText
            };
        }

        private static PeekCardViewModel PlayingCard(PredictedCard card, IReadOnlyList<string> badges)
        {
            var suitChar = card.CenterKey != null && card.CenterKey.Length > 0 ? card.CenterKey[0] : '?';
            var glyph = suitChar switch { 'H' => "♥", 'D' => "♦", 'C' => "♣", 'S' => "♠", _ => "?" };
            var rank = card.CenterKey != null && card.CenterKey.Length > 2 ? card.CenterKey.Substring(2) : "?";
            if (rank == "T") rank = "10";
            var isRed = suitChar is 'H' or 'D';

            string enhancementKey = null;
            if (card.Enhancement != null && BalatroItems.EnhancementsByName.TryGetValue(card.Enhancement, out var enhDef))
                enhancementKey = enhDef.Key;

            // Enhancement/base art, then the seal stamp, then the rank+suit
            // pips on top. Stone cards show no face.
            var isStone = enhancementKey == "m_stone";
            List<ImageSource> BuildLayers(int scale)
            {
                var built = new List<ImageSource>();
                var baseLayer = CardSpriteService.GetPlayingCardBase(enhancementKey, scale);
                if (baseLayer != null) built.Add(baseLayer);
                var sealLayer = CardSpriteService.GetSealSprite(card.Seal, scale);
                if (sealLayer != null) built.Add(sealLayer);
                if (!isStone)
                {
                    var faceLayer = CardSpriteService.GetPlayingCardFace(card.CenterKey, scale);
                    if (faceLayer != null) built.Add(faceLayer);
                }
                return built;
            }

            var layers = BuildLayers(2);
            var miniLayers = BuildLayers(1);

            var bodyParts = new List<string>();
            if (card.Enhancement != null && BalatroItems.EnhancementsByName.TryGetValue(card.Enhancement, out var e))
                bodyParts.Add($"{e.Name}: {e.Text}");
            if (card.Edition != null) bodyParts.Add(card.Edition);
            if (card.Seal != null) bodyParts.Add(card.Seal);
            if (card.Note != null) bodyParts.Add(card.Note);
            var tooltip = new PeekTooltipViewModel
            {
                Title = card.Name,
                Subtitle = "Playing Card",
                Body = bodyParts.Count > 0 ? string.Join("\n", bodyParts) : null
            };

            // The face sprite carries the pips, so the text overlay is only a
            // fallback for when the deck sheet is unavailable.
            var overlay = isStone || layers.Count > 1 ? string.Empty : $"{rank}{glyph}";

            return new PeekCardViewModel
            {
                Name = card.Name,
                TypeLabel = "Playing Card",
                Edition = card.Edition,
                Badges = string.Join(" · ", badges),
                SubText = string.Empty,
                Tooltip = tooltip,
                SpriteLayers = layers.Count > 0 ? layers : null,
                MiniSpriteLayers = miniLayers.Count > 0 ? miniLayers : null,
                FaceText = $"{rank}{glyph}",
                OverlayText = overlay,
                OverlayForeground = isRed ? RedSuit : DarkText,
                Accent = CardFaceBrush,
                FaceBackground = CardFaceBrush,
                FaceForeground = isRed ? RedSuit : DarkText
            };
        }
    }
}
