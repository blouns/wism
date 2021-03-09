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
                        foreach (var armyName in tileEntity.ArmyIds)
                        {
                            armiesNameTileDict.Add(armyName, tile);
                        }
                    }

                    // Visiting Armies (circular reference; add after army creation)
                    if (tileEntity.VisitingArmyIds != null)
                    {
                        foreach (var visitingName in tileEntity.VisitingArmyIds)
                        {
                            visitingNameTileDict.Add(visitingName, tile);
                        }
                    }

                    map[x, y] = tile;
                }
            }

            World.CreateWorld(map);
            var world = World.Current;
            world.Name = snapshot.Name;

            // Load late-bound Locations (after world creation)          
            foreach (var locationEntity in snapshot.Locations)
            {
                _ = LocationFactory.Load(locationEntity, world);
            }

            return world;
        }
    }
}