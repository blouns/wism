using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using System;
using Wism.Client.Entities;

namespace Assets.Scripts.UnityGame.Factories
{
    public class UnityWorldFactory
    {
        private DebugManager debugManager;

        public UnityWorldFactory(DebugManager debugManager)
        {
            this.debugManager = debugManager ?? throw new ArgumentNullException(nameof(debugManager));
        }

        /// <summary>
        /// Creates a WorldEntity from a scene
        /// </summary>
        /// <param name="worldName">World name</param>
        /// <param name="tilemap">WorldTilemap from scene</param>
        /// <returns>WorldEntity from a scene</returns>
        public WorldEntity CreateWorld(string worldName, WorldTilemap tilemap)
        {
            if (string.IsNullOrWhiteSpace(worldName))
            {
                throw new System.ArgumentException($"'{nameof(worldName)}' cannot be null or whitespace.", nameof(worldName));
            }

            if (tilemap is null)
            {
                throw new ArgumentNullException(nameof(tilemap));
            }

            WorldEntity entity = new WorldEntity();

            entity.Name = worldName;
            entity.Tiles = CreateTiles(worldName, tilemap, out int xUpperBound, out int yUpperBound);
            entity.MapXUpperBound = xUpperBound;
            entity.MapYUpperBound = yUpperBound;

            // TODO: Load the Scene as opposed to using 'current' scene
            var cityFactory = new UnityCityFactory(debugManager);
            entity.Cities = cityFactory.CreateCities(tilemap);

            var locationFactory = new UnityLocationFactory(debugManager);
            entity.Locations = locationFactory.CreateLocations(tilemap);

            return entity;
        }

        private TileEntity[] CreateTiles(string worldName, WorldTilemap tilemap, out int xUpperBound, out int yUpperBound)
        {            
            var map = tilemap.CreateWorldFromScene(worldName).Map;
            xUpperBound = map.GetUpperBound(0);
            yUpperBound = map.GetUpperBound(1);

            // Flatten 2D-array
            TileEntity[] tiles = new TileEntity[xUpperBound * yUpperBound];
            for (int y = 0; y < yUpperBound; y++)
            {
                for (int x = 0; x < xUpperBound; x++)
                {
                    var tile = map[x, y];
                    tiles[x + y * xUpperBound] = new TileEntity()
                    {
                        // Only set terrain and location; other details added later
                        TerrainShortName = tile.Terrain.ShortName,
                        X = tile.X,
                        Y = tile.Y
                    };
                }
            }

            return tiles;
        }        
    }
}
