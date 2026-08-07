using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace Balatron.Services.Cards
{
    /// <summary>
    /// One entry from Balatro's G.P_CENTERS — everything Card:set_ability needs
    /// to build a card. Extracted from the game's own definitions.
    /// </summary>
    public sealed class CenterDef
    {
        public string Key { get; init; }
        public string Name { get; init; }
        public string Set { get; init; }
        public string Effect { get; init; }
        public int Order { get; init; }
        public int Cost { get; init; }
        public int? Rarity { get; init; }

        /// <summary>Raw config table; values are double, string, bool or a nested table.</summary>
        public IReadOnlyDictionary<string, object> Config { get; init; }

        public bool IsJoker => Set == "Joker";
        public bool IsConsumable => Set is "Tarot" or "Planet" or "Spectral";
        public bool IsVoucher => Set == "Voucher";
    }

    /// <summary>Every joker, consumable and voucher the game defines.</summary>
    public static class CenterRegistry
    {
        private static readonly Lazy<IReadOnlyList<CenterDef>> Loaded = new(Load);

        public static IReadOnlyList<CenterDef> All => Loaded.Value;

        public static CenterDef Find(string key) =>
            All.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.Ordinal));

        private static IReadOnlyList<CenterDef> Load()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Balatron;component/Resources/centers.json", UriKind.Absolute);
                using var stream = Application.GetResourceStream(uri)?.Stream;
                if (stream == null)
                    return Array.Empty<CenterDef>();

                using var reader = new StreamReader(stream);
                using var document = JsonDocument.Parse(reader.ReadToEnd());

                return document.RootElement.EnumerateArray()
                    .Select(element => new CenterDef
                    {
                        Key = Text(element, "key"),
                        Name = Text(element, "name"),
                        Set = Text(element, "set"),
                        Effect = Text(element, "effect"),
                        Order = Int(element, "order"),
                        Cost = Int(element, "cost"),
                        Rarity = element.TryGetProperty("rarity", out var r) && r.ValueKind == JsonValueKind.Number
                            ? r.GetInt32()
                            : null,
                        Config = element.TryGetProperty("config", out var config)
                            ? ReadTable(config)
                            : new Dictionary<string, object>()
                    })
                    .ToList();
            }
            catch
            {
                return Array.Empty<CenterDef>();
            }
        }

        private static string Text(JsonElement element, string name) =>
            element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private static int Int(JsonElement element, string name) =>
            element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
                ? value.GetInt32()
                : 0;

        private static IReadOnlyDictionary<string, object> ReadTable(JsonElement element)
        {
            var table = new Dictionary<string, object>(StringComparer.Ordinal);
            if (element.ValueKind != JsonValueKind.Object)
                return table;

            foreach (var property in element.EnumerateObject())
                table[property.Name] = ReadValue(property.Value);
            return table;
        }

        private static object ReadValue(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Object => ReadTable(element),
            _ => null
        };
    }
}
