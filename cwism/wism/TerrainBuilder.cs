using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism
{
    public static class TerrainBuilder
    {
        private static List<Terrain> terrainKinds = new List<Terrain>();

        static TerrainBuilder()
        {
            LoadTerrain();
        }

        private static void LoadTerrain()
        {
            IList<Terrain> terrains = ModFactory.LoadTerrains(ModFactory.DefaultPath);
            terrainKinds.AddRange(terrains);            
        }

        private static readonly int[,] defaultMapRepresentation =
        {
            { 1, 1, 1, 1, 1 },
            { 1, 2, 2, 2, 1 },
            { 1, 2, 3, 2, 1 },
            { 1, 2, 2, 2, 1 },
            { 1, 1, 1, 1, 1 }
        };

        public static int[,] DefaultMapRepresentation => defaultMapRepresentation;

        public static Terrain[,] GenerateMap(int[,] mapRepresentation)
        {
            if (mapRepresentation == null)
                throw new ArgumentNullException("mapRepresentation", "The map representation must not be null.");

            int height = mapRepresentation.GetLength(0);
            int width = mapRepresentation.GetLength(1);

            Terrain[,] map = new Terrain[height, width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    map[i, j] = terrainKinds[mapRepresentation[i, j]];
                }
            }

            return map;
        }
    }    
}