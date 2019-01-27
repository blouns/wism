using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using BranallyGames.Wism;
using System;

public class TerrainTilemap : MonoBehaviour
{
    public World world;

    void Start()
    {
        CreateWorld();
    }

    private void CreateWorld()
    {        
        int boundsX, boundsY;
        TileBase[] allTiles = GetTiles(out boundsX, out boundsY);
        BranallyGames.Wism.Tile[,] gameMap = new BranallyGames.Wism.Tile[boundsX, boundsY];
        for (int x = 0; x < boundsX; x++)
        {
            for (int y = 0; y < boundsY; y++)
            {
                TileBase unityTile = allTiles[x + y * boundsX];
                BranallyGames.Wism.Tile gameTile = new BranallyGames.Wism.Tile();
                gameMap[x, y] = gameTile;

                if (unityTile != null)
                {
                    Debug.Log("x:" + x + " y:" + y + " tile:" + unityTile.name);
                    // TODO: Refactor
                    if (unityTile.name.Contains("grass"))
                    {
                        gameTile.Terrain = MapBuilder.TerrainKinds["G"];
                    }
                    else if (unityTile.name.Contains("forest"))
                    {
                        gameTile.Terrain = MapBuilder.TerrainKinds["F"];
                    }
                    else if (unityTile.name.Contains("hill"))
                    {
                        gameTile.Terrain = MapBuilder.TerrainKinds["h"];
                    }
                    else if (unityTile.name.Contains("water"))
                    {
                        gameTile.Terrain = MapBuilder.TerrainKinds["W"];
                    }
                    else if (unityTile.name.Contains("mountain"))
                    {
                        gameTile.Terrain = MapBuilder.TerrainKinds["M"];
                    }
                    else if (unityTile.name.Contains("marsh"))
                    {
                        gameTile.Terrain = MapBuilder.TerrainKinds["m"];
                    }
                    else if (unityTile.name.Contains("road"))
                    {
                        gameTile.Terrain = MapBuilder.TerrainKinds["R"];
                    }
                    else if (unityTile.name.Contains("bridge"))
                    {
                        gameTile.Terrain = MapBuilder.TerrainKinds["B"];
                    }
                }
                else
                {
                    Debug.Log("x:" + x + " y:" + y + " tile: (null)");

                    // Null or empty tiles are "Void"
                    gameTile.Terrain = MapBuilder.TerrainKinds["V"];
                }
            }
        }

        MapBuilder.AffixMapObjects(gameMap);
        World.CreateWorld(gameMap);
        this.world = World.Current;
    }

    private TileBase[] GetTiles(out int xSize, out int ySize)
    {
        // Constrain bounds
        int maxSize = 10000;
        Tilemap tilemap = GetComponent<Tilemap>();

        tilemap.CompressBounds();
        xSize = Mathf.Min(tilemap.size.x, maxSize);
        ySize = Mathf.Min(tilemap.size.y, maxSize);
        tilemap.size = new Vector3Int(xSize, ySize, 1);
        tilemap.ResizeBounds();
        
        return tilemap.GetTilesBlock(tilemap.cellBounds);
    }
}
