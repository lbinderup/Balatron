using System;
using System.Collections.Generic;
using System.Globalization;

namespace Balatron.Services.Rng
{
    /// <summary>
    /// Port of Balatro's pseudohash / pseudoseed / pseudorandom machinery
    /// (functions/misc_functions.lua). One instance represents a snapshot of
    /// G.GAME.pseudorandom taken from a save file; every call advances the
    /// per-key counters exactly like the running game would.
    /// </summary>
    public sealed class BalatroRng
    {
        private readonly Dictionary<string, double> _counters;
        private readonly string _seed;
        private readonly double _hashedSeed;

        public BalatroRng(string seed, IReadOnlyDictionary<string, double> counters)
        {
            _seed = seed ?? string.Empty;
            // hashed_seed is stored in the save with limited precision;
            // recompute it from the seed string for full accuracy.
            _hashedSeed = Pseudohash(_seed);
            _counters = counters == null
                ? new Dictionary<string, double>(StringComparer.Ordinal)
                : new Dictionary<string, double>(counters, StringComparer.Ordinal);
        }

        /// <summary>Balatro's pseudohash(str).</summary>
        public static double Pseudohash(string s)
        {
            double num = 1;
            for (var i = s.Length - 1; i >= 0; i--)
            {
                num = Fract(1.1239285023 / num * s[i] * Math.PI + Math.PI * (i + 1));
            }
            return num;
        }

        // Lua's a % 1 (floored), not C#'s truncated %.
        private static double Fract(double value) => value - Math.Floor(value);

        // Replicates tonumber(string.format("%.13f", value)).
        private static double Round13(double value)
        {
            return double.Parse(value.ToString("F13", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        /// <summary>Balatro's pseudoseed(key): advances the per-key counter and returns a seed for LuaRandom.</summary>
        public double PseudoSeed(string key)
        {
            if (!_counters.TryGetValue(key, out var current))
                current = Pseudohash(key + _seed);

            current = Math.Abs(Round13(Fract(current * 1.72431234 + 2.134453429141)));
            _counters[key] = current;
            return (current + _hashedSeed) / 2.0;
        }

        /// <summary>A generator seeded from the key, for effects that draw repeatedly (shuffles).</summary>
        public LuaRandom Generator(string key) => new(PseudoSeed(key));

        /// <summary>pseudorandom(key): one uniform double in [0, 1).</summary>
        public double Random(string key) => new LuaRandom(PseudoSeed(key)).NextDouble();

        /// <summary>pseudorandom(key, min, max).</summary>
        public int RandomInt(string key, int min, int max) => new LuaRandom(PseudoSeed(key)).NextInt(min, max);

        /// <summary>
        /// pseudorandom_element over a pre-sorted pool (Balatro pools are already
        /// in deterministic sort order). Returns the chosen index (0-based).
        /// </summary>
        public int ChooseIndex(string key, int poolSize) => new LuaRandom(PseudoSeed(key)).NextInt(1, poolSize) - 1;

        /// <summary>Direct read access for diagnostics.</summary>
        public IReadOnlyDictionary<string, double> Counters => _counters;
    }
}
