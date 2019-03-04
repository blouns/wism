using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using BranallyGames.Wism;
using System;
using Assets.Scripts.Units;
using System.IO;
using System.Threading;

public class WorldTilemap : MonoBehaviour
{
    public GameObject[] armyKinds;          // Prefabs for units; TODO: automate creation of prefabs
    public GameObject armyPrefab;
    public GameObject WarPanel;

    private Camera followCamera;
    private World world;
    private Tilemap tileMap;
    private IList<Player> players;
    private ArmyFactory armyFactory;
    private readonly Dictionary<Guid, ArmyGameObject> armyDictionary = new Dictionary<Guid, ArmyGameObject>();

    private InputState inputState;
    private ArmyGameObject selectedArmy;
    public GameObject SelectedBoxPrefab;
    private GameObject selectedArmyBox;

    public ArmyGameObject SelectedArmy { get => selectedArmy; set => selectedArmy = value; }
    public InputState InputState { get => inputState; set => inputState = value; }

    private void Start()
    {
        Time.fixedDeltaTime = 0.25f;    // Refresh at 1/4 second
        SetupCameras();
        tileMap = transform.GetComponent<Tilemap>();
        world = CreateWorldFromScene();
        players = ReadyPlayers();
        armyFactory = ArmyFactory.Create(armyKinds);

        // TODO: Swap to use Resources or similar run-time generation of units
        //Sprite[] sprites = Resources.LoadAll<Sprite>("Armies/sirians_hero_ALPHA");
        //SpriteRenderer sr = armyPrefab.GetComponent<SpriteRenderer>();
        //sr.sprite = sprites[0];
        //Instantiate<GameObject>(this.armyPrefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);

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

            switch (this.InputState)
            {
                case InputState.Unselected:
                    SelectObject(clickedTile);
                    break;
                case InputState.ArmySelected:
                    // TODO: Second click selects top unit in army
                    // TODO: Double-click selects entire army

                    Destroy(this.selectedArmyBox);
                    if (MovingOntoEnemy(SelectedArmy.Army, clickedTile))
                    {
                        AttackArmyAt(clickedTile);
                    }
                    else
                    {
                        MoveSelectedArmyTo(clickedTile);
                    }
                    
                    break;
                case InputState.ArmyMoving:
                    // Do nothing
                    break;
                case InputState.ArmyAttacking:
                    // Do nothing
                    break;
                default:
                    throw new InvalidOperationException("Cannot transition to unknown state.");
            }

        }
        else if (Input.GetMouseButtonDown(1))
        {
            if (this.InputState == InputState.ArmySelected)
            {
                DeselectObject();
            }
        }
    }

    private void AttackArmyAt(BranallyGames.Wism.Tile targetTile)
    {
        if (SelectedArmy.Army == targetTile.Army)
            return;

        Debug.Log(String.Format("{0} are attacking {1}!", 
            SelectedArmy.Army.Affiliation, 
            targetTile.Army.Affiliation));

        InputState = InputState.ArmyAttacking;
        this.SelectedArmy.Path = null;
        this.SelectedArmy.TargetTile = targetTile;
    }

    private void FixedUpdate()
    {
        if (this.players == null)
            return;

        MoveArmies();
        CleanupArmies();
        DrawArmies();
    }

    private void MoveArmies()
    {
        if (InputState == InputState.ArmyMoving)
        {
            MoveSelectedArmy();
        }
        else if (InputState == InputState.ArmyAttacking)
        {
            AttackArmy();
        }
    }

    private void AttackArmy()
    {        
        Army attacker = SelectedArmy.Army;        
        Army defender = SelectedArmy.TargetTile.Army;

        string attackingAffiliation = attacker.Affiliation.DisplayName;
        string defendingAffiliation = defender.Affiliation.DisplayName;

        // Attack one unit is killed, but not the entire army
        AttackOnce(attacker, defender, out AttackResult result);
        switch (result)
        {
            case AttackResult.AttackerWon:
                MoveSelectedArmyTo(SelectedArmy.TargetTile);
                this.WarPanel.SetActive(false);
                break;

            case AttackResult.DefenderWon:
                DeselectObject();
                this.WarPanel.SetActive(false);
                break;

            case AttackResult.Battling:
                // Do nothing
                break;

            default:
                throw new InvalidOperationException("Unknown attack result from battle.");
        }
    }

    private void AttackOnce(Army attacker, Army defender, out AttackResult result)
    {
        string attackingUnitName = attacker[0].DisplayName;
        string defendingUnitName = defender[0].DisplayName;
        string attackerName = attacker.Affiliation.ToString();
        string defenderName = defender.Affiliation.ToString();
        
        result = AttackResult.Battling;
        if (GameManager.WarStrategy.AttackOnce(attacker, defender.Tile))
        {
            Debug.Log(String.Format("War: {0}:{1} has killed {2}:{3}.",
                attackerName, attackingUnitName,
                defenderName, defendingUnitName));
        }
        else
        {
            Debug.Log(String.Format("War: {0}:{1} has killed {2}:{3}.", 
                defenderName, defendingUnitName, 
                attackerName, attackingUnitName));
        }
        
        if (attacker.Size == 0)
        {
            // Attacker has lost the battle (all attacking units killed)
            result = AttackResult.DefenderWon;
            Debug.Log(String.Format("War: {0} have lost!", attackerName));
        }
        else if (defender.Size == 0)
        {
            // Attack has won the battle (all enemy units killed)
            result = AttackResult.AttackerWon;
            Debug.Log(String.Format("War: {0} are victorious!", attackerName));
        }
    }

    private void MoveSelectedArmy()
    {
        // Check if the next tile contains an enemy army
        if (MovingOntoEnemy(SelectedArmy.Army, SelectedArmy.TargetTile))
        {
            // Cannot move onto an enemy army without explicity attacking
            InputState = InputState.ArmySelected;
            this.SelectedArmy.Path = null;
            this.SelectedArmy.TargetTile = null;

            Debug.Log(String.Format("Enemy detected at {0}.", this.SelectedArmy.TargetTile.Coordinates));
        }
        // Try to move the army one step
        else if (!this.SelectedArmy.Army.TryMoveOneStep(this.SelectedArmy.TargetTile, ref this.SelectedArmy.Path, out float distance))
        {
            // Done moving due to path completion, out of moves, or hit barrier
            InputState = InputState.Unselected;

            if (this.SelectedArmy.Path != null && this.SelectedArmy.Path.Count == 0)
            {
                Debug.Log(String.Format("Successfully moved {0} to {1}.", this.SelectedArmy.Army, this.SelectedArmy.TargetTile.Coordinates));
            }
            else
            {
                Debug.Log(String.Format("Failed to mov {0} to {1}.", this.SelectedArmy.Army, this.SelectedArmy.TargetTile.Coordinates));
            }
        }
        // Continue moving along the path
        else
        {
            // 
            Debug.Log(String.Format("Moving {0} to {1}; distance remaining {2}...",
                this.SelectedArmy.Army, this.SelectedArmy.TargetTile.Coordinates, distance));

            Vector3 vector = ConvertGameToUnityCoordinates(this.SelectedArmy.Army.GetCoordinates());
            Rigidbody2D rb = this.SelectedArmy.GameObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.transform.position = vector;
            }
        }
    }

    private bool MovingOntoEnemy(Army army, BranallyGames.Wism.Tile targetTile)
    {
        return (targetTile.HasArmy() && (targetTile.Army.Affiliation != army.Affiliation));
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
            this.InputState = InputState.ArmySelected;
            Vector3 worldVector = ConvertGameToUnityCoordinates(army.GetCoordinates());
            this.selectedArmyBox = Instantiate<GameObject>(SelectedBoxPrefab, worldVector, Quaternion.identity, tileMap.transform);
        }
    }
       
    private void MoveSelectedArmyTo(BranallyGames.Wism.Tile targetTile)
    {        
        // Move the selected unit to the clicked tile location
        if (this.SelectedArmy == null)
            throw new InvalidOperationException("Selected Army was null.");

        if (this.SelectedArmy.Army == targetTile.Army)
            return;

        this.SelectedArmy.Path = null;
        this.SelectedArmy.TargetTile = targetTile;
        this.InputState = InputState.ArmyMoving;
    }

    private BranallyGames.Wism.Tile GetClickedTile()
    {        
        Vector3 point = followCamera.ScreenToWorldPoint(Input.mousePosition);
        Coordinates gameCoord = ConvertUnityToGameCoordinates(point, tileMap);
        BranallyGames.Wism.Tile gameTile = world.Map[gameCoord.X, gameCoord.Y];
        
        return gameTile;
    }

    private void DeselectObject()
    {        
        this.InputState = InputState.Unselected;
        this.SelectedArmy = null;
        Destroy(this.selectedArmyBox);
        Debug.Log("Deselected army.");
    }

    private IList<Player> ReadyPlayers()
    {
        // Temp: add armies to map for testing

        // Ready Player One
        Player player1 = World.Current.Players[0];
        player1.Affiliation.IsHuman = true;        
        player1.HireHero(World.Current.Map[1, 1]);
        player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), World.Current.Map[1, 2]);
        player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), World.Current.Map[1, 2]);
        player1.ConscriptArmy(ModFactory.FindUnitInfo("Cavalry"), World.Current.Map[2, 1]);
        player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), World.Current.Map[9, 17]);

        // Ready Player Two
        Player player2 = World.Current.Players[1];
        player2.Affiliation = Affiliation.Create(ModFactory.FindAffiliationInfo("StormGiants")); // Hack
        player2.Affiliation.IsHuman = false;
        player2.HireHero(World.Current.Map[18, 10]);

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

    private Vector3 ConvertGameToUnityCoordinates(Coordinates coord)
    {
        Vector3 worldVector = tileMap.CellToWorld(new Vector3Int(coord.X + 1, coord.Y + 1, 0));
        worldVector.x += tileMap.cellBounds.xMin - tileMap.tileAnchor.x;
        worldVector.y += tileMap.cellBounds.yMin - tileMap.tileAnchor.y;
        return worldVector;
    }
   
    private Coordinates ConvertUnityToGameCoordinates(Vector3 worldVector, Tilemap tileMap)
    {
        // BUGBUG: This is broken; need to adjust to tilemap coordinates in case
        //       the tilemap is translated to another location. 
        return new Coordinates(Mathf.FloorToInt(worldVector.x), Mathf.FloorToInt(worldVector.y));        
        
        //worldVector.x -= tileMap.cellBounds.xMin + tileMap.tileAnchor.x - 1;
        //worldVector.y -= tileMap.cellBounds.yMin + tileMap.tileAnchor.y - 1;
        //Vector3Int cellVector = tileMap.WorldToCell(worldVector);
        //return new Coordinates(cellVector.x, cellVector.y);
    }

    private void InstantiateArmy(Army army, Vector3 worldVector)
    {
        // Find or create and set up the GameObject connected to WISM MapObject
        if (!armyDictionary.ContainsKey(army.Guid))
        {            
            GameObject armyPrefab = armyFactory.FindGameObjectKind(army);
            if (armyPrefab == null)
            {
                Debug.Log(String.Format("GameObject not found: {0}_{1}", army.ID, army.Affiliation.ID));
                return;
            }

            GameObject go = Instantiate<GameObject>(armyPrefab, worldVector, Quaternion.identity, tileMap.transform);

            // Add to the instantiated armies dictionary for tracking
            ArmyGameObject ago = new ArmyGameObject(army, go);
            armyDictionary.Add(army.Guid, ago);

            // Select if selected army
            if ((this.InputState == InputState.ArmySelected) && (SelectedArmy.Army == army))
            {
                SelectObject(army.Tile);
            }
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

public enum InputState
{
    Unselected = 0,
    ArmySelected,
    ArmyMoving,
    ArmyAttacking
}

public enum AttackResult
{
    Battling,
    AttackerWon,
    DefenderWon
}
