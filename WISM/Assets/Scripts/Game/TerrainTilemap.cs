using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using BranallyGames.Wism;
using System;
using Assets.Scripts.Units;
using System.IO;

public class TerrainTilemap : MonoBehaviour
{
    private World world;
    private Tilemap tileMap;
    private IList<Player> players;
    public GameObject[] armyKinds;

    void Start()
    {        
        tileMap = transform.GetComponent<Tilemap>();
        world = CreateWorld();
        players = ReadyPlayerOne();

        DrawArmies(players);
    }

    private IList<Player> ReadyPlayerOne()
    {
        // Temp: add armies to map for testing
        Player player1 = World.Current.Players[0];
        player1.Affiliation.IsHuman = true;        
        player1.HireHero(World.Current.Map[2, 2]);
        player1.ConscriptUnit(ModFactory.FindUnitInfo("LightInfantry"), World.Current.Map[2, 3]);
        player1.ConscriptUnit(ModFactory.FindUnitInfo("Cavalry"), World.Current.Map[3, 2]);
        player1.ConscriptUnit(ModFactory.FindUnitInfo("Pegasus"), World.Current.Map[9, 17]);

        return World.Current.Players;
    }
   
    void FixedUpdate()
    {
        //if (army != null)
        //{
        //    if (army.Affiliation.IsHuman)
        //    {
        //        if (Input.GetKeyDown(KeyCode.DownArrow))
        //        {
        //            bool moved = army.TryMove(Direction.South);
        //            if (moved)
        //            {
        //                Debug.Log(army.DisplayName + ": Moved south");
        //            }
        //            else
        //            {
        //                Debug.Log(army.DisplayName + ": Moved blocked");
        //            }
        //        }
        //    }
        //}
    }

    private World CreateWorld()
    {
        MapBuilder.Initialize(GameManager.DefaultModPath);

        TileBase[] tilemapTiles = GetTiles(out int boundsX, out int boundsY);
        BranallyGames.Wism.Tile[,] gameMap = new BranallyGames.Wism.Tile[boundsX, boundsY];

        for (int y = 0; y < boundsY; y++)
        {
            for (int x = 0; x < boundsX; x++)
            {
                TileBase unityTile = tilemapTiles[x + y * boundsX];
                BranallyGames.Wism.Tile gameTile = new BranallyGames.Wism.Tile();
                gameMap[x, y] = gameTile;

                if (unityTile != null)
                {
                    foreach (BranallyGames.Wism.Terrain terrain in MapBuilder.TerrainKinds.Values)
                    {
                        if (unityTile.name.ToLowerInvariant().Contains(terrain.DisplayName.ToLowerInvariant()))
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

    private void DrawArmies(IList<Player> players)
    {
        foreach (Player player in players)
        {
            Debug.Log("Player: " + player.Affiliation.DisplayName);
            foreach (Army army in player.GetArmies())
            {
                Coordinate coord = army.GetCoordinates();
                float gameToUnityMappingX = coord.X + tileMap.cellBounds.xMin - .5f;
                float gameToUnityMappingY = coord.Y + tileMap.cellBounds.yMin - .5f;
                Vector3 worldVector = new Vector3(gameToUnityMappingX, gameToUnityMappingY, 0.0f);
                Debug.Log(
                    String.Format(
                        "Army: {0} WISM:({1}, {2}), Unity:({3}, {4})",
                        army.DisplayName, coord.X, coord.Y, worldVector.x, worldVector.y));

                InstantiateArmy(army, worldVector);
            }
        }
    }

    private void InstantiateArmy(Army army, Vector3 worldVector)
    {
        if (armyKinds == null || armyKinds.Length == 0)
        {
            Debug.Log("No army kinds found.");
            return;
        }

        ArmyFactory factory = ArmyFactory.Create(armyKinds);
        GameObject gameObject = factory.FindGameObject(army);
        if (gameObject == null)
        {
            Debug.Log(String.Format("GameObject not found: {0}_{1}", army.ID, army.Affiliation.ID));
            return;
        }

        Debug.Log(String.Format("Instantiating: {0}_{1}", army.ID, army.Affiliation.ID));
        Instantiate(gameObject, worldVector, Quaternion.identity);
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
