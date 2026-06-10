using System.Collections.Generic;
using UnityEngine;

namespace WismCompanion.UI
{
    /// <summary>
    /// Loads and caches Warlords Classic sprite textures from Resources/Sprites/.
    /// All lookups return null on miss so callers can fall back to color rendering.
    /// </summary>
    public static class SpriteRegistry
    {
        // Quadrant indices: 0=TopLeft, 1=TopRight, 2=BottomLeft, 3=BottomRight
        private static readonly string[] CityQuadrantNames = { "top_left", "top_right", "bottom_left", "bottom_right" };

        private static readonly Dictionary<string, Texture2D> Cache = new();
        private static bool _loaded;

        // Maps 4-bit NESW road-connectivity mask (0-15) to road sprite index (0-14).
        // Derived directly from WismUnity RoadTile.GetTileData if-else chain.
        // Grid layout: [1]=W, [3]=S, [5]=N, [7]=E  (x-outer, y-inner iteration, Y-up tilemap)
        // Encoding: bit3=N, bit2=E, bit1=S, bit0=W
        private static readonly int[] RoadSpriteByMask =
        {
             1, // 0000 → isolated         → default East-west (sprite 1)
             0, // 0001 → W only           → East end
             2, // 0010 → S only           → North end
             8, // 0011 → S+W             → South-west corner
            14, // 0100 → E only           → West end
             1, // 0101 → E+W             → East-west
             7, // 0110 → S+E             → South-east corner
            13, // 0111 → S+E+W           → T west-south-east
             6, // 1000 → N only           → South end
             5, // 1001 → N+W             → North-west corner
             4, // 1010 → N+S             → North-south
            12, // 1011 → N+S+W           → T west-north-south
             3, // 1100 → N+E             → North-east corner
            11, // 1101 → N+E+W           → T west-north-east
             9, // 1110 → N+S+E           → T north-east-south
            10, // 1111 → all             → Crossroads
        };

        public static Texture2D GetTerrain(string terrainType, int adjacencyIndex)
        {
            EnsureLoaded();
            switch (MapColors.CleanTerrainName(terrainType))
            {
                case "Grass":   return Load("terrain/grass");
                case "Marsh":   return Load("terrain/marsh");
                case "Forest":  return Load($"terrain/forest_{adjacencyIndex}");
                case "Mountain":
                    // Mountain has no index-13 single sprite; use index 5 (middle) as fallback
                    return Load(adjacencyIndex <= 12 ? $"terrain/mountain_{adjacencyIndex}" : "terrain/mountain_5");
                case "Water":
                    // Water has no index-13 single sprite; use index 5 (middle) as fallback
                    return Load(adjacencyIndex <= 12 ? $"terrain/water_{adjacencyIndex}" : "terrain/water_5");
                case "Hill":
                    return Load($"terrain/hills_{Mathf.Clamp(adjacencyIndex, 0, 4)}");
                // Road uses the road sprite set; adjacencyIndex carries the 4-bit NESW mask.
                case "Road":
                    return GetRoad(adjacencyIndex);
                default:
                    return null;
            }
        }

        /// <param name="neswMask">4-bit mask: bit3=N, bit2=E, bit1=S, bit0=W indicating which
        /// neighboring tiles are also road or bridge.</param>
        public static Texture2D GetRoad(int neswMask)
        {
            EnsureLoaded();
            var idx = RoadSpriteByMask[Mathf.Clamp(neswMask, 0, 15)];
            return Load($"terrain/road_{idx}");
        }

        /// Selects a bridge sprite using the 3-way B/R/W neighbor classification from BridgeTile.cs.
        /// Grid: [1]=W, [3]=S, [5]=N, [7]=E  (same x-outer, y-inner scan as RoadTile)
        /// <param name="bridgeMask">4-bit NESW: which cardinal neighbors are Bridge tiles.</param>
        /// <param name="roadMask">4-bit NESW: which cardinal neighbors are Road tiles.</param>
        public static Texture2D GetBridge(int bridgeMask, int roadMask)
        {
            EnsureLoaded();
            bool nB = (bridgeMask & 8) != 0, eB = (bridgeMask & 4) != 0;
            bool sB = (bridgeMask & 2) != 0, wB = (bridgeMask & 1) != 0;
            bool nR = (roadMask   & 8) != 0, eR = (roadMask   & 4) != 0;
            bool sR = (roadMask   & 2) != 0, wR = (roadMask   & 1) != 0;

            // Mirrors BridgeTile.GetTileData: [1]=W, [3]=S, [5]=N, [7]=E
            if (eB && wR) return Load("terrain/road_15"); // EW-left:   [7]=B(E), [1]=R(W)
            if (nB && sR) return Load("terrain/road_16"); // NS-bottom: [5]=B(N), [3]=R(S)
            if (sB && nR) return Load("terrain/road_17"); // NS-top:    [3]=B(S), [5]=R(N)
            if (wB && eR) return Load("terrain/road_18"); // EW-right:  [1]=B(W), [7]=R(E)
            return Load("terrain/road_18");               // default fallback
        }

        public static Texture2D GetLocation(string locationType)
        {
            EnsureLoaded();
            switch (MapColors.CleanTerrainName(locationType))
            {
                case "Ruins":  return Load("location/ruins");
                case "Temple": return Load("location/temple");
                case "Tomb":   return Load("location/tomb");
                case "Tower":  return Load("location/tower");
                default:       return null;
            }
        }

        /// <param name="quadrant">0=TL, 1=TR, 2=BL, 3=BR</param>
        public static Texture2D GetCity(string clanName, int quadrant)
        {
            EnsureLoaded();
            if (quadrant < 0 || quadrant > 3) return null;
            var key = NormalizeClan(clanName);
            return Load($"city/{key}_castle_{CityQuadrantNames[quadrant]}");
        }

        public static Texture2D GetArmy(string clanName, bool isHero)
        {
            EnsureLoaded();
            var key = NormalizeClan(clanName);
            return isHero ? Load($"army/{key}_hero") : Load($"army/{key}_infantry");
        }

        /// <param name="count">Stack depth 1–8</param>
        public static Texture2D GetFlag(string clanName, int count)
        {
            EnsureLoaded();
            var key = NormalizeClan(clanName);
            var n = Mathf.Clamp(count, 1, 8);
            return Load($"army/{key}_flag_{n}");
        }

        private static string NormalizeClan(string clan)
        {
            if (string.IsNullOrWhiteSpace(clan)) return "neutral";
            return clan.Trim().ToLowerInvariant() switch
            {
                "sirians"    => "sirians",
                "stormgiants" => "stormgiants",
                "greydwarves" => "greydwarves",
                "orcsofkor"  => "orcsofkor",
                "elvallie"   => "elvallie",
                "selentines" => "selentines",
                "horselords" => "horselords",
                "lordbane"   => "lordbane",
                _            => "neutral"
            };
        }

        private static Texture2D Load(string path)
        {
            if (Cache.TryGetValue(path, out var cached)) return cached;
            var tex = Resources.Load<Texture2D>($"Sprites/{path}");
            Cache[path] = tex; // cache null results too to avoid repeated failed lookups
            return tex;
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
        }
    }
}
