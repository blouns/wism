using Assets.Scripts.Units;
using System;
using System.Collections.Generic;
using UnityEngine;
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
    public class UnityGame : MonoBehaviour
    {
        private int lastCommandId = 0;
        private ILogger logger;
        private ControllerProvider provider;

        public GameObject[] ArmyKinds;          // Prefabs for units; TODO: automate creation of prefabs
        public GameObject ArmyPrefab;
        public WorldTilemap WorldTilemap;
        public GameManager GameManager;

        [SerializeField]
        private GameObject warPanelPrefab;
        [SerializeField]
        private GameObject unitPickerPrefab;
        private WarPanel WarPanel;
        private UnitPicker ArmyPickerPanel;

        private Camera followCamera;
        private World world;
        private List<Player> players;
        private ArmyFactory armyFactory;
        private readonly Dictionary<int, ArmyGameObject> armyDictionary = new Dictionary<int, ArmyGameObject>();

        private ArmyGameObject selectedArmyGo;
        public GameObject SelectedBoxPrefab;
        private GameObject selectedArmyBox;

        //public ArmyGameObject SelectedArmyGameObject { get => selectedArmyGo; set => selectedArmyGo = value; }

        private AttackResult attackResult;
        bool armyPickerActive = false;
        bool initialized = false;

        public void Start()
        {
            GameManager.Initialize();
            Initialize(GameManager.LoggerFactory, GameManager.ControllerProvider);
        }

        public void Update()
        {
            if (!initialized)
            {
                return;
            }

            RunOnce();
        }

        public void FixedUpdate()
        {
            if (!initialized)
            {
                return;
            }
        }

        private void SetTime(float time)
        {
            Time.fixedDeltaTime = time;
        }

        public void Initialize(ILoggerFactory loggerFactory, ControllerProvider provider)
        {
            this.logger = loggerFactory.CreateLogger();
            this.provider = provider;

            // Set up game UI
            SetTime(GameManager.StandardTime);
            SetupCameras();            
            WarPanel = this.warPanelPrefab.GetComponent<WarPanel>();
            ArmyPickerPanel = this.unitPickerPrefab.GetComponent<UnitPicker>();

            // Set up game            
            world = WorldTilemap.CreateWorldFromScene();
            players = Game.Current.Players;
            CreateDefaultArmies();

            armyFactory = ArmyFactory.Create(ArmyKinds);
            CreateArmyGameObjects();
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

        /// <summary>
        /// Run one iteration of the game loop
        /// </summary>
        public void RunOnce()
        {
            try
            {
                Draw();
                HandleInput();
                DoTasks();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
        }

        private void DoTasks()
        {
            foreach (Command command in provider.CommandController.GetCommandsAfterId(lastCommandId))
            {
                logger.LogInformation($"Task executing: {command.Id}: {command.GetType()}");

                // Run the command
                var result = command.Execute();

                // Process the result
                // TODO: Perhaps shift to using GameState instead of ActionState?
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
                    // Do NOT advance Command ID; we are still processing this command                    
                }
            }
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Tile clickedTile = WorldTilemap.GetClickedTile(followCamera);
                Debug.Log("Clicked on " + clickedTile.Terrain.DisplayName);

                switch (Game.Current.GameState)
                {
                    case GameState.Ready:
                        if (!armyPickerActive)
                        {
                            SelectObject(clickedTile);
                        }
                        break;

                    case GameState.SelectedArmy:
                        // TODO: Second click selects top unit in army
                        // TODO: Double-click selects entire army

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

            }
            else if (Input.GetMouseButtonDown(1))
            {
                if (Game.Current.GameState == GameState.SelectedArmy)
                {
                    //DeselectObject();
                }
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                if (Game.Current.GameState == GameState.SelectedArmy)
                {
                    //ArmyPickerPanel.Initialize(this.SelectedArmy.Army, this.armyKinds);
                    //this.InputState = InputState.SelectingArmys;
                }
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                ToggleMinimap();
            }
        }

        private void Draw()
        {
            // Do nothing
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
        */


        /*
        private void MoveArmies()
        {
            if (Game.Current.GameState == GameState.MovingArmy)
            {
                GameManager.MoveSelectedArmies()
            }
            else if (Game.Current.GameState == GameState.AttackingArmy)
            {
                AttackArmy(this.SelectedArmy);
            }
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

        private void MoveArmySelectedArmiesOneStep(ArmyGameObject armyGO)
        {
            SetTime(GameManager.StandardTime);
            SetCameraTarget(armyGO.GameObject.transform);

            if (Game.Current.GameState != GameState.SelectedArmy)
            {
                Debug.Log("You must first select an army.");
            }


        }

        /*
        private void MoveArmy(ArmyGameObject armyGO)
        {
            SetTime(GameManager.StandardTime);
            var armies = Game.Current.GetSelectedArmies();
            var armyGo = armyFactory.FindGameObjectKind(armies[0]);
            SetCameraTarget(armyGO.GameObject.transform);
            if (armyGO.Path == null)
            {
                armyGO.Armies.FindPath(armyGO.TargetTile, out armyGO.Path, out float distance);
                if (armyGO.Path.Count < 2)
                {
                    Debug.Log("Impossible route.");
                    return;
                }
            }

            // Check if the next tile contains an enemy army; cannot move onto an enemy army without explicity attacking
            if ((armyGO.Path.Count > 1) && MovingOntoEnemy(SelectedArmy.Armies, armyGO.Path[1]))
            {
                Debug.LogFormat("Enemy detected at {0}.", armyGO.TargetTile.Coordinates);
                armyGO.Path = null;
                armyGO.TargetTile = null;
                SelectObject(SelectedArmy.Armies.Tile);
            }
            // Try to move the army one step
            else if (!armyGO.Armies.TryMoveOneStep(armyGO.TargetTile, ref armyGO.Path, out float distance))
            {
                // Done moving due to path completion, out of moves, or hit barrier
                if (armyGO.Path != null && armyGO.Path.Count == 0)
                {
                    Debug.LogFormat("Moved {0} to {1}", armyGO.Armies, armyGO.TargetTile.Coordinates);
                }
                else
                {
                    Debug.LogFormat("Cannot move {0} to {1}.", armyGO.Armies, armyGO.TargetTile.Coordinates);
                }

                if (armyGO.Armies.MovesRemaining == 0)
                {
                    InputState = InputState.Unselected;
                }
                else
                {
                    armyGO.Path = null;
                    armyGO.TargetTile = null;
                    SelectObject(SelectedArmy.Armies.Tile);
                }
            }
            // Continue moving along the path
            else
            {
                Vector3 vector = ConvertGameToUnityCoordinates(armyGO.Armies.GetCoordinates());

                if (armyGO.GameObject == null)
                {
                    Debug.LogErrorFormat("Trying to move a destroyed object: {0}", armyGO.Armies.Guid);
                }
                Rigidbody2D rb = armyGO.GameObject.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.transform.position = vector;
                }
            }

            Debug.LogFormat("Moves remaining: {0}", armyGO.Armies.MovesRemaining);
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

        private void CreateArmyGameObjects()
        {
            // Verify that army kinds were loaded
            if (ArmyKinds == null || ArmyKinds.Length == 0)
            {
                Debug.Log("No army kinds found.");
                return;
            }

            foreach (Player player in Game.Current.Players)
            {
                // Create all the objects
                foreach (Army army in player.GetArmies())
                {
                    // Find or create and set up the GameObject connected to WISM MapObject
                    if (!armyDictionary.ContainsKey(army.Id))
                    {
                        Vector3 worldVector = WorldTilemap.ConvertGameToUnityCoordinates(army.X, army.Y);
                        InstantiateArmy(army, worldVector);
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
                    ago.GameObject.SetActive(true);
                }
            }

        }

        private void SelectObject(Tile clickedTile)
        {
            // If tile contains an army, select it     
            if (clickedTile.HasArmies())
            {
                List<Army> armies = clickedTile.Armies;
                Army army = armies[0];

                if (!armyDictionary.ContainsKey(army.Id))
                {
                    throw new InvalidOperationException("Could not find selected army in game objects.");
                }

                //this.SelectedArmyGameObject = armyDictionary[army.Id];
                Vector3 worldVector = WorldTilemap.ConvertGameToUnityCoordinates(army.X, army.Y);
                this.selectedArmyBox = Instantiate<GameObject>(SelectedBoxPrefab, worldVector, Quaternion.identity, WorldTilemap.transform);
                this.selectedArmyBox.SetActive(true);
                SetCameraTarget(this.selectedArmyBox.transform);
            }
            else
            {
                Vector3 worldVector = WorldTilemap.ConvertGameToUnityCoordinates(clickedTile.X, clickedTile.Y);
                GameObject go = Instantiate<GameObject>(SelectedBoxPrefab, worldVector, Quaternion.identity, WorldTilemap.transform);
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

        private void MoveSelectedArmyTo(Tile targetTile)
        {
            // Move the selected unit to the clicked tile location
            GameManager.MoveSelectedArmies(targetTile.X, targetTile.Y);

            //if (this.SelectedArmy == null)
            //    throw new InvalidOperationException("Selected Army was null.");

            //if (this.SelectedArmyGameObject.Army == targetTile.Armies)
            //    return;

            //this.SelectedArmy.Path = null;
            //this.SelectedArmy.TargetTile = targetTile;
            //this.InputState = GameState.MovingArmy;
        }        

        internal void DeselectObject()
        {
            GameManager.DeselectArmies();

            //this.SelectedArmyGameObject = null;
            Destroy(this.selectedArmyBox);
            SetTime(GameManager.StandardTime);
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

        public enum AttackResult
        {
            Battling,
            AttackerWon,
            DefenderWon
        }
    }
}
