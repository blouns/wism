using Assets.Scripts.Units;
using BranallyGames.Wism;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldTilemap : MonoBehaviour
{
    public GameObject[] armyKinds;          // Prefabs for units; TODO: automate creation of prefabs
    public GameObject armyPrefab;

    [SerializeField]
    private GameObject warPanelPrefab;
    [SerializeField]
    private GameObject unitPickerPrefab;
    private WarPanel WarPanel;
    private UnitPicker UnitPickerPanel;

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
    private AttackResult attackResult;

    private void Start()
    {
        // Set up game UI
        SetTime(GameManager.StandardTime);
        SetupCameras();
        tileMap = transform.GetComponent<Tilemap>();
        WarPanel = this.warPanelPrefab.GetComponent<WarPanel>();
        UnitPickerPanel = this.unitPickerPrefab.GetComponent<UnitPicker>();

        // Set up game
        world = CreateWorldFromScene();
        players = ReadyPlayers();
        armyFactory = ArmyFactory.Create(armyKinds);

        CreateArmyGameObjects();
    }

    private void SetTime(float time)
    {
        Time.fixedDeltaTime = time;
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

                case InputState.SelectingUnits:
                    // Do nothing
                    break;

                case InputState.ArmySelected:
                    // TODO: Second click selects top unit in army
                    // TODO: Double-click selects entire army

                    // Move or attack; can only attack from and adjacent tile
                    bool isAttacking = MovingOntoEnemy(SelectedArmy.Army, clickedTile);
                    bool isAdjacent = clickedTile.IsNeighbor(SelectedArmy.Army.Tile);
                    if (isAttacking && isAdjacent)
                    {
                        // War!
                        Destroy(this.selectedArmyBox);
                        AttackArmyAt(clickedTile);
                    }
                    // Cannot attack from non-adjacent tile
                    else if (isAttacking & !isAdjacent)
                    {
                        // Do nothing
                        Debug.Log("Too far away to attack.");
                    }
                    else if (!isAttacking)
                    {
                        // Move
                        Destroy(this.selectedArmyBox);
                        MoveSelectedArmyTo(clickedTile);
                    }
                    break;

                case InputState.ArmyMoving:
                    // Do nothing
                    break;

                case InputState.ArmyAttacking:
                    // Do nothing
                    break;

                case InputState.BattleCompleted:
                    CompleteBattle();
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
        else if (Input.GetKeyDown(KeyCode.M))
        {
            if (this.InputState == InputState.ArmySelected)
            {
                UnitPickerPanel.Initialize(this.SelectedArmy.Army, this.armyKinds);
                this.InputState = InputState.SelectingUnits;
            }
        }
        else if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleMinimap();
        }
    }

    private void ToggleMinimap()
    {
        GameObject map = UnityUtilities.GameObjectHardFind("MinimapBorder");
        map.SetActive(!map.activeSelf);
    }

    private void CompleteBattle()
    {
        switch (this.attackResult)
        {
            case AttackResult.AttackerWon:
                MoveSelectedArmyTo(SelectedArmy.TargetTile);

                this.WarPanel.Teardown();
                SetTime(GameManager.StandardTime);
                break;

            case AttackResult.DefenderWon:
                DeselectObject();

                this.WarPanel.Teardown();
                SetTime(GameManager.StandardTime);
                break;

            case AttackResult.Battling:
                SetTime(GameManager.WarTime);
                break;

            default:
                throw new InvalidOperationException("Unknown attack result from battle.");
        }
    }

    private void AttackArmyAt(BranallyGames.Wism.Tile targetTile)
    {
        Army attacker = SelectedArmy.Army;
        Army defender = targetTile.Army;

        if (attacker == defender)
            return;

        Debug.LogFormat("{0} are attacking {1}!",
            SelectedArmy.Army.Affiliation,
            targetTile.Army.Affiliation);

        InputState = InputState.ArmyAttacking;
        this.SelectedArmy.Path = null;
        this.SelectedArmy.TargetTile = targetTile;

        // Set up war UI    
        WarPanel.Initialize(attacker, defender, armyKinds);
        SetTime(GameManager.WarTime);
    }

    private void FixedUpdate()
    {
        if (this.players == null)
            return;

        MoveArmies();
        CleanupArmies();
        CreateArmyGameObjects();
    }

    private void MoveArmies()
    {
        if (InputState == InputState.ArmyMoving)
        {
            MoveArmy(this.SelectedArmy);
        }
        else if (InputState == InputState.ArmyAttacking)
        {
            AttackArmy(this.SelectedArmy);
        }
    }

    private void AttackArmy(ArmyGameObject armyGO)
    {
        Army attacker = armyGO.Army;
        Army defender = armyGO.TargetTile.Army;

        string attackingAffiliation = attacker.Affiliation.DisplayName;
        string defendingAffiliation = defender.Affiliation.DisplayName;

        // Attack until one unit is killed, but not the entire army
        AttackOnce(attacker, defender, out this.attackResult);

        if (this.attackResult != AttackResult.Battling)
        {
            InputState = InputState.BattleCompleted;
        }
    }

    private bool AttackOnce(Army attacker, Army defender, out AttackResult result)
    {
        string attackerName = attacker.Affiliation.ToString();
        string defenderName = defender.Affiliation.ToString();

        // Empty army
        if (attacker.Size == 0)
        {
            Debug.LogWarning("Attacker attacked without any units.");
            result = AttackResult.DefenderWon;
            return false;
        }
        else if (defender.Size == 0)
        {
            Debug.LogWarning("Defender attacked without any units.");
            result = AttackResult.AttackerWon;
            return false;
        }

        // Battle it out
        result = AttackResult.Battling;
        Guid selectedUnitGuid = this.SelectedArmy.Army.Guid;
        IList<Unit> attackingUnits = attacker.SortByBattleOrder(defender.Tile);
        IList<Unit> defendingUnits = defender.SortByBattleOrder(defender.Tile);
        Unit attackingUnit = attackingUnits[0];
        Unit defendingUnit = defendingUnits[0];
        bool battleContinues = GameManager.WarStrategy.AttackOnce(attacker, defender.Tile, out bool didAttackerWin);

        if (didAttackerWin)
        {
            WarPanel.UpdateBattle(didAttackerWin, defendingUnit);
            Debug.LogFormat("War: {0}:{1} has killed {2}:{3}.",
                attackerName, attackingUnit.DisplayName,
                defenderName, defendingUnit.DisplayName);
        }
        else // Attack failed
        {
            WarPanel.UpdateBattle(didAttackerWin, attackingUnit);
            Debug.LogFormat("War: {0}:{1} has killed {2}:{3}.",
                defenderName, defendingUnit.DisplayName,
                attackerName, attackingUnit.DisplayName);

            // If Selected Unit lost a unit, reset the top GameObject
            if (battleContinues && (selectedUnitGuid == attackingUnit.Guid))
            {
                // TODO: Move this into the core game loop to reset all armies; this could impact any army

                // Replace the GameObject with the next unit in the army               
                ArmyGameObject ago = this.armyDictionary[selectedUnitGuid];
                ago.GameObject.SetActive(true);
            }
        }

        if (!battleContinues)
        {
            if (!didAttackerWin)
            {
                // Attacker has lost the battle (all attacking units killed)
                result = AttackResult.DefenderWon;
                Debug.LogFormat("War: {0} have lost!", attackerName);
            }
            else if (didAttackerWin)
            {
                // Attack has won the battle (all enemy units killed)
                result = AttackResult.AttackerWon;
                Debug.LogFormat("War: {0} are victorious!", attackerName);
            }
        }

        return battleContinues;
    }

    private void MoveArmy(ArmyGameObject armyGO)
    {
        SetTime(GameManager.StandardTime);
        SetCameraTarget(armyGO.GameObject.transform);
        if (armyGO.Path == null)
        {
            armyGO.Army.FindPath(armyGO.TargetTile, out armyGO.Path, out float distance);
            if (armyGO.Path.Count < 2)
            {
                Debug.Log("Impossible route.");
                return;
            }
        }

        // Check if the next tile contains an enemy army; cannot move onto an enemy army without explicity attacking
        if ((armyGO.Path.Count > 1) && MovingOntoEnemy(SelectedArmy.Army, armyGO.Path[1]))
        {
            Debug.LogFormat("Enemy detected at {0}.", armyGO.TargetTile.Coordinates);
            armyGO.Path = null;
            armyGO.TargetTile = null;
            SelectObject(SelectedArmy.Army.Tile);
        }
        // Try to move the army one step
        else if (!armyGO.Army.TryMoveOneStep(armyGO.TargetTile, ref armyGO.Path, out float distance))
        {
            // Done moving due to path completion, out of moves, or hit barrier
            if (armyGO.Path != null && armyGO.Path.Count == 0)
            {
                Debug.LogFormat("Moved {0} to {1}", armyGO.Army, armyGO.TargetTile.Coordinates);
            }
            else
            {
                Debug.LogFormat("Cannot move {0} to {1}.", armyGO.Army, armyGO.TargetTile.Coordinates);
            }

            if (armyGO.Army.MovesRemaining == 0)
            {
                InputState = InputState.Unselected;
            }
            else
            {
                armyGO.Path = null;
                armyGO.TargetTile = null;
                SelectObject(SelectedArmy.Army.Tile);
            }
        }
        // Continue moving along the path
        else
        {
            Vector3 vector = ConvertGameToUnityCoordinates(armyGO.Army.GetCoordinates());

            if (armyGO.GameObject == null)
            {
                Debug.LogErrorFormat("Trying to move a destroyed object: {0}", armyGO.Army.Guid);
            }
            Rigidbody2D rb = armyGO.GameObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.transform.position = vector;
            }
        }

        Debug.LogFormat("Moves remaining: {0}", armyGO.Army.MovesRemaining);
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
                    // Found the army so don't remove it
                    toDelete.Remove(army.Guid);
                }
            }
        }

        // Remove objects missing from the game
        toDelete.ForEach(guid =>
        {
            Destroy(armyDictionary[guid].GameObject);
            armyDictionary.Remove(guid);
        });
    }

    private void CreateArmyGameObjects()
    {
        // Verify that army kids were loaded
        if (armyKinds == null || armyKinds.Length == 0)
        {
            Debug.Log("No army kinds found.");
            return;
        }

        foreach (Player player in world.Players)
        {
            // Create all the objects
            foreach (Army army in player.GetArmies())
            {
                // Find or create and set up the GameObject connected to WISM MapObject
                if (!armyDictionary.ContainsKey(army.Guid))
                {                     
                    Vector3 worldVector = ConvertGameToUnityCoordinates(army.GetCoordinates());
                    InstantiateArmy(army, worldVector);
                }
            }

            // Draw only the "top" unit for each army on the map
            foreach (BranallyGames.Wism.Tile tile in World.Current.Map)
            {
                if (!tile.HasArmy() || !this.armyDictionary.ContainsKey(tile.Army.Guid))
                    continue;

                ArmyGameObject ago = this.armyDictionary[tile.Army.Guid];
                ago.GameObject.SetActive(true);
            }
        }

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
            this.selectedArmyBox.SetActive(true);

            SetCameraTarget(this.selectedArmyBox.transform);
        }
        else
        {
            Vector3 worldVector = ConvertGameToUnityCoordinates(clickedTile.Coordinates);
            GameObject go = Instantiate<GameObject>(SelectedBoxPrefab, worldVector, Quaternion.identity, tileMap.transform);
            go.SetActive(false);
            SetCameraTarget(go.transform);
            Destroy(go, .5f);
        }
    }

    private void SetCameraTarget(Transform transform)
    {
        CameraFollow camera = this.followCamera.GetComponent<CameraFollow>();
        camera.target = transform;
    }

    internal void SetSelectedArmy(List<Unit> selectedUnits)
    {
        bool sameUnitsSelected = false;
        Army originalArmy = this.SelectedArmy.Army;

        if (this.SelectedArmy.Army.Size == selectedUnits.Count)
        {
            sameUnitsSelected = true;
            for (int i = 0; i < selectedUnits.Count; i++)
            {
                sameUnitsSelected &= selectedUnits.Contains(this.SelectedArmy.Army[i]);
            }
        }

        if (!sameUnitsSelected)
        {
            Army selectedArmy = originalArmy.Split(selectedUnits);

            // Remove existing game object; create two new ones for split
            armyDictionary.Remove(originalArmy.Guid);
            Vector3 worldVector = ConvertGameToUnityCoordinates(selectedArmy.GetCoordinates());
            InstantiateArmy(originalArmy, worldVector);

            //BUGBUG: Cannot add the GUID for an army twice. Need a clean split for the army that is staying. 
            //        Maybe Split shoudl instead return two armies?
            InstantiateArmy(selectedArmy, worldVector);
        }

        InputState = InputState.ArmySelected;
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
        // TODO: Clamp to only positions on the world or UI
        Vector3 point = followCamera.ScreenToWorldPoint(Input.mousePosition);
        Coordinates gameCoord = ConvertUnityToGameCoordinates(point, tileMap);
        BranallyGames.Wism.Tile gameTile = world.Map[gameCoord.X, gameCoord.Y];

        return gameTile;
    }

    internal void DeselectObject()
    {
        this.InputState = InputState.Unselected;
        this.SelectedArmy = null;
        Destroy(this.selectedArmyBox);
        SetTime(GameManager.StandardTime);
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
        player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), World.Current.Map[2, 1]);
        player1.ConscriptArmy(ModFactory.FindUnitInfo("Cavalry"), World.Current.Map[2, 1]);
        player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), World.Current.Map[2, 1]);

        player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), World.Current.Map[9, 17]);
        player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), World.Current.Map[9, 17]);
        player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), World.Current.Map[9, 17]);
        player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), World.Current.Map[9, 17]);
        player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), World.Current.Map[9, 17]);
        player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), World.Current.Map[9, 17]);
        player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), World.Current.Map[9, 17]);
        player1.ConscriptArmy(ModFactory.FindUnitInfo("Pegasus"), World.Current.Map[9, 17]);

        // Ready Player Two
        Player player2 = World.Current.Players[1];
        player2.Affiliation = Affiliation.Create(ModFactory.FindAffiliationInfo("StormGiants")); // Hack
        player2.Affiliation.IsHuman = false;
        player2.HireHero(World.Current.Map[18, 10]);
        player2.HireHero(World.Current.Map[1, 3]);
        player2.HireHero(World.Current.Map[2, 3]);
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
        World.Current.Random = GameManager.Random;

        return World.Current;
    }

    internal Vector3 ConvertGameToUnityCoordinates(Coordinates coord)
    {
        Vector3 worldVector = tileMap.CellToWorld(new Vector3Int(coord.X + 1, coord.Y + 1, 0));
        worldVector.x += tileMap.cellBounds.xMin - tileMap.tileAnchor.x;
        worldVector.y += tileMap.cellBounds.yMin - tileMap.tileAnchor.y;
        return worldVector;
    }

    internal Coordinates ConvertUnityToGameCoordinates(Vector3 worldVector, Tilemap tileMap)
    {
        // BUGBUG: This is broken; need to adjust to tilemap coordinates in case
        //       the tilemap is translated to another location. 
        return new Coordinates(Mathf.FloorToInt(worldVector.x), Mathf.FloorToInt(worldVector.y));

        //worldVector.x -= tileMap.cellBounds.xMin + tileMap.tileAnchor.x - 1;
        //worldVector.y -= tileMap.cellBounds.yMin + tileMap.tileAnchor.y - 1;
        //Vector3Int cellVector = tileMap.WorldToCell(worldVector);
        //return new Coordinates(cellVector.x, cellVector.y);
    }

    internal void InstantiateArmy(Army army, Vector3 worldVector)
    {
        GameObject armyPrefab = armyFactory.FindGameObjectKind(army);
        if (armyPrefab == null)
        {
            Debug.LogFormat("GameObject not found: {0}_{1}", army.ID, army.Affiliation.ID);
            return;
        }

        GameObject go = Instantiate<GameObject>(armyPrefab, worldVector, Quaternion.identity, tileMap.transform);
        go.SetActive(false);

        // Add to the instantiated armies dictionary for tracking
        ArmyGameObject ago = new ArmyGameObject(army, go);
        armyDictionary.Add(army.Guid, ago);

        // Select if selected army
        if ((this.InputState == InputState.ArmySelected) && (SelectedArmy.Army == army))
        {
            SelectObject(army.Tile);
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
            if (camera.name == "MainCamera")
            {
                followCamera = camera;
            }
        }

        if (followCamera == null)
        {
            throw new InvalidOperationException("Could not find the MainCamera.");
        }
    }
}

public enum InputState
{
    Unselected = 0,
    SelectingUnits,
    ArmySelected,
    ArmyMoving,
    ArmyAttacking,
    BattleCompleted
}

public enum AttackResult
{
    Battling,
    AttackerWon,
    DefenderWon
}
