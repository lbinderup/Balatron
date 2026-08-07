using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Balatron.Models;

namespace Balatron.Services.Live
{
    public sealed class ShopCardInfo
    {
        public string CenterKey { get; init; }
        public string Label { get; init; }
        public string Edition { get; init; }
        public int Cost { get; init; }
        public bool Eternal { get; init; }
        public bool Perishable { get; init; }
        public bool Rental { get; init; }
    }

    public sealed class PackOfferInfo
    {
        public string CenterKey { get; init; }
        public string Label { get; init; }
    }

    public sealed class OwnedJokerInfo
    {
        public string CenterKey { get; init; }
        public string Label { get; init; }
        public string Edition { get; init; }
    }

    /// <summary>
    /// Everything the predictor needs, extracted from one parsed save.jkr.
    /// </summary>
    public sealed class GameStateSnapshot
    {
        public string SourcePath { get; init; }
        public DateTime LoadedAt { get; init; }

        public string Seed { get; init; }
        public int State { get; init; }
        public int Ante { get; init; }
        public int Round { get; init; }
        public double Dollars { get; init; }
        public string DeckName { get; init; }
        public int ShopSlots { get; init; }

        public double JokerRate { get; init; }
        public double TarotRate { get; init; }
        public double PlanetRate { get; init; }
        public double PlayingCardRate { get; init; }
        public double SpectralRate { get; init; }
        public double EditionRate { get; init; }

        public IReadOnlyDictionary<string, double> RngCounters { get; init; }
        public IReadOnlySet<string> UsedJokers { get; init; }
        public IReadOnlySet<string> UsedVouchers { get; init; }
        public IReadOnlySet<string> BannedKeys { get; init; }
        public IReadOnlySet<string> PoolFlags { get; init; }
        public IReadOnlyDictionary<string, bool> HandVisible { get; init; }
        public IReadOnlySet<string> DeckEnhancements { get; init; }
        public IReadOnlyDictionary<string, string> BlindStates { get; init; }
        public IReadOnlyDictionary<string, string> BlindTags { get; init; }

        public bool ShowmanOwned { get; init; }
        public bool EternalsInShop { get; init; }
        public bool PerishablesInShop { get; init; }
        public bool RentalsInShop { get; init; }
        public bool FirstShopBuffoonDone { get; init; }

        public IReadOnlyList<OwnedJokerInfo> OwnedJokers { get; init; }
        public int ConsumableSlots { get; init; }
        public int OwnedConsumableCount { get; init; }
        public string LastTarotPlanet { get; init; }
        public IReadOnlyList<ShopCardInfo> ShopCards { get; init; }
        public IReadOnlyList<PackOfferInfo> PackOffers { get; init; }
        public string VoucherOffer { get; init; }
        public string VoucherCenter { get; init; }

        /// <summary>GAME.current_round.voucher — this round's shop voucher; set even while outside the shop.</summary>
        public string CurrentRoundVoucher { get; init; }

        public bool InShop => State == 5;

        public string StateName => State switch
        {
            1 => "Selecting Hand",
            2 => "Hand Played",
            3 => "Drawing",
            4 => "Game Over",
            5 => "Shop",
            7 => "Blind Select",
            8 => "Round Eval",
            9 => "Arcana Pack",
            10 => "Celestial Pack",
            11 => "Menu",
            15 => "Spectral Pack",
            17 => "Standard Pack",
            18 => "Buffoon Pack",
            19 => "New Round",
            _ => $"State {State}"
        };

        public static GameStateSnapshot Parse(LuaNode root, string sourcePath)
        {
            var game = Child(root, "GAME");
            if (game == null)
                throw new FormatException("Save file has no GAME table.");

            var pseudo = Child(game, "pseudorandom");
            var counters = new Dictionary<string, double>(StringComparer.Ordinal);
            var seed = string.Empty;
            if (pseudo != null)
            {
                foreach (var child in pseudo.Children)
                {
                    if (child.Key == "seed")
                        seed = StringValue(child.Value);
                    else if (child.Key != "hashed_seed" && TryNumber(child.Value, out var num))
                        counters[child.Key] = num;
                }
            }

            var cardAreas = Child(root, "cardAreas");

            var ownedJokers = new List<OwnedJokerInfo>();
            var jokerCards = Child(Child(cardAreas, "jokers"), "cards");
            if (jokerCards != null)
            {
                foreach (var card in OrderedChildren(jokerCards))
                {
                    ownedJokers.Add(new OwnedJokerInfo
                    {
                        CenterKey = CenterKey(card),
                        Label = StringValue(Child(card, "label")?.Value),
                        Edition = Views.LuaNodeTreeWindow.GetEditionType(card)
                    });
                }
            }

            var consumablesArea = Child(cardAreas, "consumeables");
            var ownedConsumableCount = Child(consumablesArea, "cards")?.Children.Count ?? 0;
            var consumableSlots = (int)Number(Child(Child(consumablesArea, "config"), "card_limit")?.Value, 2);

            var shopCards = new List<ShopCardInfo>();
            var shopCardsNode = Child(Child(cardAreas, "shop_jokers"), "cards");
            if (shopCardsNode != null)
            {
                foreach (var card in OrderedChildren(shopCardsNode))
                {
                    var ability = Child(card, "ability");
                    shopCards.Add(new ShopCardInfo
                    {
                        CenterKey = CenterKey(card),
                        Label = StringValue(Child(card, "label")?.Value),
                        Edition = Views.LuaNodeTreeWindow.GetEditionType(card),
                        Cost = (int)Number(Child(card, "cost")?.Value),
                        Eternal = BoolValue(Child(ability, "eternal")?.Value),
                        Perishable = BoolValue(Child(ability, "perishable")?.Value),
                        Rental = BoolValue(Child(ability, "rental")?.Value)
                    });
                }
            }

            var packs = new List<PackOfferInfo>();
            var boosterCards = Child(Child(cardAreas, "shop_booster"), "cards");
            if (boosterCards != null)
            {
                foreach (var card in OrderedChildren(boosterCards))
                {
                    packs.Add(new PackOfferInfo
                    {
                        CenterKey = CenterKey(card),
                        Label = StringValue(Child(card, "label")?.Value)
                    });
                }
            }

            var voucherCards = Child(Child(cardAreas, "shop_vouchers"), "cards");
            var firstVoucher = voucherCards?.Children.Count > 0 ? voucherCards.Children[0] : null;
            var voucherOffer = firstVoucher != null ? StringValue(Child(firstVoucher, "label")?.Value) : null;
            var voucherCenter = firstVoucher != null ? CenterKey(firstVoucher) : null;

            var deckEnhancements = new HashSet<string>(StringComparer.Ordinal);
            foreach (var areaName in new[] { "deck", "hand", "discard", "play" })
            {
                foreach (var key in CenterKeys(Child(cardAreas, areaName)))
                {
                    if (key != null && key.StartsWith("m_", StringComparison.Ordinal))
                        deckEnhancements.Add(key);
                }
            }

            var hands = Child(game, "hands");
            var handVisible = new Dictionary<string, bool>(StringComparer.Ordinal);
            if (hands != null)
            {
                foreach (var hand in hands.Children)
                    handVisible[hand.Key] = BoolValue(Child(hand, "visible")?.Value);
            }

            var modifiers = Child(game, "modifiers");
            var roundResets = Child(game, "round_resets");

            var blindStates = new Dictionary<string, string>(StringComparer.Ordinal);
            var statesNode = Child(roundResets, "blind_states");
            if (statesNode != null)
            {
                foreach (var blind in statesNode.Children)
                    blindStates[blind.Key] = StringValue(blind.Value);
            }

            var blindTags = new Dictionary<string, string>(StringComparer.Ordinal);
            var tagsNode = Child(roundResets, "blind_tags");
            if (tagsNode != null)
            {
                foreach (var blind in tagsNode.Children)
                    blindTags[blind.Key] = StringValue(blind.Value);
            }

            return new GameStateSnapshot
            {
                SourcePath = sourcePath,
                LoadedAt = DateTime.Now,
                Seed = seed,
                State = (int)Number(Child(root, "STATE")?.Value),
                Ante = (int)Number(Child(roundResets, "ante")?.Value, 1),
                Round = (int)Number(Child(game, "round")?.Value),
                Dollars = Number(Child(game, "dollars")?.Value),
                DeckName = StringValue(Child(Child(root, "BACK"), "name")?.Value),
                ShopSlots = (int)Number(Child(Child(game, "shop"), "joker_max")?.Value, 2),
                JokerRate = Number(Child(game, "joker_rate")?.Value, 20),
                TarotRate = Number(Child(game, "tarot_rate")?.Value, 4),
                PlanetRate = Number(Child(game, "planet_rate")?.Value, 4),
                PlayingCardRate = Number(Child(game, "playing_card_rate")?.Value),
                SpectralRate = Number(Child(game, "spectral_rate")?.Value),
                EditionRate = Number(Child(game, "edition_rate")?.Value, 1),
                RngCounters = counters,
                UsedJokers = KeySet(Child(game, "used_jokers")),
                UsedVouchers = KeySet(Child(game, "used_vouchers")),
                BannedKeys = KeySet(Child(game, "banned_keys")),
                PoolFlags = KeySet(Child(game, "pool_flags")),
                HandVisible = handVisible,
                DeckEnhancements = deckEnhancements,
                BlindStates = blindStates,
                BlindTags = blindTags,
                ShowmanOwned = ownedJokers.Any(j => j.CenterKey == "j_ring_master"),
                EternalsInShop = BoolValue(Child(modifiers, "enable_eternals_in_shop")?.Value),
                PerishablesInShop = BoolValue(Child(modifiers, "enable_perishables_in_shop")?.Value),
                RentalsInShop = BoolValue(Child(modifiers, "enable_rentals_in_shop")?.Value),
                FirstShopBuffoonDone = BoolValue(Child(game, "first_shop_buffoon")?.Value),
                OwnedJokers = ownedJokers,
                ConsumableSlots = consumableSlots,
                OwnedConsumableCount = ownedConsumableCount,
                LastTarotPlanet = StringValue(Child(game, "last_tarot_planet")?.Value),
                ShopCards = shopCards,
                PackOffers = packs,
                VoucherOffer = voucherOffer,
                VoucherCenter = voucherCenter,
                CurrentRoundVoucher = StringValue(Child(Child(game, "current_round"), "voucher")?.Value)
            };
        }

        private static LuaNode Child(LuaNode node, string key) =>
            node?.Children.FirstOrDefault(c => c.Key == key);

        private static IEnumerable<LuaNode> OrderedChildren(LuaNode node) =>
            node.Children.OrderBy(c => int.TryParse(c.Key, out var i) ? i : int.MaxValue);

        private static string CenterKey(LuaNode cardNode) =>
            StringValue(Child(Child(cardNode, "save_fields"), "center")?.Value);

        private static IEnumerable<string> CenterKeys(LuaNode areaNode)
        {
            var cards = Child(areaNode, "cards");
            if (cards == null)
                yield break;
            foreach (var card in OrderedChildren(cards))
            {
                var key = CenterKey(card);
                if (!string.IsNullOrEmpty(key))
                    yield return key;
            }
        }

        private static IReadOnlySet<string> KeySet(LuaNode tableNode)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (tableNode != null)
            {
                foreach (var child in tableNode.Children)
                    set.Add(child.Key);
            }
            return set;
        }

        private static string StringValue(string raw) => raw?.Trim().Trim('"');

        private static bool BoolValue(string raw) =>
            string.Equals(raw?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

        private static bool TryNumber(string raw, out double value) =>
            double.TryParse(raw?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private static double Number(string raw, double fallback = 0) =>
            TryNumber(raw, out var value) ? value : fallback;
    }
}
