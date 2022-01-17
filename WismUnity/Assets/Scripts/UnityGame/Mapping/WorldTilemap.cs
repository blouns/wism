using Assets.Scripts.Common;
using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Wism.Client.Core;
using Wism.Client.Modules;
using Terrain = Wism.Client.MapObjects.Terrain;
using Tile = Wism.Client.Core.Tile;

namespace Assets.Scripts.Tilemaps
{
    public class WorldTilemap : MonoBehaviour
    {
        private Tilemap tileMap;
        private GameObject searchGO;

        public void Start()
        {
            tileMap = transform.GetComponent<Tilemap>();
        }

        public World CreateWorldFromScene()
        {
            var unityManagerGO = UnityUtilities.GameObjectHardFind("UnityManager");
            var debugManager = unityManagerGO.GetComponent<DebugManager>();
            var gameManager = unityManagerGO.GetComponent<GameManager>();

            MapBuilder.Initialize(gameManager.ModPath, gameManager.WorldName);
            debugManager.LogInformation("Initialized MapBuilder");

            TileBase[] tilemapTiles = GetUnityTiles(out int boundsX, out int boundsY);
            Tile[,] gameMap = new Tile[boundsX, boundsY];

            for (int y = 0; y < boundsY; y++)
            {
                for (int x = 0; x < boundsX; x++)
                {
                    TileBase unityTile = tilemapTiles[x + y * boundsX];
                    Tile gameTile = new Tile();
                    gameMap[x, y] = gameTile;

                    if (unityTile != null)
                    {
                        foreach (Terrain terrain in MapBuilder.TerrainKinds.Values)
                        {
                            if (unityTile.name.ToLowerInvariant().Contains(terrain.ShortName.ToLowerInvariant()))
                            {
                                gameTile.Terrain = terrain;
                                break;
                            }
                        }

                        if (gameTile.Terrain == null)
                        {
                            throw new InvalidOperationException("Failed to create world; unknown terrain type: " + unityTile.name);
                        }
                    }
                    else
                    {
                        // Null or empty tiles are "Void"
                        gameTile.Terrain = MapBuilder.TerrainKinds["Void"];
                    }
                }
            }

            debugManager.LogInformation("Loaded map from Unity Tiles");
            MapBuilder.AffixMapObjects(gameMap);

            debugManager.LogInformation("Affixed map objects");
            World.CreateWorld(gameMap);
            debugManager.LogInformation("Created world from game map");
            World.Current.Name = GameManager.DefaultWorld;

            return World.Current;
        }

        internal List<(int,int)> GetLocationsWithCityTerrain()
        {
            var tilemapTiles = GetUnityTiles(out int boundsX, out int boundsY);
            var tiles = new List<(int,int)>();
            for (int y = 0; y < boundsY; y++)
            {
                for (int x = 0; x < boundsX; x++)
                {
                    var unityTile = tilemapTiles[x + y * boundsX];                    

                    if (unityTile != null && 
                        unityTile.name.ToLowerInvariant().Contains("castle"))
                    {
                        tiles.Add((x,y));
                    }
                }
            }

            return tiles;
        }

        public void ShowSearchIcon(int x, int y)
        {
            this.searchGO = UnityUtilities.GameObjectHardFind("Search");
            searchGO.transform.position = ConvertGameToUnityVector(x, y);
            searchGO.SetActive(true);
        }

        public void HideSearchIcon()
        {
            if (this.searchGO)
            {
                this.searchGO.SetActive(false);
            }
        }

        internal Tilemap GetTilemap()
        {
            return tileMap;
        }

        internal void RefreshTile(Vector3 position)
        {
            tileMap.RefreshTile(tileMap.WorldToCell(position));
        }

        internal void SetTile(Vector3 position, TileBase tile)
        {
            tileMap.SetTile(tileMap.WorldToCell(position), tile);
        }

        public Vector3 ConvertGameToUnityVector(int gameX, int gameY)
        {
            return MapUtilities.ConvertGameToUnityVector(gameX, gameY, this);            
        }

        public Vector2Int ConvertUnityToGameVector(Vector3 worldVector)
        {
            return MapUtilities.ConvertUnityToGameVector(worldVector);
        }

        private TileBase[] GetUnityTiles(out int xSize, out int ySize)
        {
            // Constrain bounds
            const int maxSize = 1000;
            Tilemap tilemap = GetComponent<Tilemap>();

            tilemap.CompressBounds();
            xSize = Mathf.Min(tilemap.size.x, maxSize);
            ySize = Mathf.Min(tilemap.size.y, maxSize);
            tilemap.size = new Vector3Int(xSize, ySize, 1);
            tilemap.ResizeBounds();

            return tilemap.GetTilesBlock(tilemap.cellBounds);
        }

        public Tile GetClickedTile(Camera followCamera)
        {
            Vector3 worldPoint = followCamera.ScreenToWorldPoint(Input.mousePosition);
            var gameCoord = ConvertUnityToGameVector(worldPoint);
            Tile gameTile = World.Current.Map[gameCoord.x, gameCoord.y];

            return gameTile;
        }
    }
}