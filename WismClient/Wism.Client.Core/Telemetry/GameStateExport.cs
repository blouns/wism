using System.Collections.Generic;
using System.Linq;
using Wism.Client.MapObjects;

namespace Wism.Client.Core.Telemetry
{
    public static class GameStateExport
    {
        public static Tile[,] GetMap() => World.Current.Map;

        public static int GetMapWidth() => World.Current.Map.GetUpperBound(0) + 1;
        public static int GetMapHeight() => World.Current.Map.GetUpperBound(1) + 1;

        public static IEnumerable<Tile> GetAllTiles()
        {
            var map = World.Current.Map;
            int width = GetMapWidth();
            int height = GetMapHeight();

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    yield return map[x, y];
        }

        public static IEnumerable<Player> GetPlayers() => Game.Current.Players;

        public static IEnumerable<Army> GetAllArmies() =>
            Game.Current.Players.SelectMany(p => p.GetArmies());

        public static IEnumerable<City> GetCities() =>
            GetAllTiles()
                .Where(t => t.HasCity())
                .Select(t => t.City)
                .Distinct();
    }
}
