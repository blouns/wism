// File: Wism.Client.AI/InfluenceMaps/InfluenceFieldExporter.cs

using Wism.Client.Core;
using Wism.Companion.Shared.Models;

namespace Wism.Client.AI.InfluenceMaps
{
    /// <summary>
    ///     Serializes an <see cref="ISpatialAdvisor"/> field into a public-safe
    ///     <see cref="InfluenceFieldDto"/> for the companion spatial overlay (Workstream A3).
    ///     Observation-only: it reads the advisor's calibrated channels and copies them; it never
    ///     influences a decision.
    /// </summary>
    public static class InfluenceFieldExporter
    {
        /// <summary>
        ///     Sample an already-computed advisor over a <paramref name="width"/> × <paramref name="height"/>
        ///     grid into a flat, row-major DTO. Returns null for a missing advisor or non-positive size.
        /// </summary>
        public static InfluenceFieldDto Export(ISpatialAdvisor advisor, int width, int height)
        {
            if (advisor == null || width <= 0 || height <= 0)
            {
                return null;
            }

            var count = width * height;
            var dto = new InfluenceFieldDto
            {
                Width = width,
                Height = height,
                Tension = new float[count],
                Friendly = new float[count],
                Enemy = new float[count]
            };

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var i = (y * width) + x;
                    dto.Tension[i] = (float)advisor.GetTension(x, y);
                    dto.Friendly[i] = (float)advisor.GetFriendly(x, y);
                    dto.Enemy[i] = (float)advisor.GetEnemy(x, y);
                }
            }

            return dto;
        }

        /// <summary>
        ///     Build a field from the current player's perspective off live game state: floods a fresh
        ///     <see cref="ForwardFeedInfluenceMap"/> (deterministic, momentum off) and exports it.
        ///     Returns null when there is no live game/map.
        /// </summary>
        public static InfluenceFieldDto BuildForCurrentPlayer()
        {
            var map = World.Current?.Map;
            if (Game.Current == null || map == null)
            {
                return null;
            }

            // Visualization-tuned flood: a gentler decay spreads influence across far more of the
            // map and a smaller calibration denominator makes typical stacks read clearly. This is
            // a separate instance from the AI's decision field, so it changes only the overlay.
            var advisor = new ForwardFeedInfluenceMap { Lambda = 0.18 };
            advisor.CalibrationReference /= 4.0;
            advisor.Update();
            return Export(advisor, map.GetLength(0), map.GetLength(1));
        }
    }
}
