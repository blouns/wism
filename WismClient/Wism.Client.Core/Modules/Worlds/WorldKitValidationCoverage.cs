using System.Collections.Generic;

namespace Wism.Client.Modules.Worlds
{
    public sealed class WorldKitValidationCoverage
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int TileCount { get; set; }
        public int ExpectedTileCount { get; set; }
        public int CityCount { get; set; }
        public int LocationCount { get; set; }
        public int ClansWithStarts { get; set; }
        public int RequestedPlayers { get; set; }
        public int ReachableCityPairs { get; set; }
        public int TotalCityPairs { get; set; }
        public bool Loadable { get; set; }
        public IDictionary<string, int> TerrainCounts { get; set; } = new Dictionary<string, int>();
    }
}
