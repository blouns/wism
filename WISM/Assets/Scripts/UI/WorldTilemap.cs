using Assets.Scripts.Units;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Wism.Client.Api.CommandProviders;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Terrain = Wism.Client.MapObjects.Terrain;
using Tile = Wism.Client.Core.Tile;

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
        // BUGBUG: This is broken; need to adjust to tilemap coordinates in case
        //       the tilemap is translated to another location. 
        return (Mathf.FloorToInt(worldVector.x), Mathf.FloorToInt(worldVector.y));

        //worldVector.x -= tileMap.cellBounds.xMin + tileMap.tileAnchor.x - 1;
        //worldVector.y -= tileMap.cellBounds.yMin + tileMap.tileAnchor.y - 1;
        //Vector3Int cellVector = tileMap.WorldToCell(worldVector);
        //return new Coordinates(cellVector.x, cellVector.y);
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
        // TODO: Clamp to only positions on the world or UI
        Vector3 point = followCamera.ScreenToWorldPoint(Input.mousePosition);
        var gameCoord = ConvertUnityToGameCoordinates(point);
        Tile gameTile = World.Current.Map[gameCoord.Item1, gameCoord.Item2];

        return gameTile;
    }
}
