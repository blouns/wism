using System;

namespace Wism.Companion.Shared.Models
{
    /// <summary>
    ///     A per-cell snapshot of the AI's forward-feed influence field, sent to the companion's
    ///     spatial overlay. Stored row-major: <c>index = (y * Width) + x</c>.
    /// </summary>
    /// <remarks>
    ///     Observation-only and public-safe: every value is derived from visible board state (force
    ///     and tension per cell), so it carries no AI decision data, orchestration, or routing.
    ///     Channels mirror the advisor's calibrated outputs — tension in [-1, 1], friendly/enemy in
    ///     [0, 1].
    /// </remarks>
    public class InfluenceFieldDto
    {
        public int Width { get; set; }

        public int Height { get; set; }

        /// <summary>Signed friendly-minus-enemy tension in [-1, 1]; the headline overlay channel.</summary>
        public float[] Tension { get; set; } = Array.Empty<float>();

        /// <summary>Calibrated friendly influence in [0, 1].</summary>
        public float[] Friendly { get; set; } = Array.Empty<float>();

        /// <summary>Calibrated enemy influence in [0, 1].</summary>
        public float[] Enemy { get; set; } = Array.Empty<float>();

        /// <summary>Row-major index of cell (x, y) into the channel arrays.</summary>
        public int IndexOf(int x, int y) => (y * Width) + x;
    }
}
