using Assets.Scripts.Managers;
using System;
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

        public void Start()
        {
            tileMap = transform.GetComponent<Tilemap>();
        }

        public World CreateWorldFromScene()
        {
            MapBuilder.Initialize(GameManager.DefaultModPath);

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

            MapBuilder.AffixMapObjects(gameMap);
            World.CreateWorld(gameMap);

            return World.Current;
        }

        internal void RefreshTile(Vector3 position)
        {
            tileMap.RefreshTile(tileMap.WorldToCell(position));
        }

        internal void SetTile(Vector3 position, TileBase tile)
        {
            tileMap.SetTile(tileMap.WorldToCell(position), tile);
        }

        internal Vector3 ConvertGameToUnityCoordinates(int gameX, int gameY)
        {
            this.tileMap = transform.GetComponent<Tilemap>();
            Vector3 worldVector = tileMap.CellToWorld(new Vector3Int(gameX + 1, gameY + 1, 0));
            worldVector.x += tileMap.cellBounds.xMin - tileMap.tileAnchor.x;
            worldVector.y += tileMap.cellBounds.yMin - tileMap.tileAnchor.y;
            return worldVector;
        }

        internal (int, int) ConvertUnityToGameCoordinates(Vector3 worldVector)
        {
            // TODO: Add support to adjust to tilemap coordinates in case
            //       the tilemap is translated to another location. 
            return (Mathf.FloorToInt(worldVector.x), Mathf.FloorToInt(worldVector.y));
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
            var gameCoord = ConvertUnityToGameCoordinates(worldPoint);
            Tile gameTile = World.Current.Map[gameCoord.Item1, gameCoord.Item2];

            MoveCrosshairs(gameCoord);

            return gameTile;
        }

        // TODO: Move to Crosshairs or Minimap
        private static void MoveCrosshairs((int, int) gameCoord)
        {
            var crossGO = UnityUtilities.GameObjectHardFind("Crosshairs");
            var crossRect = crossGO.GetComponent<RectTransform>();
            var minimapPanelRect = UnityUtilities.GameObjectHardFind("MinimapPanel").GetComponent<RectTransform>();
            
            float newXPercent = ((float)gameCoord.Item1) / ((float)World.Current.Map.GetUpperBound(0));
            float newYPercent = ((float)gameCoord.Item2) / ((float)World.Current.Map.GetUpperBound(1));
            float newX = minimapPanelRect.sizeDelta.x * newXPercent - (minimapPanelRect.sizeDelta.x / 2f);
            float newY = minimapPanelRect.sizeDelta.y * newYPercent - (minimapPanelRect.sizeDelta.y / 2f);

            // TODO: Clamp to minimap
            crossRect.localPosition = new Vector3(newX, newY, 0f);
        }
    }
}