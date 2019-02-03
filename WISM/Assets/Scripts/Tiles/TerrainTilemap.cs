using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using BranallyGames.Wism;
using System;

public class TerrainTilemap : MonoBehaviour
{
    public World world;
    public GameObject hero;

    void Start()
    {
        CreateWorld();
    }

    private void CreateWorld()
    {
        int boundsX, boundsY;
        TileBase[] allTiles = GetTiles(out boundsX, out boundsY);

        MapBuilder.Initialize(@"Assets\Scripts\Core\mod");
        BranallyGames.Wism.Tile[,] gameMap = new BranallyGames.Wism.Tile[boundsX, boundsY];
        for (int y = 0; y < boundsY; y++)
        {
            for (int x = 0; x < boundsX; x++)
            {
                TileBase unityTile = allTiles[x + y * boundsX];
                BranallyGames.Wism.Tile gameTile = new BranallyGames.Wism.Tile();
                gameMap[x, y] = gameTile;

                if (unityTile != null)
                {
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
                    // Null or empty tiles are "Void"
                    gameTile.Terrain = MapBuilder.TerrainKinds["V"];
                }
            }
        }

        //MapBuilder.AffixMapObjects(gameMap);
        for (int y = 0; y < gameMap.GetLength(1); y++)
        {
            for (int x = 0; x < gameMap.GetLength(0); x++)
            {
                // Affix gameMap objects and coordinates with tile
                BranallyGames.Wism.Tile tile = gameMap[x, y];
                tile.Coordinate = new Coordinate(x, y);
                if (tile.Army != null)
                    tile.Army.Tile = tile;
                if (tile.Terrain != null)
                    tile.Terrain.Tile = tile;
            }
        }
        World.CreateWorld(gameMap);
        this.world = World.Current;

        // TODO: Draw the units on the map
        DrawArmies();
    }

    private void DrawArmies()
    {
        Tilemap tilemap = GetComponent<Tilemap>();
        int x = -15, y = -13, z = 1;
        Vector3 worldVector = tilemap.CellToWorld(new Vector3Int(x, y, z));
        Debug.Log(String.Format("Cell ({0}, {1}, {2}); World ({3}, {4}, {5})",
            x, y, z, worldVector.x, worldVector.y, worldVector.z));

        Instantiate(hero, worldVector, Quaternion.identity);

        foreach (Player player in World.Current.Players)
        {
            Debug.Log("Player: " + player.Affiliation.DisplayName);
            foreach (Army army in player.GetArmies())
            {                
                Coordinate coord = army.GetCoordinates();
                worldVector = tilemap.CellToWorld(new Vector3Int(coord.X, coord.Y, 1));
                Debug.Log("Army: " + army.DisplayName + ", @(" + coord.X + ", " + coord.Y + ")");
                worldVector.x = worldVector.x + x;
                worldVector.y = worldVector.y + y;
                Instantiate(hero, worldVector, Quaternion.identity);
            }
        }
    }

    private TileBase[] GetTiles(out int xSize, out int ySize)
    {
        // Constrain bounds
        int maxSize = 1000;
        Tilemap tilemap = GetComponent<Tilemap>();

        tilemap.CompressBounds();
        xSize = Mathf.Min(tilemap.size.x, maxSize);
        ySize = Mathf.Min(tilemap.size.y, maxSize);
        tilemap.size = new Vector3Int(xSize, ySize, 1);
        tilemap.ResizeBounds();
        
        return tilemap.GetTilesBlock(tilemap.cellBounds);
    }
}
