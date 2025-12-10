using System;
using System.Collections.Generic;

namespace Balatron.Services
{
    internal static class JokerSpriteTileMap
    {
        public static readonly IReadOnlyDictionary<string, IReadOnlyList<(int Column, int Row)>> Assignments =
            new Dictionary<string, IReadOnlyList<(int Column, int Row)>>(StringComparer.OrdinalIgnoreCase)
            {
                ["j_sock_and_buskin"] = new List<(int, int)> { (3, 1) },
                ["j_bull"] = new List<(int, int)> { (1, 0) },
                ["j_jolly"] = new List<(int, int)> { (2, 0) },
                ["j_square"] = new List<(int, int)> { (3, 0) },
                ["j_perkeo"] = new List<(int, int)> { (7, 8), (7, 9) }, // multi-tile layering example
            };
    }
}
