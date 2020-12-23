using Assets.Scripts.UI;
using Assets.Scripts.Units;
using System;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using ILogger = Wism.Client.Common.ILogger;
using Tile = Wism.Client.Core.Tile;

namespace Assets.Scripts.Wism
{
    /// <summary>
    /// Unity game is the primary game loop and bridge to the Unity UI and WISM API via GameManager
    /// </summary>
    public class UnityGame : MonoBehaviour
    {
        private int lastCommandId = 0;
        private ILogger logger;
        private ControllerProvider provider;

        // TODO: automate creation of army prefabs
        public GameObject[] ArmyKinds;
        public GameObject ArmyPrefab;

        public WorldTilemap WorldTilemap;
        public GameManager GameManager;

        [SerializeField]
        private GameObject warPanelPrefab;
        [SerializeField]
        private GameObject armyPickerPrefab;
        private WarPanel WarPanel;
        private ArmyPicker ArmyPickerPanel;

        private Camera followCamera;
        private ArmyFactory armyFactory;
        private readonly Dictionary<int, ArmyGameObject> armyDictionary = new Dictionary<int, ArmyGameObject>();

        public GameObject SelectedBoxPrefab;
        private SelectedArmyBox selectedArmyBox;
        private int selectedArmyIndex;

        // Input handling
        private readonly Timer mouseSingleClickTimer = new Timer();
        private bool singleClickProcessed;

        public bool SelectingArmies { get; set; }

        public void Start()
        {
            GameManager.Initialize();
            Initialize(GameManager.LoggerFactory, GameManager.ControllerProvider);
        }        

        private void Update()
        {
            HandleInput();
        }        

        public void FixedUpdate()
        {
            try
            {
                Draw();
                DoTasks();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
        }

        void SingleClick(object o, System.EventArgs e)
        {
            mouseSingleClickTimer.Stop();

            singleClickProcessed = true;
        }

        private void HandleInput()
        {
            if (SelectingArmies)
            {
                // Army picker has focus
                return;
            }

            if (singleClickProcessed)
            {
                // Single click performed
                HandleLeftClick();
                singleClickProcessed = false;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                // Detect single vs. double-click
                if (mouseSingleClickTimer.Enabled == false)
                {
                    // ... timer start
                    mouseSingleClickTimer.Start();
                    // ... wait for double click...
                    return;
                }
                else
                {
                    // Doubleclick performed - Cancel single click
                    mouseSingleClickTimer.Stop();

                    HandleLeftClick(true);
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
            }
            else
            {
                HandleKeyboard();
            }
        }

        private void Draw()
        {
            DrawSelectedArmiesBox();
            DrawArmyGameObjects();
        }


        public void Initialize(ILoggerFactory loggerFactory, ControllerProvider provider)
        {
            this.logger = loggerFactory.CreateLogger();
            this.provider = provider;

            // Set up game UI
            SetTime(GameManager.StandardTime);
            SetupCameras();            
            WarPanel = this.warPanelPrefab.GetComponent<WarPanel>();
            ArmyPickerPanel = this.armyPickerPrefab.GetComponent<ArmyPicker>();

            Vector3 worldVector = WorldTilemap.ConvertGameToUnityCoordinates(1, 1);
            this.selectedArmyBox = Instantiate<GameObject>(SelectedBoxPrefab, worldVector, Quaternion.identity, WorldTilemap.transform).GetComponent<SelectedArmyBox>();
            this.selectedArmyIndex = -1;

            // Set up game    
            World.CreateWorld(WorldTilemap.CreateWorldFromScene().Map);
            armyFactory = ArmyFactory.Create(ArmyKinds);
            CreateDefaultArmies();
            DrawArmyGameObjects();

            mouseSingleClickTimer.Interval = 400;
            mouseSingleClickTimer.Elapsed += SingleClick;
        }

        
        /// <summary>
        /// Execute the commands from the UI, AI, or other devices
        /// </summary>
        private void DoTasks()
        {            
            foreach (Command command in provider.CommandController.GetCommandsAfterId(lastCommandId))
            {
                logger.LogInformation($"Task executing: {command.Id}: {command.GetType()}");
                Debug.Log($"Pre-command GameState: {Game.Current.GameState}");
                Debug.Log($"{command}");

                // Run the command
                var result = command.Execute();

                Debug.Log($"Post-command GameState: {Game.Current.GameState}");

                // Process the result
                if (result == ActionState.Succeeded)
                {
                    logger.LogInformation($"Task successful");
                    lastCommandId = command.Id;
                }
                else if (result == ActionState.Failed)
                {
                    logger.LogInformation($"Task failed");
                    lastCommandId = command.Id;                   
                }
                else if (result == ActionState.InProgress)
                {
                    logger.LogInformation("Task started and in progress");
                    // Do nothing; do not advance Command ID
                }
            }
        }

        private void SetTime(float time)
        {
            Time.fixedDeltaTime = time;
        }

        private void HandleKeyboard()
        {   
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (Game.Current.GameState == GameState.SelectedArmy)
                {
                    var armiesToPick = Game.Current.GetSelectedArmies()[0].Tile.GetAllArmies();
                    ArmyPickerPanel.Initialize(this, armiesToPick, armyFactory);
                }
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                ToggleMinimap();
            }
        }

        private void HandleRightClick()
        {
            if (Game.Current.GameState == GameState.SelectedArmy)
            {
                DeselectObject();
            }
        }

        private void HandleLeftClick(bool isDoubleClick = false)
        {
            Tile clickedTile = WorldTilemap.GetClickedTile(followCamera);
            switch (Game.Current.GameState)
            {
                case GameState.Ready:
                    SelectObject(clickedTile, isDoubleClick);                    
                    break;

                case GameState.SelectedArmy:
                    if (Game.Current.ArmiesSelected() &&
                        clickedTile == Game.Current.GetSelectedArmies()[0].Tile)
                    {
                        // Clicking on already selected tile
                        SelectObject(clickedTile, isDoubleClick);
                    }
                    else
                    {
                        // Move
                        GameManager.MoveSelectedArmies(clickedTile.X, clickedTile.Y);
                    }

                    // TODO: Second click selects top unit in army

                    // Move or attack; can only attack from and adjacent tile
                    //bool isAttacking = MovingOntoEnemy(SelectedArmy.Army, clickedTile);
                    //bool isAdjacent = clickedTile.IsNeighbor(SelectedArmy.Army.Tile);
                    //if (isAttacking && isAdjacent)
                    //{
                    //    // War!
                    //    Destroy(this.selectedArmyBox);
                    //    AttackArmyAt(clickedTile);
                    //}
                    //// Cannot attack from non-adjacent tile
                    //else if (isAttacking & !isAdjacent)
                    //{
                    //    // Do nothing
                    //    Debug.Log("Too far away to attack.");
                    //}
                    //else if (!isAttacking)
                    //{
                    //    // Move
                    //    Destroy(this.selectedArmyBox);
                    //    MoveSelectedArmyTo(clickedTile);
                    //}                        
                    break;

                case GameState.MovingArmy:
                    // Do nothing
                    break;

                case GameState.AttackingArmy:
                    // Do nothing
                    break;

                case GameState.CompletedBattle:
                    //CompleteBattle();
                    break;

                default:
                    throw new InvalidOperationException("Cannot transition to unknown state.");
            }

            Draw();
        }

        private void DrawSelectedArmiesBox()
        {
            if (this.selectedArmyBox == null)
            {
                throw new InvalidOperationException("Selected army box was null.");
            }

            if (!Game.Current.ArmiesSelected())
            {
                // None; so delete bounding box if it exists
                if (this.selectedArmyBox.IsSelectedBoxActive())
                {
                    this.selectedArmyBox.HideSelectedBox();
                }

                return;
            }

            List<Army> armies = Game.Current.GetSelectedArmies();
            Army army = armies[0];
            Tile tile = army.Tile;

            // Have the selected armies already been rendered?
            if (this.selectedArmyBox.IsSelectedBoxActive())
            {
                var boxGameCoords = WorldTilemap.ConvertUnityToGameCoordinates(this.selectedArmyBox.transform.position);
                if (boxGameCoords.Item1 == tile.X &&
                    boxGameCoords.Item2 == tile.Y)
                {
                    // Do nothing; already rendered
                    return;
                }
                else
                {
                    // Clear the old box; it is stale
                    this.selectedArmyBox.HideSelectedBox();
                }
            }

            if (!armyDictionary.ContainsKey(army.Id))
            {
                throw new InvalidOperationException("Could not find selected army in game objects.");
            }

            // Render the selected box
            Vector3 worldVector = WorldTilemap.ConvertGameToUnityCoordinates(army.X, army.Y);
            this.selectedArmyBox.ShowSelectedBox(worldVector);
            SetCameraTarget(this.selectedArmyBox.transform);
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

        private void ToggleMinimap()
        {
            GameObject map = UnityUtilities.GameObjectHardFind("MinimapBorder");
            map.SetActive(!map.activeSelf);
        }

        /*
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
      
        private void AttackArmyAt(Wism.Client.Core.Tile targetTile)
        {
            Army attacker = SelectedArmy.Army;
            Army defender = targetTile.Army;

            if (attacker == defender)
                return;

            Debug.LogFormat("{0} are attacking {1}!",
                SelectedArmy.Army.Clan,
                targetTile.Army.Clan);

            InputState = GameState.AttackingArmy;
            this.SelectedArmy.Path = null;
            this.SelectedArmy.TargetTile = targetTile;

            // Set up war UI    
            WarPanel.Initialize(attacker, defender, armyKinds);
            SetTime(GameManager.WarTime);
        }

        private void AttackArmy(ArmyGameObject armyGO)
        {
            List<Army> attackers = armyGO.Armies;
            List<Army> defenders = armyGO.TargetTile.Armies;

            string attackingClan = attackers[0].Clan.DisplayName;
            string defendingClan = defenders[0].Clan.DisplayName;

            // Attack until one unit is killed, but not the entire army
            AttackOnce(attackers, defenders, out this.attackResult);

            if (this.attackResult != AttackResult.Battling)
            {
                GameState = GameState.BattleCompleted;
            }
        }

        private bool AttackOnce(List<Army> attackers, List<Army> defenders, out AttackResult result)
        {
            // Empty army
            if (attackers.Count == 0)
            {
                Debug.LogWarning("Attacker attacked without any units.");
                result = AttackResult.DefenderWon;
                return false;
            }
            else if (defenders.Count == 0)
            {
                Debug.LogWarning("Defender attacked without any units.");
                result = AttackResult.AttackerWon;
                return false;
            }

            string attackerName = attackers[0].Clan.ToString();
            string defenderName = defenders[0].Clan.ToString();

            // Battle it out
            //result = AttackResult.Battling;
            Guid selectedArmyGuid = this.SelectedArmy.Armies.Guid;
            IList<Army> attackingArmys = attackers.SortByBattleOrder(defenders.Tile);
            IList<Army> defendingArmys = defenders.SortByBattleOrder(defenders.Tile);
            Army attackingArmy = attackingArmys[0];
            Army defendingArmy = defendingArmys[0];
            bool battleContinues = GameManager.WarStrategy.AttackOnce(attackers, defenders.Tile, out bool didAttackerWin);

            if (didAttackerWin)
            {
                WarPanel.UpdateBattle(didAttackerWin, defendingArmy);
                Debug.LogFormat("War: {0}:{1} has killed {2}:{3}.",
                    attackerName, attackingArmy.DisplayName,
                    defenderName, defendingArmy.DisplayName);
            }
            else // Attack failed
            {
                WarPanel.UpdateBattle(didAttackerWin, attackingArmy);
                Debug.LogFormat("War: {0}:{1} has killed {2}:{3}.",
                    defenderName, defendingArmy.DisplayName,
                    attackerName, attackingArmy.DisplayName);

                // If Selected Army lost a unit, reset the top GameObject
                if (battleContinues && (selectedArmyGuid == attackingArmy.Guid))
                {
                    // TODO: Move this into the core game loop to reset all armies; this could impact any army

                    // Replace the GameObject with the next unit in the army               
                    ArmyGameObject ago = this.armyDictionary[selectedArmyGuid];
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
*/
        private bool IsMovingOntoEnemy(List<Army> armies, Tile targetTile)
        {
            return targetTile.CanAttackHere(armies);
        }

        private void CleanupArmies()
        {
            // Find and cleanup stale game objects
            var toDelete = new List<int>(armyDictionary.Keys);
            foreach (Player player in Game.Current.Players)
            {
                IList<Army> armies = player.GetArmies();
                foreach (Army army in armies)
                {
                    if (armyDictionary.ContainsKey(army.Id))
                    {
                        // Found the army so don't remove it
                        toDelete.Remove(army.Id);
                    }
                }
            }

            // Remove objects missing from the game
            toDelete.ForEach(id =>
            {
                Destroy(armyDictionary[id].GameObject);
                armyDictionary.Remove(id);
            });
        }

        private void DrawArmyGameObjects()
        {
            // Verify that army kinds were loaded
            if (ArmyKinds == null || ArmyKinds.Length == 0)
            {
                Debug.Log("No army kinds found.");
                return;
            }

            foreach (Player player in Game.Current.Players)
            {
                // Create all the objects if not already present
                foreach (Army army in player.GetArmies())
                {
                    // Find or create and set up the GameObject connected to WISM MapObject
                    if (!armyDictionary.ContainsKey(army.Id))
                    {
                        Vector3 worldVector = WorldTilemap.ConvertGameToUnityCoordinates(army.X, army.Y);
                        InstantiateArmy(army, worldVector);
                    }
                    else
                    {
                        armyDictionary[army.Id].GameObject.SetActive(false);
                    }
                }

                // Draw only the "top" army for each army stack on the map
                foreach (Tile tile in World.Current.Map)
                {
                    int armyId;
                    // Draw visiting armies over stationed armies
                    if (tile.HasVisitingArmies() && armyDictionary.ContainsKey(tile.VisitingArmies[0].Id))
                    {
                        armyId = tile.VisitingArmies[0].Id;
                        SetCameraTarget(armyDictionary[armyId].GameObject.transform);
                    }
                    else if (tile.HasArmies() && this.armyDictionary.ContainsKey(tile.Armies[0].Id))
                    {
                        armyId = tile.Armies[0].Id;
                    }
                    else
                    {
                        // Not a "top" army                        
                        continue;
                    }
                    
                    ArmyGameObject ago = this.armyDictionary[armyId];
                    Vector3 vector = WorldTilemap.ConvertGameToUnityCoordinates(ago.Army.X, ago.Army.Y);
                    ago.GameObject.transform.position = vector;
                    ago.GameObject.SetActive(true);
                }
            }

        }

        private void SelectObject(Tile tile, bool selectAll)
        {
            // If no armies on selected tile
            if ((Game.Current.GameState == GameState.Ready) &&
                !tile.HasVisitingArmies() &&
                !tile.HasArmies())
            {                
                CenterOnTile(tile);
                return;
            }

            var armiesToSelect = new List<Army>();
            if (selectAll)
            {
                // Selecting all armies on tile
                this.selectedArmyIndex = -1;
                armiesToSelect = tile.GetAllArmies();
            }
            else if ((tile.HasVisitingArmies() && tile.VisitingArmies.Count > 1) ||
                     (!tile.HasVisitingArmies() && tile.HasArmies()))
            {
                // Select "top" army on tile
                var allArmies = tile.GetAllArmies();
                allArmies.Sort(new ByArmyViewingOrder());

                this.selectedArmyIndex = 0;
                armiesToSelect.Add(allArmies[0]);
            }
            else if (tile.HasVisitingArmies() && tile.VisitingArmies.Count == 1 &&
                     tile.HasArmies())
            {
                // Cycle next army on tile
                var allArmies = tile.GetAllArmies();
                allArmies.Sort(new ByArmyViewingOrder());

                // Now there are only Armies (no Visiting Armies)
                this.selectedArmyIndex = (this.selectedArmyIndex + 1) % allArmies.Count;
                armiesToSelect.Add(allArmies[this.selectedArmyIndex]);
            }

            if (armiesToSelect.Count > 0)
            {
                armiesToSelect.Sort(new ByArmyViewingOrder());
                GameManager.SelectArmies(armiesToSelect);
            }
        }

        internal void DeselectObject()
        {
            GameManager.DeselectArmies();

            this.selectedArmyBox.HideSelectedBox();
            this.selectedArmyIndex = -1;
            SetTime(GameManager.StandardTime);
        }

        private void CenterOnTile(Tile clickedTile)
        {
            Vector3 worldVector = WorldTilemap.ConvertGameToUnityCoordinates(clickedTile.X, clickedTile.Y);
            this.selectedArmyBox.SetActive(false);
            this.selectedArmyBox.transform.position = worldVector;
            SetCameraTarget(this.selectedArmyBox.transform);
        }

        private void SetCameraTarget(Transform transform)
        {
            CameraFollow camera = this.followCamera.GetComponent<CameraFollow>();
            camera.target = transform;
        }

        internal void InstantiateArmy(Army army, Vector3 worldVector)
        {
            GameObject armyPrefab = armyFactory.FindGameObjectKind(army);
            if (armyPrefab == null)
            {
                Debug.LogFormat($"GameObject not found: {army.ShortName}_{army.Clan.ShortName}");
                return;
            }

            GameObject go = Instantiate<GameObject>(armyPrefab, worldVector, Quaternion.identity, WorldTilemap.transform);
            go.SetActive(false);

            // Add to the instantiated army to dictionary for tracking
            ArmyGameObject ago = new ArmyGameObject(army, go);
            armyDictionary.Add(army.Id, ago);
        }

        private void CreateDefaultArmies()
        {
            // Ready Player One
            Player player1 = Game.Current.Players[0];
            player1.HireHero(World.Current.Map[1, 1]);

            player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), World.Current.Map[1, 2]);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), World.Current.Map[1, 2]);

            player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), World.Current.Map[2, 1]);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), World.Current.Map[2, 1]);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), World.Current.Map[2, 1]);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), World.Current.Map[2, 1]);

            player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);
            player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);

            // Ready Player Two
            Player player2 = Game.Current.Players[1];
            player2.Clan.IsHuman = false;
            player2.HireHero(World.Current.Map[18, 10]);
            player2.HireHero(World.Current.Map[1, 3]);
            player2.HireHero(World.Current.Map[2, 3]);
        }

        public enum AttackResult
        {
            Battling,
            AttackerWon,
            DefenderWon
        }
    }
}
