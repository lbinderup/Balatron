using System;

namespace Balatron.Services.Rng
{
    /// <summary>
    /// Bit-exact port of LuaJIT's math.random / math.randomseed
    /// (Tausworthe TW223 generator from lj_math / lib_math.c).
    /// Balatro seeds this with the value returned by pseudoseed() for every draw.
    /// </summary>
    public sealed class LuaRandom
    {
        private readonly ulong[] _state = new ulong[4];

        public LuaRandom(double seed)
        {
            uint r = 0x11090601; // 64-k[i] as four 8-bit constants
            for (var i = 0; i < 4; i++)
            {
                var m = 1UL << (int)(r & 255);
                r >>= 8;
                // Two separate operations (multiply, then add) to match the
                // non-fused double rounding of the reference implementation.
                seed = seed * 3.14159265358979323846;
                seed = seed + 2.7182818284590452354;
                var u = (ulong)BitConverter.DoubleToInt64Bits(seed);
                if (u < m) u += m; // ensure k[i] MSB of state[i] are non-zero
                _state[i] = u;
            }

            for (var i = 0; i < 10; i++)
                NextUInt64();
        }

        private ulong NextUInt64()
        {
            ulong z, r = 0;

            z = _state[0];
            z = (((z << 31) ^ z) >> 45) ^ ((z & (ulong.MaxValue << 1)) << 18);
            r ^= z; _state[0] = z;

            z = _state[1];
            z = (((z << 19) ^ z) >> 30) ^ ((z & (ulong.MaxValue << 6)) << 28);
            r ^= z; _state[1] = z;

            z = _state[2];
            z = (((z << 24) ^ z) >> 48) ^ ((z & (ulong.MaxValue << 9)) << 7);
            r ^= z; _state[2] = z;

            z = _state[3];
            z = (((z << 21) ^ z) >> 39) ^ ((z & (ulong.MaxValue << 17)) << 8);
            r ^= z; _state[3] = z;

            return r;
        }

        /// <summary>math.random(): uniform double in [0, 1).</summary>
        public double NextDouble()
        {
            var bits = (NextUInt64() & 0x000FFFFFFFFFFFFFUL) | 0x3FF0000000000000UL;
            return BitConverter.Int64BitsToDouble((long)bits) - 1.0;
        }

        /// <summary>math.random(min, max): uniform integer in [min, max].</summary>
        public int NextInt(int min, int max)
        {
            return (int)(long)(NextDouble() * (max - min + 1)) + min;
        }
    }
}
