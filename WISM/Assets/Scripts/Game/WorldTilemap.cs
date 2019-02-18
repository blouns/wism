using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using BranallyGames.Wism;
using System;
using Assets.Scripts.Units;
using System.IO;

public class WorldTimemap : MonoBehaviour
{
    private Camera followCamera;
    private World world;
    private Tilemap tileMap;
    private IList<Player> players;
    public GameObject[] armyKinds;
    private readonly Dictionary<Guid, ArmyGameObject> armyDictionary = new Dictionary<Guid, ArmyGameObject>();

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
        DrawArmies();
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            BranallyGames.Wism.Tile clickedTile = GetClickedTile();
            Debug.Log("Clicked on " + clickedTile.Terrain.DisplayName);

            switch (this.SelectedState)
            {
                case ClickableObject.SelectedState.Selected:
                    MoveSelectedArmyTo(clickedTile);
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

    private void FixedUpdate()
    {
        if (this.players == null)
            return;

        CleanupArmies();
        DrawArmies();
    }

    private void CleanupArmies()
    {
        // Find and cleanup stale game objects
        List<Guid> toDelete = new List<Guid>(armyDictionary.Keys);
        foreach (Player player in world.Players)
        {
            IList<Army> armies = player.GetArmies();
            foreach (Army army in armies)
            {
                if (armyDictionary.ContainsKey(army.Guid))
                {
                    toDelete.Remove(army.Guid);
                }
            }
        }

        toDelete.ForEach(guid =>
        {
            Destroy(armyDictionary[guid].GameObject);
            armyDictionary.Remove(guid);
        });
    }

    private void SelectObject(BranallyGames.Wism.Tile clickedTile)
    {
        // If tile contains an army, select it     
        if (clickedTile.Army != null)
        {
            Army army = clickedTile.Army;
            Debug.Log("Selected army: " + army.DisplayName);

            if (!armyDictionary.ContainsKey(army.Guid))
            {
                throw new InvalidOperationException("Could not find selected army in instantiated game objects.");
            }

            this.SelectedArmy = armyDictionary[army.Guid];
            this.SelectedState = ClickableObject.SelectedState.Selected;
        }
    }
       
    private void MoveSelectedArmyTo(BranallyGames.Wism.Tile clickedTile)
    {        
        // Move the selected unit to the clicked tile location
        if (this.SelectedArmy == null)
            throw new InvalidOperationException("Selected Army was null.");

        if (!clickedTile.CanTraverseHere(SelectedArmy.Army))
        {
            Debug.Log(String.Format("Cannot move '{0}' to {1}'", SelectedArmy.Army.DisplayName, clickedTile.Terrain));
        }
        else
        {
            // TODO: Implement path traversal and movement in steps
            MoveArmy(this.SelectedArmy, clickedTile);
        }
    }

    private void MoveArmy(ArmyGameObject armyGo, BranallyGames.Wism.Tile targetTile)
    {
        // Move in game model
        BranallyGames.Wism.Tile originalTile = armyGo.Army.Tile;
        originalTile.Army = null;
        targetTile.Army = armyGo.Army;
        armyGo.Army.Tile = targetTile;

        // Move in Unity
        GameObject go = armyGo.GameObject;
        go.transform.position = ConvertGameToUnityCoordinates(targetTile.Coordinate);

        Debug.Log(String.Format("Moved '{0}' to {1}'", armyGo.Army.DisplayName, targetTile.Terrain));
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

        TileBase[] tilemapTiles = GetUnityTiles(out int boundsX, out int boundsY);
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

    private void DrawArmies()
    {
        // Verify that army kids were loaded
        if (armyKinds == null || armyKinds.Length == 0)
        {
            Debug.Log("No army kinds found.");
            return;
        }

        foreach (Player player in world.Players)
        {
            foreach (Army army in player.GetArmies())
            {                
                Vector3 worldVector = ConvertGameToUnityCoordinates(army.GetCoordinates());
                InstantiateArmy(army, worldVector);
            }
        }
    }

    private Vector3 ConvertGameToUnityCoordinates(Coordinate coord)
    {
        float unityX = coord.X + tileMap.cellBounds.xMin - tileMap.tileAnchor.x + 1;
        float unityY = coord.Y + tileMap.cellBounds.yMin - tileMap.tileAnchor.y + 1;
        return new Vector3(unityX, unityY, 0.0f);
    }
   
    private Coordinate ConvertUnityToGameCoordinates(Vector3 unityVector, Tilemap tileMap)
    {
        float gameX = unityVector.x - tileMap.cellBounds.xMin + tileMap.tileAnchor.x - 1;
        float gameY = unityVector.y - tileMap.cellBounds.yMin + tileMap.tileAnchor.y - 1;
        return new Coordinate(Convert.ToInt32(gameX), Convert.ToInt32(gameY));
    }

    private void InstantiateArmy(Army army, Vector3 worldVector)
    {
        // Find or create and set up the GameObject connected to WISM MapObject
        if (!armyDictionary.ContainsKey(army.Guid))
        {            
            ArmyFactory factory = ArmyFactory.Create(armyKinds);
            
            GameObject go = factory.FindGameObjectKind(army);
            if (go == null)
            {
                Debug.Log(String.Format("GameObject not found: {0}_{1}", army.ID, army.Affiliation.ID));
                return;
            }

            Instantiate(go, worldVector, Quaternion.identity);            

            // Add to the instantiated armies dictionary for tracking
            ArmyGameObject ago = new ArmyGameObject(army, go);
            ClickableObject co = go.GetComponent<ClickableObject>();
            co.ArmyGameObject = ago;
            co.TileMap = this;
            armyDictionary.Add(army.Guid, ago);
        }                
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
}
