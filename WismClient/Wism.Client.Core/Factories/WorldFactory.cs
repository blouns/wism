using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Entities;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Factories
{
    public static class WorldFactory
    {
        public static World Load(WorldEntity snapshot, 
            out Dictionary<int, Tile> armiesNameTileDict, 
            out Dictionary<int, Tile> visitingNameTileDict)
        {
            // Load Tiles            
            int xMax = snapshot.MapXUpperBound;
            int yMax = snapshot.MapYUpperBound;
            Tile[,] map = new Tile[xMax, yMax];

            armiesNameTileDict = new Dictionary<int, Tile>();
            visitingNameTileDict = new Dictionary<int, Tile>();
            var locationNameTileDict = new Dictionary<string, Tile>();

            for (int y = 0; y < yMax; y++)
            {
                for (int x = 0; x < xMax; x++)
                {
                    var tileEntity = snapshot.Tiles[x + y * xMax];

                    // Tile details
                    Tile tile = new Tile();                    
                    tile.X = tileEntity.X;
                    tile.Y = tileEntity.Y;
                    tile.Terrain = MapBuilder.TerrainKinds[tileEntity.TerrainShortName];

                    // Items
                    if (tileEntity.Items != null)
                    {
                        tile.Items = new List<Artifact>();
                        foreach (var artifact in tileEntity.Items)
                        {
                            tile.Items.Add(ArtifactFactory.Load(artifact, tile));
                        }
                    }

                    // Locations (circular reference; added to Tile during Location creation)

                    // City no-op (circular reference; added to Tile during City creation)                    

                    // Armies (circular reference; add after army creation)
                    if (tileEntity.ArmyIds != null)
                    {
                        foreach (var armyId in tileEntity.ArmyIds)
                        {
                            if (!armiesNameTileDict.ContainsKey(armyId))
                            {
                                armiesNameTileDict.Add(armyId, tile);
                            }
                        }
                    }

                    // Visiting Armies (circular reference; add after army creation)
                    if (tileEntity.VisitingArmyIds != null)
                    {
                        foreach (var armyId in tileEntity.VisitingArmyIds)
                        {
                            if (!visitingNameTileDict.ContainsKey(armyId))
                            {
                                visitingNameTileDict.Add(armyId, tile);
                            }
                        }
                    }

                    map[x, y] = tile;
                }
            }

            World.CreateWorld(map);
            var world = World.Current;
            world.Name = snapshot.Name;

            // Load late-bound Locations (after world creation) 
            if (snapshot.Locations != null)
            {
                foreach (var locationEntity in snapshot.Locations)
                {
                    _ = LocationFactory.Load(locationEntity, world);
                }
            }

            return world;
        }

        public static World Create(WorldEntity worldEntity)
        {
            // Create Tiles            
            int xMax = worldEntity.MapXUpperBound;
            int yMax = worldEntity.MapYUpperBound;
            Tile[,] map = new Tile[xMax, yMax];

            for (int y = 0; y < yMax; y++)
            {
                for (int x = 0; x < xMax; x++)
                {
                    var tileEntity = worldEntity.Tiles[x + y * xMax];

                    // Tile details
                    Tile tile = new Tile();
                    tile.X = tileEntity.X;
                    tile.Y = tileEntity.Y;
                    tile.Terrain = MapBuilder.TerrainKinds[tileEntity.TerrainShortName];

                    map[x, y] = tile;
                }
            }

            World.CreateWorld(map);
            var world = World.Current;
            world.Name = worldEntity.Name;

            // Load late-bound Locations (after world creation) 
            if (worldEntity.Locations != null)
            {
                foreach (var locationEntity in worldEntity.Locations)
                {
                    _ = LocationFactory.Create(locationEntity, world);
                }
            }

            // Load late-bound Cities (after world creation) 
            if (worldEntity.Cities != null)
            {
                foreach (var cityEntity in worldEntity.Cities)
                {
                    _ = CityFactory.Create(cityEntity, world);
                }
            }

            return world;
        }

        private static Tile[,] CreateTiles(WorldEntity worldEntity)
        {
            int xMax = worldEntity.MapXUpperBound;
            int yMax = worldEntity.MapYUpperBound;
            Tile[,] map = new Tile[xMax, yMax];
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    var entity = worldEntity.Tiles[i * xMax + j];
                    var gameTile = new Tile();
                    gameTile.X = i;
                    gameTile.Y = j;

                    if (entity != null)
                    {
                        foreach (Terrain terrain in MapBuilder.TerrainKinds.Values)
                        {
                            if (entity.TerrainShortName.ToLowerInvariant().Contains(terrain.ShortName.ToLowerInvariant()))
                            {
                                gameTile.Terrain = terrain;
                                break;
                            }
                        }

                        if (gameTile.Terrain == null)
                        {
                            throw new InvalidOperationException("Failed to create world; unknown terrain type: " + entity.TerrainShortName);
                        }
                    }
                    else
                    {
                        // Null or empty tiles are "Void"
                        gameTile.Terrain = MapBuilder.TerrainKinds["Void"];
                    }
                }
            }

            return map;
        }
    }
}