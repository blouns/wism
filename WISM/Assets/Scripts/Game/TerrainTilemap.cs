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
    private Camera followCamera;
    private World world;
    private Tilemap tileMap;
    private IList<Player> players;
    public GameObject[] armyKinds;
    private IList<ArmyGameObject> instantiatedArmies = new List<ArmyGameObject>();

    private ClickableObject.SelectedState selectedState;
    private ArmyGameObject selectedArmy;

    public ArmyGameObject SelectedArmy { get => selectedArmy; set => selectedArmy = value; }
    public ClickableObject.SelectedState SelectedState { get => selectedState; set => selectedState = value; }

    private void Start()
    {
        SetupCameras();
        tileMap = transform.GetComponent<Tilemap>();
        world = CreateWorldFromScene();
        players = ReadyPlayerOne();

        DrawArmies(players);
    }

    private void SetupCameras()
    {
        foreach (Camera camera in Camera.allCameras)
        {
            if (camera.name == "FollowCamera")
            {
                followCamera = camera;
            }            
        }

        if (followCamera == null)
        {
            throw new InvalidOperationException("Could not find the FollowCamera.");
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            BranallyGames.Wism.Tile clickedTile = GetClickedTile();
            Debug.Log("Clicked on " + clickedTile.Terrain.DisplayName);

            switch (this.SelectedState)
            {
                case ClickableObject.SelectedState.Selected:
                    MoveSelectedUnit(clickedTile);
                    this.SelectedState = ClickableObject.SelectedState.Unselected;
                    break;
                case ClickableObject.SelectedState.Moving:
                    // Do nothing
                    break;
                case ClickableObject.SelectedState.Unselected:
                    SelectObject(clickedTile);
                    break;
                default:
                    throw new InvalidOperationException("Cannot transition to unknown state.");
            }

        }
        else if (Input.GetMouseButtonDown(1))
        {
            DeselectObject();
        }
    }

    private void SelectObject(BranallyGames.Wism.Tile clickedTile)
    {
        if (clickedTile.Army != null)
        {
            Army army = clickedTile.Army;
            Debug.Log("Selected army: " + army.DisplayName);

            this.SelectedArmy = null;
            this.SelectedState = ClickableObject.SelectedState.Unselected;
            foreach (ArmyGameObject ago in this.instantiatedArmies)
            {
                if (ago.Army == army)
                {
                    this.SelectedArmy = ago;
                    this.SelectedState = ClickableObject.SelectedState.Selected;
                }
            }

            if (this.SelectedArmy == null)
                throw new InvalidOperationException("Could not find selected army in instantiated game objects.");
        }
    }

    private void FixedUpdate()
    {
        if (this.players == null)
            return;

        // Cleanup existing objects for render
        // TODO: Remove and redesign this to find and move existing objects; for now...
        for (int i = 0; i < this.instantiatedArmies.Count; i++)
        {
            ArmyGameObject ago = instantiatedArmies[i];
            Destroy(ago.GameObject);
            instantiatedArmies.RemoveAt(i);
        }

        DrawArmies(this.players);
    }

    private void MoveSelectedUnit(BranallyGames.Wism.Tile clickedTile)
    {        
        // Move the selected unit to the clicked tile location
        if (this.SelectedArmy == null)
            throw new InvalidOperationException("Selected Army was null.");

        if (!clickedTile.CanTraverseHere(SelectedArmy.Army))
        {
            Debug.Log(String.Format("Cannot move '{0}' to {1}'", SelectedArmy.Army.DisplayName, clickedTile.Terrain));
            Console.Beep(220, 800);
        }
        else
        {
            // TODO: Implement path traversal and movement in steps
            MoveArmyTo(clickedTile);
        }
    }

    private void MoveArmyTo(BranallyGames.Wism.Tile targetTile)
    {
        BranallyGames.Wism.Tile originalTile = SelectedArmy.Army.Tile;
        originalTile.Army = null;
        targetTile.Army = SelectedArmy.Army;
        SelectedArmy.Army.Tile = targetTile;

        Debug.Log(String.Format("Moved '{0}' to {1}'", SelectedArmy.Army.DisplayName, targetTile.Terrain));
    }

    private BranallyGames.Wism.Tile GetClickedTile()
    {
        Vector3 point = followCamera.ScreenToWorldPoint(Input.mousePosition);
        Coordinate gameCoord = ConvertUnityToGameCoordinates(point, tileMap);
        BranallyGames.Wism.Tile gameTile = world.Map[gameCoord.X, gameCoord.Y];
        return gameTile;
    }

    private void DeselectObject()
    {
        if (this.SelectedState == ClickableObject.SelectedState.Selected)
        {
            this.SelectedState = ClickableObject.SelectedState.Unselected;
            this.SelectedArmy = null;
        }
    }

    private IList<Player> ReadyPlayerOne()
    {
        // Temp: add armies to map for testing
        Player player1 = World.Current.Players[0];
        player1.Affiliation.IsHuman = true;        
        player1.HireHero(World.Current.Map[1, 1]);
        player1.ConscriptUnit(ModFactory.FindUnitInfo("LightInfantry"), World.Current.Map[1, 2]);
        player1.ConscriptUnit(ModFactory.FindUnitInfo("LightInfantry"), World.Current.Map[1, 2]);
        player1.ConscriptUnit(ModFactory.FindUnitInfo("Cavalry"), World.Current.Map[2, 1]);
        player1.ConscriptUnit(ModFactory.FindUnitInfo("Pegasus"), World.Current.Map[9, 17]);

        return World.Current.Players;
    }

    private World CreateWorldFromScene()
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
                        if (unityTile.name.ToLowerInvariant().Contains(terrain.ID.ToLowerInvariant()))
                        {
                            gameTile.Terrain = terrain;

                            //Instantiate trigger for tile
                            //Vector3 vector3 = ConvertGameToUnityCoordinates(gameTile.Terrain, this.tileMap);
                            //Vector3Int tileVector = new Vector3Int(Convert.ToInt32(vector3.x), Convert.ToInt32(vector3.y), 0);
                            //TileData tileData;
                            //unityTile.GetTileData(tileVector, this.tileMap, ref tileData);

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
            foreach (Army army in player.GetArmies())
            {                
                Vector3 worldVector = ConvertGameToUnityCoordinates(army, tileMap);
                InstantiateArmy(army, worldVector);
            }
        }
    }

    private static Vector3 ConvertGameToUnityCoordinates(MapObject mapObject, Tilemap tileMap)
    {
        Coordinate coord = mapObject.GetCoordinates();
        float unityX = coord.X + tileMap.cellBounds.xMin - tileMap.tileAnchor.x + 1;
        float unityY = coord.Y + tileMap.cellBounds.yMin - tileMap.tileAnchor.y + 1;
        return new Vector3(unityX, unityY, 0.0f);
    }

    private static Coordinate ConvertUnityToGameCoordinates(Vector3 unityVector, Tilemap tileMap)
    {
        float gameX = unityVector.x - tileMap.cellBounds.xMin + tileMap.tileAnchor.x - 1;
        float gameY = unityVector.y - tileMap.cellBounds.yMin + tileMap.tileAnchor.y - 1;
        return new Coordinate(Convert.ToInt32(gameX), Convert.ToInt32(gameY));
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

        // Create and set up the GameObject connected to WISM MapObject
        // TODO: Only instantiate if not already exist; and/or cleanup old objects
        GameObject go = Instantiate(gameObject, worldVector, Quaternion.identity);
        ClickableObject co = go.GetComponent<ClickableObject>();
        ArmyGameObject ago = new ArmyGameObject(army, go);
        co.ArmyGameObject = ago;
        co.TileMap = this;
        this.instantiatedArmies.Add(ago);
    }

    private TileBase[] GetTiles(out int xSize, out int ySize)
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
}
