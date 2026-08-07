using System;
using System.Collections.Generic;
using System.Linq;

namespace Balatron.Services.Prediction
{
    public sealed class JokerDef
    {
        public string Key { get; }
        public string Name { get; }
        public int Rarity { get; } // 1 common, 2 uncommon, 3 rare, 4 legendary
        public string Text { get; }

        public JokerDef(string key, string name, int rarity, string text)
        {
            Key = key;
            Name = name;
            Rarity = rarity;
            Text = text;
        }
    }

    public sealed class ConsumableDef
    {
        public string Key { get; }
        public string Name { get; }
        public string Text { get; }

        public ConsumableDef(string key, string name, string text)
        {
            Key = key;
            Name = name;
            Text = text;
        }
    }

    public sealed class VoucherDef
    {
        public string Key { get; }
        public string Name { get; }
        public string Text { get; }

        public VoucherDef(string key, string name, string text)
        {
            Key = key;
            Name = name;
            Text = text;
        }
    }

    public sealed class PackDef
    {
        public string KeyPrefix { get; }
        public string Name { get; }
        public string Kind { get; } // Arcana, Celestial, Standard, Buffoon, Spectral
        public int CardCount { get; }
        public int Choices { get; }
        public double Weight { get; }

        public PackDef(string keyPrefix, string name, string kind, int cardCount, int choices, double weight)
        {
            KeyPrefix = keyPrefix;
            Name = name;
            Kind = kind;
            CardCount = cardCount;
            Choices = choices;
            Weight = weight;
        }
    }

    /// <summary>
    /// Static registries for Balatro 1.0.1. Pool ordering is load-bearing:
    /// it must match the game's internal pool order exactly (validated against
    /// SpectralPack/Immolate), because items are picked by pool index.
    /// </summary>
    public static class BalatroItems
    {
        private static JokerDef J(string key, string name, int rarity, string text) => new(key, name, rarity, text);

        public static readonly IReadOnlyList<JokerDef> CommonJokers = new[]
        {
            J("j_joker", "Joker", 1, "+4 Mult"),
            J("j_greedy_joker", "Greedy Joker", 1, "Played cards with Diamond suit give +3 Mult when scored"),
            J("j_lusty_joker", "Lusty Joker", 1, "Played cards with Heart suit give +3 Mult when scored"),
            J("j_wrathful_joker", "Wrathful Joker", 1, "Played cards with Spade suit give +3 Mult when scored"),
            J("j_gluttenous_joker", "Gluttonous Joker", 1, "Played cards with Club suit give +3 Mult when scored"),
            J("j_jolly", "Jolly Joker", 1, "+8 Mult if played hand contains a Pair"),
            J("j_zany", "Zany Joker", 1, "+12 Mult if played hand contains a Three of a Kind"),
            J("j_mad", "Mad Joker", 1, "+10 Mult if played hand contains a Two Pair"),
            J("j_crazy", "Crazy Joker", 1, "+12 Mult if played hand contains a Straight"),
            J("j_droll", "Droll Joker", 1, "+10 Mult if played hand contains a Flush"),
            J("j_sly", "Sly Joker", 1, "+50 Chips if played hand contains a Pair"),
            J("j_wily", "Wily Joker", 1, "+100 Chips if played hand contains a Three of a Kind"),
            J("j_clever", "Clever Joker", 1, "+80 Chips if played hand contains a Two Pair"),
            J("j_devious", "Devious Joker", 1, "+100 Chips if played hand contains a Straight"),
            J("j_crafty", "Crafty Joker", 1, "+80 Chips if played hand contains a Flush"),
            J("j_half", "Half Joker", 1, "+20 Mult if played hand contains 3 or fewer cards"),
            J("j_credit_card", "Credit Card", 1, "Go up to -$20 in debt"),
            J("j_banner", "Banner", 1, "+30 Chips for each remaining discard"),
            J("j_mystic_summit", "Mystic Summit", 1, "+15 Mult when 0 discards remaining"),
            J("j_8_ball", "8 Ball", 1, "1 in 4 chance for each played 8 to create a Tarot card when scored"),
            J("j_misprint", "Misprint", 1, "+0-23 Mult (random)"),
            J("j_raised_fist", "Raised Fist", 1, "Adds double the rank of lowest ranked card held in hand to Mult"),
            J("j_chaos", "Chaos the Clown", 1, "1 free Reroll per shop"),
            J("j_scary_face", "Scary Face", 1, "Played face cards give +30 Chips when scored"),
            J("j_abstract", "Abstract Joker", 1, "+3 Mult for each Joker card"),
            J("j_delayed_grat", "Delayed Gratification", 1, "Earn $2 per discard if no discards are used by end of round"),
            J("j_gros_michel", "Gros Michel", 1, "+15 Mult; 1 in 6 chance to be destroyed at end of round"),
            J("j_even_steven", "Even Steven", 1, "Played cards with even rank give +4 Mult when scored (10 8 6 4 2)"),
            J("j_odd_todd", "Odd Todd", 1, "Played cards with odd rank give +31 Chips when scored (A 9 7 5 3)"),
            J("j_scholar", "Scholar", 1, "Played Aces give +20 Chips and +4 Mult when scored"),
            J("j_business", "Business Card", 1, "Played face cards have a 1 in 2 chance to give $2 when scored"),
            J("j_supernova", "Supernova", 1, "Adds the number of times poker hand has been played this run to Mult"),
            J("j_ride_the_bus", "Ride the Bus", 1, "Gains +1 Mult per consecutive hand played without a scoring face card"),
            J("j_egg", "Egg", 1, "Gains $3 of sell value at end of round"),
            J("j_runner", "Runner", 1, "Gains +15 Chips if played hand contains a Straight"),
            J("j_ice_cream", "Ice Cream", 1, "+100 Chips; -5 Chips for every hand played"),
            J("j_splash", "Splash", 1, "Every played card counts in scoring"),
            J("j_blue_joker", "Blue Joker", 1, "+2 Chips for each remaining card in deck"),
            J("j_faceless", "Faceless Joker", 1, "Earn $5 if 3 or more face cards are discarded at the same time"),
            J("j_green_joker", "Green Joker", 1, "+1 Mult per hand played; -1 Mult per discard"),
            J("j_superposition", "Superposition", 1, "Creates a Tarot card if hand contains an Ace and a Straight (must have room)"),
            J("j_todo_list", "To Do List", 1, "Earn $4 if poker hand is the listed hand; hand changes every round"),
            J("j_cavendish", "Cavendish", 1, "x3 Mult; 1 in 1000 chance to be destroyed at end of round"),
            J("j_red_card", "Red Card", 1, "Gains +3 Mult when any Booster Pack is skipped"),
            J("j_square", "Square Joker", 1, "Gains +4 Chips if played hand has exactly 4 cards"),
            J("j_riff_raff", "Riff-Raff", 1, "When Blind is selected, create 2 Common Jokers (must have room)"),
            J("j_photograph", "Photograph", 1, "First played face card gives x2 Mult when scored"),
            J("j_reserved_parking", "Reserved Parking", 1, "Each face card held in hand has a 1 in 2 chance to give $1"),
            J("j_mail", "Mail-In Rebate", 1, "Earn $5 for each discarded card of the listed rank; rank changes every round"),
            J("j_hallucination", "Hallucination", 1, "1 in 2 chance to create a Tarot card when any Booster Pack is opened (must have room)"),
            J("j_fortune_teller", "Fortune Teller", 1, "+1 Mult for each Tarot card used this run"),
            J("j_juggler", "Juggler", 1, "+1 hand size"),
            J("j_drunkard", "Drunkard", 1, "+1 discard each round"),
            J("j_golden", "Golden Joker", 1, "Earn $4 at end of round"),
            J("j_popcorn", "Popcorn", 1, "+20 Mult; -4 Mult per round played"),
            J("j_walkie_talkie", "Walkie Talkie", 1, "Each played 10 or 4 gives +10 Chips and +4 Mult when scored"),
            J("j_smiley", "Smiley Face", 1, "Played face cards give +5 Mult when scored"),
            J("j_ticket", "Golden Ticket", 1, "Played Gold cards earn $4 when scored"),
            J("j_swashbuckler", "Swashbuckler", 1, "Adds the sell value of all other owned Jokers to Mult"),
            J("j_hanging_chad", "Hanging Chad", 1, "Retrigger first played card used in scoring 2 additional times"),
            J("j_shoot_the_moon", "Shoot the Moon", 1, "Each Queen held in hand gives +13 Mult"),
        };

        public static readonly IReadOnlyList<JokerDef> UncommonJokers = new[]
        {
            J("j_stencil", "Joker Stencil", 2, "x1 Mult for each empty Joker slot (Joker Stencil included)"),
            J("j_four_fingers", "Four Fingers", 2, "All Flushes and Straights can be made with 4 cards"),
            J("j_mime", "Mime", 2, "Retrigger all card held in hand abilities"),
            J("j_ceremonial", "Ceremonial Dagger", 2, "When Blind is selected, destroy Joker to the right and permanently add double its sell value to this Mult"),
            J("j_marble", "Marble Joker", 2, "Adds one Stone card to the deck when Blind is selected"),
            J("j_loyalty_card", "Loyalty Card", 2, "x4 Mult every 6 hands played"),
            J("j_dusk", "Dusk", 2, "Retrigger all played cards in final hand of the round"),
            J("j_fibonacci", "Fibonacci", 2, "Each played Ace, 2, 3, 5 or 8 gives +8 Mult when scored"),
            J("j_steel_joker", "Steel Joker", 2, "Gives x0.2 Mult for each Steel Card in the full deck"),
            J("j_hack", "Hack", 2, "Retrigger each played 2, 3, 4 or 5"),
            J("j_pareidolia", "Pareidolia", 2, "All cards are considered face cards"),
            J("j_space", "Space Joker", 2, "1 in 4 chance to upgrade level of played poker hand"),
            J("j_burglar", "Burglar", 2, "When Blind is selected, gain +3 hands and lose all discards"),
            J("j_blackboard", "Blackboard", 2, "x3 Mult if all cards held in hand are Spades or Clubs"),
            J("j_sixth_sense", "Sixth Sense", 2, "If first hand of round is a single 6, destroy it and create a Spectral card (must have room)"),
            J("j_constellation", "Constellation", 2, "Gains x0.1 Mult per Planet card used"),
            J("j_hiker", "Hiker", 2, "Every played card permanently gains +5 Chips when scored"),
            J("j_card_sharp", "Card Sharp", 2, "x3 Mult if played poker hand has already been played this round"),
            J("j_madness", "Madness", 2, "When Small or Big Blind is selected, gain x0.5 Mult and destroy a random Joker"),
            J("j_seance", "Séance", 2, "If poker hand is a Straight Flush, create a random Spectral card (must have room)"),
            J("j_vampire", "Vampire", 2, "Gains x0.1 Mult per scoring Enhanced card played; removes card enhancement"),
            J("j_shortcut", "Shortcut", 2, "Allows Straights to be made with gaps of 1 rank"),
            J("j_hologram", "Hologram", 2, "Gains x0.25 Mult every time a playing card is added to the deck"),
            J("j_cloud_9", "Cloud 9", 2, "Earn $1 for each 9 in the full deck at end of round"),
            J("j_rocket", "Rocket", 2, "Earn $1 at end of round; payout increases by $2 when Boss Blind is defeated"),
            J("j_midas_mask", "Midas Mask", 2, "All played face cards become Gold cards when scored"),
            J("j_luchador", "Luchador", 2, "Sell this card to disable the current Boss Blind"),
            J("j_gift", "Gift Card", 2, "Add $1 of sell value to every Joker and Consumable card at end of round"),
            J("j_turtle_bean", "Turtle Bean", 2, "+5 hand size, reduces by 1 each round"),
            J("j_erosion", "Erosion", 2, "+4 Mult for each card below the starting deck size"),
            J("j_to_the_moon", "To the Moon", 2, "Earn an extra $1 of interest for every $5 you have at end of round"),
            J("j_stone", "Stone Joker", 2, "+25 Chips for each Stone Card in the full deck"),
            J("j_lucky_cat", "Lucky Cat", 2, "Gains x0.25 Mult every time a Lucky card successfully triggers"),
            J("j_bull", "Bull", 2, "+2 Chips for every $1 you have"),
            J("j_diet_cola", "Diet Cola", 2, "Sell this card to create a free Double Tag"),
            J("j_trading", "Trading Card", 2, "If first discard of round has only 1 card, destroy it and earn $3"),
            J("j_flash", "Flash Card", 2, "Gains +2 Mult per reroll in the shop"),
            J("j_trousers", "Spare Trousers", 2, "Gains +2 Mult if played hand contains a Two Pair"),
            J("j_ramen", "Ramen", 2, "x2 Mult, loses x0.01 Mult per card discarded"),
            J("j_selzer", "Seltzer", 2, "Retrigger all played cards for the next 10 hands"),
            J("j_castle", "Castle", 2, "Gains +3 Chips per discarded card of the listed suit; suit changes every round"),
            J("j_mr_bones", "Mr. Bones", 2, "Prevents death if chips scored are at least 25% of required chips; self destructs"),
            J("j_acrobat", "Acrobat", 2, "x3 Mult on final hand of the round"),
            J("j_sock_and_buskin", "Sock and Buskin", 2, "Retrigger all played face cards"),
            J("j_troubadour", "Troubadour", 2, "+2 hand size, -1 hand per round"),
            J("j_certificate", "Certificate", 2, "When round begins, add a random playing card with a random seal to your hand"),
            J("j_smeared", "Smeared Joker", 2, "Hearts and Diamonds count as the same suit, Spades and Clubs count as the same suit"),
            J("j_throwback", "Throwback", 2, "x0.25 Mult for each Blind skipped this run"),
            J("j_rough_gem", "Rough Gem", 2, "Played cards with Diamond suit earn $1 when scored"),
            J("j_bloodstone", "Bloodstone", 2, "1 in 2 chance for played cards with Heart suit to give x1.5 Mult when scored"),
            J("j_arrowhead", "Arrowhead", 2, "Played cards with Spade suit give +50 Chips when scored"),
            J("j_onyx_agate", "Onyx Agate", 2, "Played cards with Club suit give +7 Mult when scored"),
            J("j_glass", "Glass Joker", 2, "Gains x0.75 Mult for every Glass Card that is destroyed"),
            J("j_ring_master", "Showman", 2, "Joker, Tarot, Planet and Spectral cards may appear multiple times"),
            J("j_flower_pot", "Flower Pot", 2, "x3 Mult if poker hand contains a Diamond, Club, Heart and Spade card"),
            J("j_merry_andy", "Merry Andy", 2, "+3 discards each round, -1 hand size"),
            J("j_oops", "Oops! All 6s", 2, "Doubles all listed probabilities (e.g. 1 in 3 becomes 2 in 3)"),
            J("j_idol", "The Idol", 2, "Each played card of the listed rank and suit gives x2 Mult when scored; card changes every round"),
            J("j_seeing_double", "Seeing Double", 2, "x2 Mult if played hand has a scoring Club card and a scoring card of any other suit"),
            J("j_matador", "Matador", 2, "Earn $8 if played hand triggers the Boss Blind ability"),
            J("j_satellite", "Satellite", 2, "Earn $1 at end of round per unique Planet card used this run"),
            J("j_cartomancer", "Cartomancer", 2, "Create a Tarot card when Blind is selected (must have room)"),
            J("j_astronomer", "Astronomer", 2, "All Planet cards and Celestial Packs in the shop are free"),
            J("j_bootstraps", "Bootstraps", 2, "+2 Mult for every $5 you have"),
        };

        public static readonly IReadOnlyList<JokerDef> RareJokers = new[]
        {
            J("j_dna", "DNA", 3, "If first hand of round has only 1 card, add a permanent copy to deck and draw it to hand"),
            J("j_vagabond", "Vagabond", 3, "Create a Tarot card if hand is played with $4 or less"),
            J("j_baron", "Baron", 3, "Each King held in hand gives x1.5 Mult"),
            J("j_obelisk", "Obelisk", 3, "Gains x0.2 Mult per consecutive hand played without playing your most played poker hand"),
            J("j_baseball", "Baseball Card", 3, "Uncommon Jokers each give x1.5 Mult"),
            J("j_ancient", "Ancient Joker", 3, "Each played card of the listed suit gives x1.5 Mult when scored; suit changes every round"),
            J("j_campfire", "Campfire", 3, "Gains x0.25 Mult for each card sold; resets when Boss Blind is defeated"),
            J("j_blueprint", "Blueprint", 3, "Copies the ability of the Joker to the right"),
            J("j_wee", "Wee Joker", 3, "Gains +8 Chips when each played 2 is scored"),
            J("j_hit_the_road", "Hit the Road", 3, "Gains x0.5 Mult for every Jack discarded this round"),
            J("j_duo", "The Duo", 3, "x2 Mult if played hand contains a Pair"),
            J("j_trio", "The Trio", 3, "x3 Mult if played hand contains a Three of a Kind"),
            J("j_family", "The Family", 3, "x4 Mult if played hand contains a Four of a Kind"),
            J("j_order", "The Order", 3, "x3 Mult if played hand contains a Straight"),
            J("j_tribe", "The Tribe", 3, "x2 Mult if played hand contains a Flush"),
            J("j_stuntman", "Stuntman", 3, "+250 Chips, -2 hand size"),
            J("j_invisible", "Invisible Joker", 3, "After 2 rounds, sell this card to duplicate a random Joker"),
            J("j_brainstorm", "Brainstorm", 3, "Copies the ability of the leftmost Joker"),
            J("j_drivers_license", "Driver's License", 3, "x3 Mult if you have at least 16 Enhanced cards in your full deck"),
            J("j_burnt", "Burnt Joker", 3, "Upgrade the level of the first discarded poker hand each round"),
        };

        public static readonly IReadOnlyList<JokerDef> LegendaryJokers = new[]
        {
            J("j_caino", "Canio", 4, "Gains x1 Mult when a face card is destroyed"),
            J("j_triboulet", "Triboulet", 4, "Played Kings and Queens each give x2 Mult when scored"),
            J("j_yorick", "Yorick", 4, "Gains x1 Mult every 23 cards discarded"),
            J("j_chicot", "Chicot", 4, "Disables the effect of every Boss Blind"),
            J("j_perkeo", "Perkeo", 4, "Creates a Negative copy of 1 random consumable card when leaving the shop"),
        };

        public static readonly IReadOnlyDictionary<string, JokerDef> JokersByKey =
            CommonJokers.Concat(UncommonJokers).Concat(RareJokers).Concat(LegendaryJokers)
                .ToDictionary(j => j.Key, j => j, StringComparer.Ordinal);

        /// <summary>Jokers that can never roll the Eternal sticker.</summary>
        public static readonly IReadOnlySet<string> EternalIncompatible = new HashSet<string>(StringComparer.Ordinal)
        {
            "j_gros_michel", "j_ice_cream", "j_cavendish", "j_luchador", "j_turtle_bean",
            "j_diet_cola", "j_popcorn", "j_ramen", "j_selzer", "j_mr_bones", "j_invisible",
        };

        /// <summary>Jokers that can never roll the Perishable sticker.</summary>
        public static readonly IReadOnlySet<string> PerishableIncompatible = new HashSet<string>(StringComparer.Ordinal)
        {
            "j_ceremonial", "j_ride_the_bus", "j_runner", "j_constellation", "j_green_joker",
            "j_red_card", "j_madness", "j_square", "j_vampire", "j_rocket", "j_obelisk",
            "j_lucky_cat", "j_flash", "j_trousers", "j_castle", "j_wee",
        };

        /// <summary>Jokers locked on a fresh profile until their unlock condition is met (meta.jkr lifts these).</summary>
        public static readonly IReadOnlySet<string> DefaultLockedJokers = new HashSet<string>(StringComparer.Ordinal)
        {
            "j_ticket", "j_mr_bones", "j_acrobat", "j_sock_and_buskin", "j_swashbuckler",
            "j_troubadour", "j_certificate", "j_smeared", "j_throwback", "j_hanging_chad",
            "j_rough_gem", "j_bloodstone", "j_arrowhead", "j_onyx_agate", "j_glass",
            "j_ring_master", "j_flower_pot", "j_blueprint", "j_wee", "j_merry_andy",
            "j_oops", "j_idol", "j_seeing_double", "j_matador", "j_hit_the_road",
            "j_duo", "j_trio", "j_family", "j_order", "j_tribe", "j_stuntman",
            "j_invisible", "j_brainstorm", "j_satellite", "j_shoot_the_moon",
            "j_drivers_license", "j_cartomancer", "j_astronomer", "j_burnt", "j_bootstraps",
        };

        /// <summary>Run-gated jokers: only in the pool once the deck contains the enhancement.</summary>
        public static readonly IReadOnlyDictionary<string, string> EnhancementGates = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["j_stone"] = "m_stone",
            ["j_steel_joker"] = "m_steel",
            ["j_glass"] = "m_glass",
            ["j_ticket"] = "m_gold",
            ["j_lucky_cat"] = "m_lucky",
        };

        private static ConsumableDef C(string key, string name, string text) => new(key, name, text);

        public static readonly IReadOnlyList<ConsumableDef> Tarots = new[]
        {
            C("c_fool", "The Fool", "Creates the last Tarot or Planet card used this run (The Fool excluded)"),
            C("c_magician", "The Magician", "Enhances 2 selected cards to Lucky Cards"),
            C("c_high_priestess", "The High Priestess", "Creates up to 2 random Planet cards"),
            C("c_empress", "The Empress", "Enhances 2 selected cards to Mult Cards"),
            C("c_emperor", "The Emperor", "Creates up to 2 random Tarot cards"),
            C("c_heirophant", "The Hierophant", "Enhances 2 selected cards to Bonus Cards"),
            C("c_lovers", "The Lovers", "Enhances 1 selected card to a Wild Card"),
            C("c_chariot", "The Chariot", "Enhances 1 selected card to a Steel Card"),
            C("c_justice", "Justice", "Enhances 1 selected card to a Glass Card"),
            C("c_hermit", "The Hermit", "Doubles money (max +$20)"),
            C("c_wheel_of_fortune", "The Wheel of Fortune", "1 in 4 chance to add Foil, Holographic or Polychrome to a random Joker"),
            C("c_strength", "Strength", "Raises the rank of up to 2 selected cards by 1"),
            C("c_hanged_man", "The Hanged Man", "Destroys up to 2 selected cards"),
            C("c_death", "Death", "Select 2 cards: converts the left card into the right card"),
            C("c_temperance", "Temperance", "Gives the total sell value of all current Jokers (max $50)"),
            C("c_devil", "The Devil", "Enhances 1 selected card to a Gold Card"),
            C("c_tower", "The Tower", "Enhances 1 selected card to a Stone Card"),
            C("c_star", "The Star", "Converts up to 3 selected cards to Diamonds"),
            C("c_moon", "The Moon", "Converts up to 3 selected cards to Clubs"),
            C("c_sun", "The Sun", "Converts up to 3 selected cards to Hearts"),
            C("c_judgement", "Judgement", "Creates a random Joker (must have room)"),
            C("c_world", "The World", "Converts up to 3 selected cards to Spades"),
        };

        public static readonly IReadOnlyList<ConsumableDef> Planets = new[]
        {
            C("c_mercury", "Mercury", "Levels up Pair"),
            C("c_venus", "Venus", "Levels up Three of a Kind"),
            C("c_earth", "Earth", "Levels up Full House"),
            C("c_mars", "Mars", "Levels up Four of a Kind"),
            C("c_jupiter", "Jupiter", "Levels up Flush"),
            C("c_saturn", "Saturn", "Levels up Straight"),
            C("c_uranus", "Uranus", "Levels up Two Pair"),
            C("c_neptune", "Neptune", "Levels up Straight Flush"),
            C("c_pluto", "Pluto", "Levels up High Card"),
            C("c_planet_x", "Planet X", "Levels up Five of a Kind"),
            C("c_ceres", "Ceres", "Levels up Flush House"),
            C("c_eris", "Eris", "Levels up Flush Five"),
        };

        /// <summary>Secret planets and the poker hand that must be discovered first.</summary>
        public static readonly IReadOnlyDictionary<string, string> SecretPlanetGates = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["c_planet_x"] = "Five of a Kind",
            ["c_ceres"] = "Flush House",
            ["c_eris"] = "Flush Five",
        };

        /// <summary>
        /// The spectral pool has 18 slots; The Soul and Black Hole sit in it as
        /// permanently unavailable placeholders (they always trigger a resample).
        /// </summary>
        public static readonly IReadOnlyList<ConsumableDef> Spectrals = new[]
        {
            C("c_familiar", "Familiar", "Destroy 1 random card in hand, add 3 random Enhanced face cards"),
            C("c_grim", "Grim", "Destroy 1 random card in hand, add 2 random Enhanced Aces"),
            C("c_incantation", "Incantation", "Destroy 1 random card in hand, add 4 random Enhanced numbered cards"),
            C("c_talisman", "Talisman", "Adds a Gold Seal to 1 selected card"),
            C("c_aura", "Aura", "Adds Foil, Holographic or Polychrome to 1 selected card in hand"),
            C("c_wraith", "Wraith", "Creates a random Rare Joker, sets money to $0"),
            C("c_sigil", "Sigil", "Converts all cards in hand to a single random suit"),
            C("c_ouija", "Ouija", "Converts all cards in hand to a single random rank, -1 hand size"),
            C("c_ectoplasm", "Ectoplasm", "Adds Negative to a random Joker, -1 hand size"),
            C("c_immolate", "Immolate", "Destroys 5 random cards in hand, gain $20"),
            C("c_ankh", "Ankh", "Creates a copy of a random Joker, destroys the others"),
            C("c_deja_vu", "Déjà Vu", "Adds a Red Seal to 1 selected card"),
            C("c_hex", "Hex", "Adds Polychrome to a random Joker, destroys the others"),
            C("c_trance", "Trance", "Adds a Blue Seal to 1 selected card"),
            C("c_medium", "Medium", "Adds a Purple Seal to 1 selected card"),
            C("c_cryptid", "Cryptid", "Creates 2 exact copies of 1 selected card"),
            null, // The Soul placeholder — always resampled
            null, // Black Hole placeholder — always resampled
        };

        public static readonly ConsumableDef TheSoul = C("c_soul", "The Soul", "Creates a Legendary Joker (must have room)");
        public static readonly ConsumableDef BlackHole = C("c_black_hole", "Black Hole", "Upgrades every poker hand by 1 level");

        public static readonly IReadOnlyDictionary<string, ConsumableDef> ConsumablesByKey =
            Tarots.Concat(Planets).Concat(Spectrals.Where(s => s != null)).Concat(new[] { TheSoul, BlackHole })
                .ToDictionary(c => c.Key, c => c, StringComparer.Ordinal);

        public static readonly IReadOnlyList<ConsumableDef> Enhancements = new[]
        {
            C("m_bonus", "Bonus Card", "+30 extra Chips"),
            C("m_mult", "Mult Card", "+4 Mult"),
            C("m_wild", "Wild Card", "Counts as every suit"),
            C("m_glass", "Glass Card", "x2 Mult, 1 in 4 chance to be destroyed"),
            C("m_steel", "Steel Card", "x1.5 Mult while in hand"),
            C("m_stone", "Stone Card", "+50 Chips, no rank or suit"),
            C("m_gold", "Gold Card", "$3 if held in hand at end of round"),
            C("m_lucky", "Lucky Card", "1 in 5 chance for +20 Mult, 1 in 15 chance for $20"),
        };

        public static readonly IReadOnlyDictionary<string, ConsumableDef> EnhancementsByName =
            Enhancements.ToDictionary(e => e.Name, e => e, StringComparer.OrdinalIgnoreCase);

        private static VoucherDef V(string key, string name, string text) => new(key, name, text);

        public static readonly IReadOnlyList<VoucherDef> Vouchers = new[]
        {
            V("v_overstock_norm", "Overstock", "+1 card slot in the shop"),
            V("v_overstock_plus", "Overstock Plus", "+1 card slot in the shop"),
            V("v_clearance_sale", "Clearance Sale", "All cards and packs in shop are 25% off"),
            V("v_liquidation", "Liquidation", "All cards and packs in shop are 50% off"),
            V("v_hone", "Hone", "Foil, Holographic and Polychrome cards appear 2x more often"),
            V("v_glow_up", "Glow Up", "Foil, Holographic and Polychrome cards appear 4x more often"),
            V("v_reroll_surplus", "Reroll Surplus", "Rerolls cost $2 less"),
            V("v_reroll_glut", "Reroll Glut", "Rerolls cost an additional $2 less"),
            V("v_crystal_ball", "Crystal Ball", "+1 consumable slot"),
            V("v_omen_globe", "Omen Globe", "Spectral cards may appear in any Arcana Pack"),
            V("v_telescope", "Telescope", "Celestial Packs always contain the Planet card for your most played poker hand"),
            V("v_observatory", "Observatory", "Planet cards in your consumable area give x1.5 Mult for their poker hand"),
            V("v_grabber", "Grabber", "Permanently gain +1 hand per round"),
            V("v_nacho_tong", "Nacho Tong", "Permanently gain an additional +1 hand per round"),
            V("v_wasteful", "Wasteful", "Permanently gain +1 discard each round"),
            V("v_recyclomancy", "Recyclomancy", "Permanently gain an additional +1 discard each round"),
            V("v_tarot_merchant", "Tarot Merchant", "Tarot cards appear 2x more frequently in the shop"),
            V("v_tarot_tycoon", "Tarot Tycoon", "Tarot cards appear 4x more frequently in the shop"),
            V("v_planet_merchant", "Planet Merchant", "Planet cards appear 2x more frequently in the shop"),
            V("v_planet_tycoon", "Planet Tycoon", "Planet cards appear 4x more frequently in the shop"),
            V("v_seed_money", "Seed Money", "Raise the cap on interest earned each round to $10"),
            V("v_money_tree", "Money Tree", "Raise the cap on interest earned each round to $20"),
            V("v_blank", "Blank", "Does nothing?"),
            V("v_antimatter", "Antimatter", "+1 Joker slot"),
            V("v_magic_trick", "Magic Trick", "Playing cards can be purchased from the shop"),
            V("v_illusion", "Illusion", "Playing cards in shop may be Enhanced, Editioned and/or have a Seal"),
            V("v_hieroglyph", "Hieroglyph", "-1 Ante, -1 hand each round"),
            V("v_petroglyph", "Petroglyph", "-1 Ante, -1 discard each round"),
            V("v_directors_cut", "Director's Cut", "Reroll Boss Blind 1 time per Ante, $10 per roll"),
            V("v_retcon", "Retcon", "Reroll Boss Blind unlimited times, $10 per roll"),
            V("v_paint_brush", "Paint Brush", "+1 hand size"),
            V("v_palette", "Palette", "+1 hand size again"),
        };

        public static readonly IReadOnlyDictionary<string, VoucherDef> VouchersByKey =
            Vouchers.ToDictionary(v => v.Key, v => v, StringComparer.Ordinal);

        /// <summary>
        /// Upgrade-tier voucher → its base voucher. The list is ordered in
        /// base/upgrade pairs; an upgrade only enters the pool once its base
        /// has been redeemed this run (and it is unlocked in the profile).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> UpgradeVoucherBase = BuildUpgradeVoucherBase();

        private static IReadOnlyDictionary<string, string> BuildUpgradeVoucherBase()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 1; i < Vouchers.Count; i += 2)
                map[Vouchers[i].Key] = Vouchers[i - 1].Key;
            return map;
        }

        /// <summary>
        /// The 52 playing-card fronts in pool order (sorted by key, as the game
        /// sorts P_CARDS): all Clubs, Diamonds, Hearts, Spades — with the odd
        /// alphabetical rank order 2..9, A, J, K, Q, T.
        /// </summary>
        public static readonly IReadOnlyList<string> CardKeys = BuildCardKeys();

        private static IReadOnlyList<string> BuildCardKeys()
        {
            var suits = new[] { "C", "D", "H", "S" };
            var ranks = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "A", "J", "K", "Q", "T" };
            return suits.SelectMany(s => ranks.Select(r => $"{s}_{r}")).ToList();
        }

        public static string CardDisplayName(string cardKey)
        {
            if (string.IsNullOrEmpty(cardKey) || cardKey.Length < 3)
                return cardKey;

            var suit = cardKey[0] switch
            {
                'C' => "Clubs",
                'D' => "Diamonds",
                'H' => "Hearts",
                'S' => "Spades",
                _ => "?"
            };
            var rank = cardKey.Substring(2) switch
            {
                "T" => "10",
                "J" => "Jack",
                "Q" => "Queen",
                "K" => "King",
                "A" => "Ace",
                var r => r
            };
            return $"{rank} of {suit}";
        }

        /// <summary>
        /// Booster pack weighted pool in game order. Weights collapse the art
        /// variants (e.g. 4 normal Arcana arts at weight 1 each = 4).
        /// Total weight: 22.42.
        /// </summary>
        public static readonly IReadOnlyList<PackDef> Packs = new[]
        {
            new PackDef("p_arcana_normal", "Arcana Pack", "Arcana", 3, 1, 4),
            new PackDef("p_arcana_jumbo", "Jumbo Arcana Pack", "Arcana", 5, 1, 2),
            new PackDef("p_arcana_mega", "Mega Arcana Pack", "Arcana", 5, 2, 0.5),
            new PackDef("p_celestial_normal", "Celestial Pack", "Celestial", 3, 1, 4),
            new PackDef("p_celestial_jumbo", "Jumbo Celestial Pack", "Celestial", 5, 1, 2),
            new PackDef("p_celestial_mega", "Mega Celestial Pack", "Celestial", 5, 2, 0.5),
            new PackDef("p_standard_normal", "Standard Pack", "Standard", 3, 1, 4),
            new PackDef("p_standard_jumbo", "Jumbo Standard Pack", "Standard", 5, 1, 2),
            new PackDef("p_standard_mega", "Mega Standard Pack", "Standard", 5, 2, 0.5),
            new PackDef("p_buffoon_normal", "Buffoon Pack", "Buffoon", 2, 1, 1.2),
            new PackDef("p_buffoon_jumbo", "Jumbo Buffoon Pack", "Buffoon", 4, 1, 0.6),
            new PackDef("p_buffoon_mega", "Mega Buffoon Pack", "Buffoon", 4, 2, 0.15),
            new PackDef("p_spectral_normal", "Spectral Pack", "Spectral", 2, 1, 0.6),
            new PackDef("p_spectral_jumbo", "Jumbo Spectral Pack", "Spectral", 4, 1, 0.3),
            new PackDef("p_spectral_mega", "Mega Spectral Pack", "Spectral", 4, 2, 0.07),
        };

        public static PackDef PackFromCenterKey(string centerKey)
        {
            if (string.IsNullOrEmpty(centerKey))
                return null;
            return Packs.FirstOrDefault(p => centerKey.StartsWith(p.KeyPrefix, StringComparison.Ordinal));
        }
    }
}
