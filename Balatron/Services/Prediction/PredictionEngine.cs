using System;
using System.Collections.Generic;
using System.Linq;
using Balatron.Services.Live;
using Balatron.Services.Rng;
using LuaRandomGen = Balatron.Services.Rng.LuaRandom;

namespace Balatron.Services.Prediction
{
    public enum PredictedKind
    {
        Joker,
        Tarot,
        Planet,
        Spectral,
        PlayingCard
    }

    public sealed class PredictedCard
    {
        public PredictedKind Kind { get; init; }
        public string CenterKey { get; init; }
        public string Name { get; init; }
        public string Text { get; init; }
        public int Rarity { get; init; }          // jokers only
        public string Edition { get; init; }      // Foil / Holographic / Polychrome / Negative
        public bool Eternal { get; init; }
        public bool Perishable { get; init; }
        public bool Rental { get; init; }
        public string Enhancement { get; init; }  // playing cards
        public string Seal { get; init; }         // playing cards
        public string Note { get; init; }         // e.g. "→ Perkeo"

        // What using this card right now would produce (consumables with
        // random outcomes). Outcome cards never carry outcomes themselves.
        public string OutcomeText { get; set; }
        public IReadOnlyList<PredictedCard> OutcomeCards { get; set; }
    }

    public sealed class ShopRerollPrediction
    {
        public int Index { get; init; } // 0 = the very next reroll
        public IReadOnlyList<PredictedCard> Slots { get; init; }
    }

    /// <summary>
    /// Replays Balatro's shop / booster-pack generation from a save-state
    /// snapshot, using the same per-key RNG streams the game will consume.
    /// Each public method starts from a fresh copy of the snapshot counters,
    /// i.e. every prediction assumes it is the player's next action.
    /// </summary>
    public sealed class PredictionEngine
    {
        private readonly GameStateSnapshot _snap;
        private readonly IReadOnlySet<string> _profileUnlocked; // null => assume everything unlocked
        private readonly int _ante;

        public PredictionEngine(GameStateSnapshot snapshot, IReadOnlySet<string> profileUnlocked)
        {
            _snap = snapshot;
            _profileUnlocked = profileUnlocked;
            _ante = snapshot.Ante;
        }

        private BalatroRng FreshRng() => new(_snap.Seed, _snap.RngCounters);

        // ------------------------------------------------------------------
        // Locking rules (mirrors get_current_pool's UNAVAILABLE logic)
        // ------------------------------------------------------------------

        /// <summary>
        /// Centers the game considers "in use" when generating the next shop.
        /// A reroll destroys the current shop cards first, which clears their
        /// used_jokers entries — so they must not be excluded.
        /// </summary>
        private HashSet<string> RerollExclusions()
        {
            var set = new HashSet<string>(_snap.UsedJokers, StringComparer.Ordinal);
            foreach (var card in _snap.ShopCards)
                set.Remove(card.CenterKey);
            return set;
        }

        /// <summary>Opening a pack keeps the shop intact, so everything in used_jokers stays excluded.</summary>
        private HashSet<string> PackExclusions() => new(_snap.UsedJokers, StringComparer.Ordinal);

        private bool IsProfileLocked(string jokerKey)
        {
            if (!BalatroItems.DefaultLockedJokers.Contains(jokerKey))
                return false;
            // No meta.jkr info: assume a completed profile (everything unlocked).
            return _profileUnlocked != null && !_profileUnlocked.Contains(jokerKey);
        }

        private bool IsJokerLocked(string key, ISet<string> exclusions, ISet<string> tempLocked)
        {
            if (_snap.BannedKeys.Contains(key))
                return true;
            if (IsProfileLocked(key))
                return true;
            if (BalatroItems.EnhancementGates.TryGetValue(key, out var enhancement)
                && !_snap.DeckEnhancements.Contains(enhancement))
                return true;
            if (key == "j_gros_michel" && _snap.PoolFlags.Contains("gros_michel_extinct"))
                return true;
            if (key == "j_cavendish" && !_snap.PoolFlags.Contains("gros_michel_extinct"))
                return true;
            if (!_snap.ShowmanOwned && (exclusions.Contains(key) || tempLocked.Contains(key)))
                return true;
            return false;
        }

        private bool IsConsumableLocked(string key, ISet<string> exclusions, ISet<string> tempLocked)
        {
            if (_snap.BannedKeys.Contains(key))
                return true;
            if (BalatroItems.SecretPlanetGates.TryGetValue(key, out var hand)
                && !(_snap.HandVisible.TryGetValue(hand, out var visible) && visible))
                return true;
            if (!_snap.ShowmanOwned && (exclusions.Contains(key) || tempLocked.Contains(key)))
                return true;
            return false;
        }

        // ------------------------------------------------------------------
        // Pool choice with the game's resample scheme
        // ------------------------------------------------------------------

        private static T ChooseWithResample<T>(BalatroRng rng, string key, IReadOnlyList<T> pool, Func<T, bool> isLocked)
            where T : class
        {
            var choice = pool[rng.ChooseIndex(key, pool.Count)];
            var resample = 2;
            while ((choice == null || isLocked(choice)) && resample < 500)
            {
                choice = pool[rng.ChooseIndex($"{key}_resample{resample}", pool.Count)];
                resample++;
            }
            return choice;
        }

        // ------------------------------------------------------------------
        // Shop rerolls
        // ------------------------------------------------------------------

        public IReadOnlyList<ShopRerollPrediction> PredictRerolls(int count)
        {
            var rng = FreshRng();
            var exclusions = RerollExclusions();
            var result = new List<ShopRerollPrediction>();

            for (var i = 0; i < count; i++)
            {
                var temp = new HashSet<string>(StringComparer.Ordinal);
                var slots = new List<PredictedCard>();
                for (var slot = 0; slot < _snap.ShopSlots; slot++)
                    slots.Add(NextShopCard(rng, exclusions, temp));

                result.Add(new ShopRerollPrediction { Index = i, Slots = slots });
            }

            return result;
        }

        private PredictedCard NextShopCard(BalatroRng rng, ISet<string> exclusions, ISet<string> tempLocked)
        {
            var totalRate = _snap.JokerRate + _snap.TarotRate + _snap.PlanetRate
                            + _snap.PlayingCardRate + _snap.SpectralRate;
            var poll = rng.Random("cdt" + _ante) * totalRate;

            if (poll < _snap.JokerRate)
                return NextJoker(rng, "sho", exclusions, tempLocked);
            poll -= _snap.JokerRate;

            if (poll < _snap.TarotRate)
                return WithOutcome(NextConsumable(rng, PredictedKind.Tarot, "sho", exclusions, tempLocked, soulable: false));
            poll -= _snap.TarotRate;

            if (poll < _snap.PlanetRate)
                return WithOutcome(NextConsumable(rng, PredictedKind.Planet, "sho", exclusions, tempLocked, soulable: false));
            poll -= _snap.PlanetRate;

            if (poll < _snap.PlayingCardRate)
                return NextShopPlayingCard(rng);

            return WithOutcome(NextConsumable(rng, PredictedKind.Spectral, "sho", exclusions, tempLocked, soulable: false));
        }

        private PredictedCard NextJoker(BalatroRng rng, string source, ISet<string> exclusions, ISet<string> tempLocked,
            int forcedRarity = 0)
        {
            var rarityIndex = forcedRarity;
            if (rarityIndex == 0)
            {
                var rarityPoll = rng.Random("rarity" + _ante + source);
                rarityIndex = rarityPoll > 0.95 ? 3 : rarityPoll > 0.7 ? 2 : 1;
            }
            var pool = rarityIndex switch
            {
                3 => BalatroItems.RareJokers,
                2 => BalatroItems.UncommonJokers,
                _ => BalatroItems.CommonJokers
            };

            var def = ChooseWithResample(rng, $"Joker{rarityIndex}{source}{_ante}", pool,
                j => IsJokerLocked(j.Key, exclusions, tempLocked));
            tempLocked.Add(def.Key);

            var edition = PollJokerEdition(rng, source);

            // Sticker polls only exist for shop and buffoon-pack jokers; the
            // eternal/perishable roll always burns there, regardless of stake.
            var eternal = false;
            var perishable = false;
            var rental = false;
            if (source is "sho" or "buf")
            {
                var fromPack = source == "buf";
                var stickerPoll = rng.Random((fromPack ? "packetper" : "etperpoll") + _ante);
                eternal = _snap.EternalsInShop && stickerPoll > 0.7
                          && !BalatroItems.EternalIncompatible.Contains(def.Key);
                perishable = _snap.PerishablesInShop && stickerPoll > 0.4 && stickerPoll <= 0.7
                             && !BalatroItems.PerishableIncompatible.Contains(def.Key);
                rental = _snap.RentalsInShop
                         && rng.Random((fromPack ? "packssjr" : "ssjr") + _ante) > 0.7;
            }

            return new PredictedCard
            {
                Kind = PredictedKind.Joker,
                CenterKey = def.Key,
                Name = def.Name,
                Text = def.Text,
                Rarity = def.Rarity,
                Edition = edition,
                Eternal = eternal,
                Perishable = perishable,
                Rental = rental
            };
        }

        private string PollJokerEdition(BalatroRng rng, string source)
        {
            var poll = rng.Random($"edi{source}{_ante}");
            var m = _snap.EditionRate;
            if (poll > 1 - 0.003 * m) return "Negative";
            if (poll > 1 - 0.006 * m) return "Polychrome";
            if (poll > 1 - 0.02 * m) return "Holographic";
            if (poll > 1 - 0.04 * m) return "Foil";
            return null;
        }

        private PredictedCard NextConsumable(BalatroRng rng, PredictedKind kind, string source,
            ISet<string> exclusions, ISet<string> tempLocked, bool soulable)
        {
            var (typeName, pool) = kind switch
            {
                PredictedKind.Tarot => ("Tarot", BalatroItems.Tarots),
                PredictedKind.Planet => ("Planet", BalatroItems.Planets),
                _ => ("Spectral", BalatroItems.Spectrals)
            };

            if (soulable && !_snap.BannedKeys.Contains("c_soul"))
            {
                // Soul / Black Hole rolls are skipped entirely while the card is
                // "in use" — the short-circuit matters because a skipped roll
                // does not advance the soul_ counter.
                string forced = null;
                if (kind is PredictedKind.Tarot or PredictedKind.Spectral)
                {
                    if (CanForce("c_soul", exclusions, tempLocked) && rng.Random($"soul_{typeName}{_ante}") > 0.997)
                        forced = "c_soul";
                }
                if (kind is PredictedKind.Planet or PredictedKind.Spectral)
                {
                    if (CanForce("c_black_hole", exclusions, tempLocked) && rng.Random($"soul_{typeName}{_ante}") > 0.997)
                        forced = "c_black_hole";
                }

                if (forced == "c_soul")
                {
                    tempLocked.Add("c_soul");
                    var legendary = PredictSoulJoker(rng);
                    return Card(kind, BalatroItems.TheSoul, note: legendary != null ? $"→ {legendary.Name}" : null);
                }
                if (forced == "c_black_hole")
                {
                    tempLocked.Add("c_black_hole");
                    return Card(kind, BalatroItems.BlackHole);
                }
            }

            var def = ChooseWithResample(rng, $"{typeName}{source}{_ante}", pool,
                c => IsConsumableLocked(c.Key, exclusions, tempLocked));
            tempLocked.Add(def.Key);
            return Card(kind, def);
        }

        private bool CanForce(string key, ISet<string> exclusions, ISet<string> tempLocked) =>
            _snap.ShowmanOwned || (!exclusions.Contains(key) && !tempLocked.Contains(key));

        private JokerDef PredictSoulJoker(BalatroRng rng)
        {
            var owned = new HashSet<string>(_snap.UsedJokers, StringComparer.Ordinal);
            return ChooseWithResample(rng, "Joker4", BalatroItems.LegendaryJokers,
                j => !_snap.ShowmanOwned && owned.Contains(j.Key));
        }

        private static PredictedCard Card(PredictedKind kind, ConsumableDef def, string note = null) => new()
        {
            Kind = kind,
            CenterKey = def.Key,
            Name = def.Name,
            Text = def.Text,
            Note = note
        };

        private PredictedCard NextShopPlayingCard(BalatroRng rng)
        {
            var cardKey = BalatroItems.CardKeys[rng.ChooseIndex($"frontsho{_ante}", BalatroItems.CardKeys.Count)];
            string note = null;
            if (_snap.UsedVouchers.Contains("v_illusion") && rng.Random("illusion") > 0.6)
                note = "modified (Illusion)";

            return new PredictedCard
            {
                Kind = PredictedKind.PlayingCard,
                CenterKey = cardKey,
                Name = BalatroItems.CardDisplayName(cardKey),
                Note = note
            };
        }

        // ------------------------------------------------------------------
        // Booster pack contents
        // ------------------------------------------------------------------

        public IReadOnlyList<PredictedCard> PredictPackContents(string packCenterKey)
        {
            var def = BalatroItems.PackFromCenterKey(packCenterKey);
            if (def == null)
                return Array.Empty<PredictedCard>();

            return GenerateSegment(FreshRng(), def, PackExclusions());
        }

        /// <summary>
        /// Packs of the same kind consume the same RNG streams: opening one
        /// shifts the next one's contents. This simulates opening the given
        /// packs in order and returns one card segment per pack — together
        /// they form the kind's continuous card sequence.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<PredictedCard>> PredictPackSequence(IReadOnlyList<PackDef> packs)
        {
            var rng = FreshRng();
            var exclusions = PackExclusions();
            return packs.Select(def => (IReadOnlyList<PredictedCard>)GenerateSegment(rng, def, exclusions)).ToList();
        }

        private List<PredictedCard> GenerateSegment(BalatroRng rng, PackDef def, ISet<string> exclusions)
        {
            // In-pack duplicate locks reset at pack boundaries: unchosen cards
            // return to the pool when a pack closes.
            var temp = new HashSet<string>(StringComparer.Ordinal);
            var cards = new List<PredictedCard>();

            for (var i = 0; i < def.CardCount; i++)
            {
                switch (def.Kind)
                {
                    case "Arcana":
                        if (_snap.UsedVouchers.Contains("v_omen_globe") && rng.Random("omen_globe") > 0.8)
                            cards.Add(WithOutcome(NextConsumable(rng, PredictedKind.Spectral, "ar2", exclusions, temp, soulable: true)));
                        else
                            cards.Add(WithOutcome(NextConsumable(rng, PredictedKind.Tarot, "ar1", exclusions, temp, soulable: true)));
                        break;
                    case "Celestial":
                        cards.Add(WithOutcome(NextConsumable(rng, PredictedKind.Planet, "pl1", exclusions, temp, soulable: true)));
                        break;
                    case "Spectral":
                        cards.Add(WithOutcome(NextConsumable(rng, PredictedKind.Spectral, "spe", exclusions, temp, soulable: true)));
                        break;
                    case "Buffoon":
                        var joker = NextJoker(rng, "buf", exclusions, temp);
                        cards.Add(joker);
                        break;
                    case "Standard":
                        cards.Add(NextStandardCard(rng));
                        break;
                }
            }

            return cards;
        }

        private PredictedCard NextStandardCard(BalatroRng rng)
        {
            string enhancement = null;
            if (rng.Random("stdset" + _ante) > 0.6)
            {
                var def = BalatroItems.Enhancements[rng.ChooseIndex($"Enhancedsta{_ante}", BalatroItems.Enhancements.Count)];
                enhancement = def.Name;
            }

            var cardKey = BalatroItems.CardKeys[rng.ChooseIndex($"frontsta{_ante}", BalatroItems.CardKeys.Count)];

            string edition = null;
            var editionPoll = rng.Random("standard_edition" + _ante);
            if (editionPoll > 0.988) edition = "Polychrome";
            else if (editionPoll > 0.96) edition = "Holographic";
            else if (editionPoll > 0.92) edition = "Foil";

            string seal = null;
            if (rng.Random("stdseal" + _ante) > 0.8)
            {
                var sealPoll = rng.Random("stdsealtype" + _ante);
                seal = sealPoll > 0.75 ? "Red Seal"
                    : sealPoll > 0.5 ? "Blue Seal"
                    : sealPoll > 0.25 ? "Gold Seal"
                    : "Purple Seal";
            }

            return new PredictedCard
            {
                Kind = PredictedKind.PlayingCard,
                CenterKey = cardKey,
                Name = BalatroItems.CardDisplayName(cardKey),
                Enhancement = enhancement,
                Edition = edition,
                Seal = seal
            };
        }

        // ------------------------------------------------------------------
        // Consumable outcomes ("if bought and used right now")
        // ------------------------------------------------------------------

        /// <summary>
        /// Computes what a consumable with a random effect would actually do if
        /// used next, from the current save counters. Each effect has its own
        /// RNG stream, so this stays valid regardless of shop actions.
        /// Returns false when the card has no predictable random outcome.
        /// </summary>
        public bool TryPredictOutcome(string centerKey, out string text, out IReadOnlyList<PredictedCard> cards)
        {
            text = null;
            cards = null;

            switch (centerKey)
            {
                case "c_high_priestess":
                    cards = PredictCreatedConsumables(PredictedKind.Planet, "pri", centerKey);
                    return true;

                case "c_emperor":
                    cards = PredictCreatedConsumables(PredictedKind.Tarot, "emp", centerKey);
                    return true;

                case "c_judgement":
                {
                    var rng = FreshRng();
                    cards = new[] { NextJoker(rng, "jud", PackExclusions(), new HashSet<string>(StringComparer.Ordinal)) };
                    return true;
                }

                case "c_wraith":
                {
                    var rng = FreshRng();
                    cards = new[] { NextJoker(rng, "wra", PackExclusions(), new HashSet<string>(StringComparer.Ordinal), forcedRarity: 3) };
                    return true;
                }

                case "c_wheel_of_fortune":
                    return TryPredictWheelOfFortune(out text, out cards);

                case "c_ectoplasm":
                    return TryPredictJokerEdition("ectoplasm", "Negative", EditionlessJokers(), out text, out cards);

                case "c_hex":
                    return TryPredictJokerEdition("hex", "Polychrome", EditionlessJokers(), out text, out cards);

                case "c_ankh":
                {
                    // Ankh picks from every joker, including Eternal ones.
                    var pool = JokersBySortId(_snap.OwnedJokers);
                    if (pool.Count == 0)
                    {
                        text = "No Jokers to copy";
                        return true;
                    }
                    var chosen = pool[FreshRng().ChooseIndex("ankh_choice", pool.Count)];
                    cards = new[] { OwnedJokerCard(chosen) };
                    text = "Copied (all others destroyed)";
                    return true;
                }

                case "c_aura":
                {
                    // poll_edition('aura', guaranteed, no negative)
                    var poll = FreshRng().Random("aura");
                    text = poll > 0.85 ? "Polychrome" : poll > 0.5 ? "Holographic" : "Foil";
                    return true;
                }

                case "c_familiar":
                    return TryPredictHandConjure("Familiar", 3, out text, out cards);

                case "c_grim":
                    return TryPredictHandConjure("Grim", 2, out text, out cards);

                case "c_incantation":
                    return TryPredictHandConjure("Incantation", 4, out text, out cards);

                case "c_immolate":
                    return TryPredictImmolate(out text, out cards);

                case "c_sigil":
                {
                    var suits = new[] { "Spades", "Hearts", "Diamonds", "Clubs" };
                    text = $"All cards in hand become {suits[FreshRng().ChooseIndex("sigil", suits.Length)]}";
                    return true;
                }

                case "c_ouija":
                {
                    var ranks = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King", "Ace" };
                    text = $"All cards in hand become rank {ranks[FreshRng().ChooseIndex("ouija", ranks.Length)]}";
                    return true;
                }

                case "c_fool" when !string.IsNullOrEmpty(_snap.LastTarotPlanet)
                                   && _snap.LastTarotPlanet != "c_fool"
                                   && BalatroItems.ConsumablesByKey.TryGetValue(_snap.LastTarotPlanet, out var last):
                {
                    var kind = BalatroItems.Planets.Any(p => p.Key == last.Key) ? PredictedKind.Planet : PredictedKind.Tarot;
                    cards = new[] { Card(kind, last) };
                    return true;
                }

                default:
                    return false;
            }
        }

        private IReadOnlyList<PredictedCard> PredictCreatedConsumables(PredictedKind kind, string source, string selfKey)
        {
            var rng = FreshRng();
            var exclusions = PackExclusions();
            exclusions.Add(selfKey); // the used card is still materialized while its effect runs
            var temp = new HashSet<string>(StringComparer.Ordinal);

            // "Up to 2": limited by free consumable room after the used card's
            // slot frees up.
            var count = Math.Clamp(_snap.ConsumableSlots - _snap.OwnedConsumableCount, 0, 2);
            if (count == 0)
                count = 1; // buying from the shop requires a free slot anyway

            var cards = new List<PredictedCard>();
            for (var i = 0; i < count; i++)
                cards.Add(NextConsumable(rng, kind, source, exclusions, temp, soulable: false));
            return cards;
        }

        // Every "random card/joker" pick runs through pseudorandom_element,
        // which sorts candidates by sort_id before indexing — so these are
        // reproducible from the save regardless of on-screen order.
        private static List<OwnedJokerInfo> JokersBySortId(IEnumerable<OwnedJokerInfo> jokers) =>
            jokers.OrderBy(j => j.SortId).ToList();

        private List<OwnedJokerInfo> EditionlessJokers() =>
            JokersBySortId(_snap.OwnedJokers.Where(j => !j.HasEdition));

        private List<HandCardInfo> HandBySortId() =>
            _snap.HandCards.OrderBy(c => c.SortId).ToList();

        private static PredictedCard OwnedJokerCard(OwnedJokerInfo joker, string edition = null)
        {
            BalatroItems.JokersByKey.TryGetValue(joker.CenterKey ?? string.Empty, out var def);
            return new PredictedCard
            {
                Kind = PredictedKind.Joker,
                CenterKey = joker.CenterKey,
                Name = joker.Label ?? def?.Name ?? joker.CenterKey,
                Text = def?.Text,
                Rarity = def?.Rarity ?? 1,
                Edition = edition ?? (joker.HasEdition ? joker.Edition : null)
            };
        }

        private static PredictedCard HandCard(HandCardInfo card)
        {
            string enhancement = null;
            if (card.EnhancementKey != null
                && BalatroItems.EnhancementsByKey.TryGetValue(card.EnhancementKey, out var enh))
                enhancement = enh.Name;

            return new PredictedCard
            {
                Kind = PredictedKind.PlayingCard,
                CenterKey = card.CardKey,
                Name = BalatroItems.CardDisplayName(card.CardKey),
                Enhancement = enhancement,
                Seal = card.Seal != null ? $"{card.Seal} Seal" : null
            };
        }

        private bool TryPredictJokerEdition(string key, string edition, List<OwnedJokerInfo> pool,
            out string text, out IReadOnlyList<PredictedCard> cards)
        {
            text = null;
            cards = null;

            if (pool.Count == 0)
            {
                text = "No Jokers without an edition";
                return true;
            }

            var chosen = pool[FreshRng().ChooseIndex(key, pool.Count)];
            cards = new[] { OwnedJokerCard(chosen, edition) };
            return true;
        }

        /// <summary>Familiar / Grim / Incantation: destroy one card in hand, conjure enhanced ones.</summary>
        private bool TryPredictHandConjure(string effect, int count, out string text,
            out IReadOnlyList<PredictedCard> cards)
        {
            text = null;
            cards = null;

            var hand = HandBySortId();
            if (hand.Count == 0)
            {
                text = "Needs cards in hand";
                return true;
            }

            var rng = FreshRng();
            var destroyed = hand[rng.ChooseIndex("random_destroy", hand.Count)];

            var suits = new[] { "S", "H", "D", "C" };
            // The Enhanced pool in game order, minus Stone.
            var enhancements = BalatroItems.Enhancements.Where(e => e.Key != "m_stone").ToList();

            var created = new List<PredictedCard>();
            for (var i = 0; i < count; i++)
            {
                string rank;
                string suit;
                switch (effect)
                {
                    case "Familiar":
                    {
                        var faces = new[] { "J", "Q", "K" };
                        rank = faces[rng.ChooseIndex("familiar_create", faces.Length)];
                        suit = suits[rng.ChooseIndex("familiar_create", suits.Length)];
                        break;
                    }
                    case "Grim":
                        rank = "A";
                        suit = suits[rng.ChooseIndex("grim_create", suits.Length)];
                        break;
                    default:
                    {
                        var numbers = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "T" };
                        rank = numbers[rng.ChooseIndex("incantation_create", numbers.Length)];
                        suit = suits[rng.ChooseIndex("incantation_create", suits.Length)];
                        break;
                    }
                }

                var enhancement = enhancements[rng.ChooseIndex("spe_card", enhancements.Count)];
                var cardKey = $"{suit}_{rank}";
                created.Add(new PredictedCard
                {
                    Kind = PredictedKind.PlayingCard,
                    CenterKey = cardKey,
                    Name = BalatroItems.CardDisplayName(cardKey),
                    Enhancement = enhancement.Name
                });
            }

            cards = created;
            text = $"Destroys {BalatroItems.CardDisplayName(destroyed.CardKey)}, creates:";
            return true;
        }

        private bool TryPredictImmolate(out string text, out IReadOnlyList<PredictedCard> cards)
        {
            text = null;
            cards = null;

            var hand = HandBySortId();
            if (hand.Count == 0)
            {
                text = "Needs cards in hand";
                return true;
            }

            // pseudoshuffle: sort by sort_id, then seeded Fisher-Yates.
            var shuffled = hand.ToList();
            var rng = FreshRng().Generator("immolate");
            for (var i = shuffled.Count; i >= 2; i--)
            {
                var j = rng.NextInt(1, i);
                (shuffled[i - 1], shuffled[j - 1]) = (shuffled[j - 1], shuffled[i - 1]);
            }

            cards = shuffled.Take(5).Select(HandCard).ToList();
            text = "Destroys:";
            return true;
        }

        /// <summary>
        /// What an owned joker would do the next time its random effect fires.
        /// </summary>
        public bool TryPredictJokerOutcome(string centerKey, out string text, out IReadOnlyList<PredictedCard> cards)
        {
            text = null;
            cards = null;

            switch (centerKey)
            {
                case "j_perkeo":
                {
                    var pool = _snap.OwnedConsumables.OrderBy(c => c.SortId).ToList();
                    if (pool.Count == 0)
                    {
                        text = "No consumables to copy";
                        return true;
                    }
                    var chosen = pool[FreshRng().ChooseIndex("perkeo", pool.Count)];
                    BalatroItems.ConsumablesByKey.TryGetValue(chosen.CenterKey ?? string.Empty, out var def);
                    cards = new[]
                    {
                        new PredictedCard
                        {
                            Kind = KindOfConsumable(chosen.CenterKey),
                            CenterKey = chosen.CenterKey,
                            Name = def?.Name ?? chosen.Label,
                            Text = def?.Text,
                            Edition = "Negative"
                        }
                    };
                    text = "On leaving the shop:";
                    return true;
                }

                case "j_invisible":
                {
                    var pool = JokersBySortId(_snap.OwnedJokers.Where(j => j.CenterKey != "j_invisible"));
                    if (pool.Count == 0)
                    {
                        text = "No other Jokers to duplicate";
                        return true;
                    }
                    var chosen = pool[FreshRng().ChooseIndex("invisible", pool.Count)];
                    cards = new[] { OwnedJokerCard(chosen) };
                    text = "When sold, duplicates:";
                    return true;
                }

                case "j_madness":
                {
                    var pool = JokersBySortId(_snap.OwnedJokers
                        .Where(j => j.CenterKey != "j_madness" && !j.Eternal));
                    if (pool.Count == 0)
                    {
                        text = "No destructible Jokers";
                        return true;
                    }
                    var chosen = pool[FreshRng().ChooseIndex("madness", pool.Count)];
                    cards = new[] { OwnedJokerCard(chosen) };
                    text = "Next blind, destroys:";
                    return true;
                }

                case "j_certificate":
                {
                    var rng = FreshRng();
                    var cardKey = BalatroItems.CardKeys[rng.ChooseIndex("cert_fr", BalatroItems.CardKeys.Count)];
                    var sealPoll = rng.Random("certsl");
                    var seal = sealPoll > 0.75 ? "Red Seal"
                        : sealPoll > 0.5 ? "Blue Seal"
                        : sealPoll > 0.25 ? "Gold Seal"
                        : "Purple Seal";
                    cards = new[]
                    {
                        new PredictedCard
                        {
                            Kind = PredictedKind.PlayingCard,
                            CenterKey = cardKey,
                            Name = BalatroItems.CardDisplayName(cardKey),
                            Seal = seal
                        }
                    };
                    text = "At round start, adds:";
                    return true;
                }

                case "j_marble":
                {
                    var cardKey = BalatroItems.CardKeys[FreshRng().ChooseIndex("marb_fr", BalatroItems.CardKeys.Count)];
                    cards = new[]
                    {
                        new PredictedCard
                        {
                            Kind = PredictedKind.PlayingCard,
                            CenterKey = cardKey,
                            Name = BalatroItems.CardDisplayName(cardKey),
                            Enhancement = "Stone Card"
                        }
                    };
                    text = "Next blind, adds to deck:";
                    return true;
                }

                case "j_8_ball":
                    return TryPredictJokerCreation(PredictedKind.Tarot, "8ba", "Per scoring 8:", out text, out cards);
                case "j_superposition":
                    return TryPredictJokerCreation(PredictedKind.Tarot, "sup", "On Ace + Straight:", out text, out cards);
                case "j_cartomancer":
                    return TryPredictJokerCreation(PredictedKind.Tarot, "car", "When blind selected:", out text, out cards);
                case "j_vagabond":
                    return TryPredictJokerCreation(PredictedKind.Tarot, "vag", "Playing a hand at ≤$4:", out text, out cards);
                case "j_seance":
                    return TryPredictJokerCreation(PredictedKind.Spectral, "sea", "On Straight Flush:", out text, out cards);
                case "j_sixth_sense":
                    return TryPredictJokerCreation(PredictedKind.Spectral, "sixth", "On first-hand single 6:", out text, out cards);

                case "j_riff_raff":
                {
                    var rng = FreshRng();
                    var exclusions = PackExclusions();
                    var temp = new HashSet<string>(StringComparer.Ordinal);
                    cards = new[]
                    {
                        NextJoker(rng, "rif", exclusions, temp, forcedRarity: 1),
                        NextJoker(rng, "rif", exclusions, temp, forcedRarity: 1)
                    };
                    text = "When blind selected:";
                    return true;
                }

                default:
                    return false;
            }
        }

        private static PredictedKind KindOfConsumable(string centerKey) =>
            BalatroItems.Tarots.Any(t => t.Key == centerKey) ? PredictedKind.Tarot
            : BalatroItems.Planets.Any(p => p.Key == centerKey) ? PredictedKind.Planet
            : PredictedKind.Spectral;

        private bool TryPredictJokerCreation(PredictedKind kind, string source, string label,
            out string text, out IReadOnlyList<PredictedCard> cards)
        {
            var rng = FreshRng();
            cards = new[]
            {
                NextConsumable(rng, kind, source, PackExclusions(),
                    new HashSet<string>(StringComparer.Ordinal), soulable: false)
            };
            text = label;
            return true;
        }

        private bool TryPredictWheelOfFortune(out string text, out IReadOnlyList<PredictedCard> cards)
        {
            text = null;
            cards = null;

            var eligible = _snap.OwnedJokers
                .Where(j => string.IsNullOrEmpty(j.Edition) || j.Edition == "None")
                .ToList();
            if (eligible.Count == 0)
            {
                text = "No eligible Jokers (all have editions)";
                return true;
            }

            var rng = FreshRng();
            if (rng.Random("wheel_of_fortune") >= 0.25)
            {
                text = "Nope!";
                return true;
            }

            var target = eligible[rng.ChooseIndex("wheel_of_fortune", eligible.Count)];
            var editionPoll = rng.Random("wheel_of_fortune");
            var edition = editionPoll > 0.85 ? "Polychrome" : editionPoll > 0.5 ? "Holographic" : "Foil";

            BalatroItems.JokersByKey.TryGetValue(target.CenterKey ?? string.Empty, out var def);
            cards = new[]
            {
                new PredictedCard
                {
                    Kind = PredictedKind.Joker,
                    CenterKey = target.CenterKey,
                    Name = target.Label ?? def?.Name ?? target.CenterKey,
                    Text = def?.Text,
                    Rarity = def?.Rarity ?? 1,
                    Edition = edition
                }
            };
            return true;
        }

        /// <summary>Attaches the outcome prediction to a freshly generated consumable card.</summary>
        private PredictedCard WithOutcome(PredictedCard card)
        {
            if (TryPredictOutcome(card.CenterKey, out var text, out var cards))
            {
                card.OutcomeText = text;
                card.OutcomeCards = cards;
            }
            return card;
        }

        // ------------------------------------------------------------------
        // Vouchers
        // ------------------------------------------------------------------

        private bool IsVoucherLocked(string key)
        {
            if (_snap.BannedKeys.Contains(key))
                return true;
            if (_snap.UsedVouchers.Contains(key))
                return true;
            if (BalatroItems.UpgradeVoucherBase.TryGetValue(key, out var baseKey))
            {
                if (!_snap.UsedVouchers.Contains(baseKey))
                    return true;
                if (_profileUnlocked != null && !_profileUnlocked.Contains(key))
                    return true;
            }
            return false;
        }

        /// <summary>The voucher the next shop will offer ("Voucher" + the ante that shop belongs to).</summary>
        public VoucherDef PredictShopVoucher(int voucherAnte)
        {
            var rng = FreshRng();
            return ChooseWithResample(rng, "Voucher" + voucherAnte, BalatroItems.Vouchers,
                v => IsVoucherLocked(v.Key));
        }

        /// <summary>
        /// Vouchers added to the shop by skipping blinds with a Voucher Tag,
        /// in skip order. Sequential skips share the stream and can't repeat.
        /// </summary>
        public IReadOnlyList<VoucherDef> PredictTagVouchers(int count)
        {
            var rng = FreshRng();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<VoucherDef>();
            for (var i = 0; i < count; i++)
            {
                var voucher = ChooseWithResample(rng, "Voucher_fromtag", BalatroItems.Vouchers,
                    v => IsVoucherLocked(v.Key) || seen.Contains(v.Key));
                seen.Add(voucher.Key);
                result.Add(voucher);
            }
            return result;
        }

        // ------------------------------------------------------------------
        // Next shop's booster offers (rolled when a shop is entered)
        // ------------------------------------------------------------------

        public IReadOnlyList<PackDef> PredictNextShopPacks(int packSlots = 2)
        {
            var rng = FreshRng();
            var offers = new List<PackDef>();
            var totalWeight = BalatroItems.Packs.Sum(p => p.Weight);

            for (var i = 0; i < packSlots; i++)
            {
                // The first pack ever offered (ante <= 2) is always a Buffoon
                // Pack and consumes no RNG.
                if (i == 0 && !_snap.FirstShopBuffoonDone && _ante <= 2)
                {
                    offers.Add(BalatroItems.Packs.First(p => p.KeyPrefix == "p_buffoon_normal"));
                    continue;
                }

                var poll = rng.Random("shop_pack" + _ante) * totalWeight;
                double cumulative = 0;
                foreach (var pack in BalatroItems.Packs)
                {
                    cumulative += pack.Weight;
                    if (poll < cumulative)
                    {
                        offers.Add(pack);
                        break;
                    }
                }
            }

            return offers;
        }
    }
}
