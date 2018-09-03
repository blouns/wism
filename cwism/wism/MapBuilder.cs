using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wism
{
    public static class MapBuilder
    {
        private static Dictionary<char, Terrain> terrainKinds = new Dictionary<char, Terrain>();

        static MapBuilder()
        {
            LoadTerrain();
        }

        private static void LoadTerrain()
        {
            IList<Terrain> terrains = ModFactory.LoadTerrains(ModFactory.DefaultPath);
            foreach (Terrain t in terrains)
                terrainKinds.Add(t.Symbol, t);
        }

        private static readonly char[,] defaultMapRepresentation =
        {
            { 'M', 'M', 'M', 'M', 'M' },
            { 'M', 'm', 'm', 'm', 'M' },
            { 'M', 'm', 'F', 'm', 'M' },
            { 'M', 'm', 'm', 'm', 'M' },
            { 'M', 'M', 'M', 'M', 'M' }
        };

        public static char[,] DefaultMapRepresentation => defaultMapRepresentation;

        public static Terrain[,] GenerateMap(char[,] mapRepresentation)
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
                    char symbol = mapRepresentation[i, j];
                    Terrain terrain = terrainKinds[symbol];
                    map[i, j] = terrain;
                }
            }

            return map;
        }
    }    
}