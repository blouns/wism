// File: Wism.Client.AI/InfluenceMaps/IInfluenceMap.cs

using Wism.Client.Core;

namespace Wism.Client.AI.InfluenceMaps
{
    public interface IInfluenceMap
    {
        /// <summary>
        /// Update the influence map based on the current game state.
        /// </summary>
        void Update();

        /// <summary>
        /// Get the influence value at a given tile.
        /// </summary>
        double GetInfluence(Tile tile);
    }
}
