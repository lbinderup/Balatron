using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Balatron.Models;

namespace Balatron.Services.Cards
{
    /// <summary>
    /// Builds a save-ready card node from a center definition, mirroring the
    /// game's Card:set_ability plus the skeleton Card:save writes. Card:load
    /// takes the ability table verbatim, so it has to match what the game
    /// would have produced.
    /// </summary>
    public static class CardFactory
    {
        /// <summary>Sort ids are per-run and only used for ordering; keep new cards last.</summary>
        private const int GeneratedSortIdBase = 900000;
        private static int _sortIdCounter;

        public static LuaNode Create(CenterDef center)
        {
            if (center == null)
                throw new ArgumentNullException(nameof(center));

            var card = Table(null, null);
            var sellCost = Math.Max(1, center.Cost / 2);

            Value(card, "sort_id", (GeneratedSortIdBase + _sortIdCounter++).ToString(CultureInfo.InvariantCulture));
            Value(card, "label", Quote(center.Name));
            Value(card, "base_cost", center.Cost.ToString(CultureInfo.InvariantCulture));
            Value(card, "cost", center.Cost.ToString(CultureInfo.InvariantCulture));
            Value(card, "extra_cost", "0");
            Value(card, "sell_cost", sellCost.ToString(CultureInfo.InvariantCulture));
            Value(card, "rank", "1");
            Value(card, "facing", Quote("front"));
            Value(card, "sprite_facing", Quote("front"));
            Value(card, "debuff", "false");
            Value(card, "added_to_deck", "true");

            // Without these the game re-runs discovery/unlock checks on load and
            // can refuse to show a card the profile has never seen.
            Value(card, "bypass_discovery_center", "true");
            Value(card, "bypass_discovery_ui", "true");
            Value(card, "bypass_lock", "true");

            var saveFields = Table(card, "save_fields");
            Value(saveFields, "center", Quote(center.Key));

            var parameters = Table(card, "params");
            Value(parameters, "discover", "false");
            Value(parameters, "bypass_discovery_center", "true");
            Value(parameters, "bypass_discovery_ui", "true");

            // Jokers carry a zeroed base block; it only matters for playing cards.
            var baseNode = Table(card, "base");
            Value(baseNode, "nominal", "0");
            Value(baseNode, "suit_nominal", "0");
            Value(baseNode, "face_nominal", "0");
            Value(baseNode, "times_played", "0");

            BuildAbility(card, center);
            return card;
        }

        /// <summary>Card:set_ability — every field is config.X with a fixed default.</summary>
        private static void BuildAbility(LuaNode card, CenterDef center)
        {
            var ability = Table(card, "ability");
            var config = center.Config ?? new Dictionary<string, object>();

            Value(ability, "name", Quote(center.Name));
            if (center.Effect != null)
                Value(ability, "effect", Quote(center.Effect));
            Value(ability, "set", Quote(center.Set));

            Value(ability, "mult", Number(config, "mult", 0));
            Value(ability, "h_mult", Number(config, "h_mult", 0));
            Value(ability, "h_x_mult", Number(config, "h_x_mult", 0));
            Value(ability, "h_dollars", Number(config, "h_dollars", 0));
            Value(ability, "p_dollars", Number(config, "p_dollars", 0));
            Value(ability, "t_mult", Number(config, "t_mult", 0));
            Value(ability, "t_chips", Number(config, "t_chips", 0));
            Value(ability, "x_mult", Number(config, "Xmult", 1));
            Value(ability, "h_size", Number(config, "h_size", 0));
            Value(ability, "d_size", Number(config, "d_size", 0));
            Value(ability, "extra_value", "0");
            Value(ability, "type", config.TryGetValue("type", out var type) ? Quote(type?.ToString()) : "\"\"");
            Value(ability, "order", center.Order.ToString(CultureInfo.InvariantCulture));
            Value(ability, "perma_bonus", "0");
            Value(ability, "bonus", Number(config, "bonus", 0));
            Value(ability, "hands_played_at_create", "0");

            if (config.TryGetValue("extra", out var extra) && extra != null)
                WriteExtra(ability, extra);

            // Consumables keep their whole config under `consumeable`.
            if (center.IsConsumable || center.IsVoucher)
            {
                var consumable = Table(ability, "consumeable");
                foreach (var (key, value) in config)
                    WriteConfigValue(consumable, key, value);
            }

            ApplyPerCardExtras(ability, center);
        }

        private static void WriteExtra(LuaNode ability, object extra)
        {
            if (extra is IReadOnlyDictionary<string, object> table)
            {
                var node = Table(ability, "extra");
                foreach (var (key, value) in table)
                    WriteConfigValue(node, key, value);
            }
            else
            {
                Value(ability, "extra", Scalar(extra));
            }
        }

        private static void WriteConfigValue(LuaNode parent, string key, object value)
        {
            if (value is IReadOnlyDictionary<string, object> nested)
            {
                var node = Table(parent, key);
                foreach (var (k, v) in nested)
                    WriteConfigValue(node, k, v);
            }
            else if (value != null)
            {
                Value(parent, key, Scalar(value));
            }
        }

        /// <summary>The handful of jokers set_ability seeds with extra state.</summary>
        private static void ApplyPerCardExtras(LuaNode ability, CenterDef center)
        {
            switch (center.Name)
            {
                case "Invisible Joker":
                    Value(ability, "invis_rounds", "0");
                    break;
                case "Caino":
                    Value(ability, "caino_xmult", "1");
                    break;
                case "Yorick":
                    Value(ability, "yorick_discards", "0");
                    break;
            }
        }

        private static string Number(IReadOnlyDictionary<string, object> config, string key, double fallback)
        {
            var value = config.TryGetValue(key, out var raw) && raw is double d ? d : fallback;
            return value == Math.Floor(value)
                ? ((long)value).ToString(CultureInfo.InvariantCulture)
                : value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Scalar(object value) => value switch
        {
            double d => d == Math.Floor(d)
                ? ((long)d).ToString(CultureInfo.InvariantCulture)
                : d.ToString("0.###", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            string s => Quote(s),
            _ => "0"
        };

        private static string Quote(string value) => "\"" + (value ?? string.Empty) + "\"";

        private static LuaNode Table(LuaNode parent, string key)
        {
            var node = new LuaNode { Key = key, Parent = parent, IsTable = true, ForceQuotedKey = key != null };
            parent?.Children.Add(node);
            return node;
        }

        private static void Value(LuaNode parent, string key, string value)
        {
            parent.Children.Add(new LuaNode
            {
                Key = key,
                Parent = parent,
                Value = value,
                ForceQuotedKey = true
            });
        }
    }
}
