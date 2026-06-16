// File: Wism.Client.AI/InfluenceMaps/ISpatialAdvisor.cs

using Wism.Client.Core;

namespace Wism.Client.AI.InfluenceMaps
{
    /// <summary>
    ///     Read-only spatial advisor: a terrain-aware influence field that ADAPTA modules
    ///     query for friendly/enemy force, tension (the front line), and movement gradient.
    ///     Extends <see cref="IInfluenceMap"/> for source compatibility with the existing seam.
    /// </summary>
    /// <remarks>
    ///     Prototype for Recommendation 1 / Workstream A of the forward-feed influence-map
    ///     whitepaper. The field is keyed by (x, y) coordinates rather than <see cref="Tile"/>
    ///     identity so it survives world cloning/reconstruction during evals. All channels are
    ///     calibrated to 0..1 against a fixed reference; tension is friendly minus enemy in
    ///     [-1, 1]. Raw (un-normalized) force is available for callers that need magnitude.
    /// </remarks>
    public interface ISpatialAdvisor : IInfluenceMap
    {
        /// <summary>Calibrated friendly influence at the tile, in [0, 1].</summary>
        double GetFriendly(Tile tile);

        /// <summary>Calibrated enemy influence at the tile, in [0, 1].</summary>
        double GetEnemy(Tile tile);

        /// <summary>Tension (friendly - enemy) at the tile, in [-1, 1]. Sign indicates control.</summary>
        double GetTension(Tile tile);

        /// <summary>Un-normalized friendly force at the tile (magnitude preserved).</summary>
        double GetRawFriendly(Tile tile);

        /// <summary>Un-normalized enemy force at the tile (magnitude preserved).</summary>
        double GetRawEnemy(Tile tile);

        /// <summary>True when the tile sits on a tension zero-crossing (a contested front).</summary>
        bool IsFrontLine(Tile tile);

        /// <summary>
        ///     Best neighboring tile to step toward. When <paramref name="ascendFriendly"/> is
        ///     true the step climbs the friendly gradient (toward safety/support); otherwise it
        ///     climbs the enemy gradient (toward the enemy). Returns <paramref name="from"/> when
        ///     no neighbor improves.
        /// </summary>
        Tile GetGradientStep(Tile from, bool ascendFriendly);

        // Coordinate overloads — the field is keyed by (x, y), NOT Tile identity, so callers
        // can query a field built for a cloned/reconstructed world by coordinate.

        /// <summary>Calibrated friendly influence at (x, y), in [0, 1]; 0 if out of bounds.</summary>
        double GetFriendly(int x, int y);

        /// <summary>Calibrated enemy influence at (x, y), in [0, 1]; 0 if out of bounds.</summary>
        double GetEnemy(int x, int y);

        /// <summary>Tension (friendly - enemy) at (x, y), in [-1, 1]; 0 if out of bounds.</summary>
        double GetTension(int x, int y);
    }
}
