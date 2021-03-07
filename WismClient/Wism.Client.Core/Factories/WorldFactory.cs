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
            out Dictionary<string, Tile> cityNameTileDict, 
            out Dictionary<string, Tile> armiesNameTileDict, 
            out Dictionary<string, Tile> visitingNameTileDict)
        {
            // Load Tiles            
            int xMax = snapshot.Tiles.GetUpperBound(0) + 1;
            int yMax = snapshot.Tiles.GetUpperBound(1) + 1;            
            Tile[,] map = new Tile[xMax, yMax];

            cityNameTileDict = new Dictionary<string, Tile>();
            armiesNameTileDict = new Dictionary<string, Tile>();
            visitingNameTileDict = new Dictionary<string, Tile>();
            var locationNameTileDict = new Dictionary<string, Tile>();

            for (int y = 0; y < yMax; y++)
            {
                for (int x = 0; x < xMax; x++)
                {
                    var tileEntity = snapshot.Tiles[x, y];

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

                    // Locations
                    if (!String.IsNullOrWhiteSpace(tileEntity.LocationShortName))
                    {
                        locationNameTileDict.Add(tileEntity.LocationShortName, tile);
                    }

                    // City (circular reference; add later)
                    if (!String.IsNullOrWhiteSpace(tileEntity.CityShortName))
                    {
                        cityNameTileDict.Add(tileEntity.CityShortName, tile);
                    }

                    // Armies (circular reference; add later)
                    foreach (var armyName in tileEntity.ArmyShortNames)
                    {
                        armiesNameTileDict.Add(armyName, tile);
                    }

                    // Visiting Armies (circular reference; add later)
                    foreach (var visitingName in tileEntity.VisitingArmyShortNames)
                    {
                        visitingNameTileDict.Add(visitingName, tile);
                    }


                    map[x, y] = tile;
                }
            }

            World.CreateWorld(map);
            var world = World.Current;

            // Load late-bound Locations           
            foreach (var locationEntity in snapshot.Locations)
            {
                world.AddLocation(
                    LocationFactory.Load(locationEntity),
                    locationNameTileDict[locationEntity.LocationShortName]);
            }

            return world;
        }
    }
}