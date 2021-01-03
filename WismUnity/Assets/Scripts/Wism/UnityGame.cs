using Assets.Scripts.CommandProcessors;
using Assets.Scripts.UI;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.Units;
using System;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using ILogger = Wism.Client.Common.ILogger;
using Tile = Wism.Client.Core.Tile;
using Assets.Scripts.Worlds;

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
        private CityManager cityManager;

        [SerializeField]
        private GameObject warPanelPrefab;        
        internal WarPanel WarPanel;
        [SerializeField]
        private GameObject armyPickerPrefab;
        private ArmyPicker ArmyPickerPanel;

        private List<ICommandProcessor> commandProcessors;

        private Camera followCamera;
        private ArmyFactory armyFactory;
        private readonly Dictionary<int, ArmyGameObject> armyDictionary = new Dictionary<int, ArmyGameObject>();

        public GameObject SelectedBoxPrefab;
        private SelectedArmyBox selectedArmyBox;
        private int selectedArmyIndex;

        // Input handling
        private readonly Timer mouseSingleClickTimer = new Timer();
        private bool singleClickProcessed;

        public List<Army> CurrentAttackers { get; set; }
        public List<Army> CurrentDefenders { get; set; }

        public bool SelectingArmies { get; set; }

        public Dictionary<int, ArmyGameObject> ArmyDictionary => armyDictionary;

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
                CleanupArmies();
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

        /// <summary>
        /// Process keyboard and mouse input, including single and double click handling
        /// </summary>
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
                Draw();
            }
            else if (Input.GetMouseButtonDown(0))
            {
                // Detect single vs. double-click
                if (mouseSingleClickTimer.Enabled == false)
                {
                    mouseSingleClickTimer.Start();
                    // Wait for double click
                    return;
                }
                else
                {
                    // Double click performed, so cancel single click
                    mouseSingleClickTimer.Stop();

                    HandleLeftClick(true);
                    Draw();
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
                Draw();
            }
            else
            {
                HandleKeyboard();
                Draw();
            }
        }

        private void Draw()
        {
            DrawSelectedArmiesBox();
            DrawArmyGameObjects();
            this.cityManager.DrawCities();
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

            // Create command processors:
            // Commands are proccessed by processors. All commands can be handled by the
            // StandardCommand processor, but special behavior or cut-scenes can be driven
            // by using specialized processors (e.g. battle cut scene).
            this.commandProcessors = new List<ICommandProcessor>()
            {
                new PrepareForBattleProcessor(loggerFactory, this),
                new BattleProcessor(loggerFactory, this),
                new CompleteBattleProcessor(loggerFactory, this),
                new StandardProcessor(loggerFactory)
            };

            Vector3 worldVector = WorldTilemap.ConvertGameToUnityCoordinates(1, 1);
            this.selectedArmyBox = Instantiate<GameObject>(SelectedBoxPrefab, worldVector, Quaternion.identity, WorldTilemap.transform).GetComponent<SelectedArmyBox>();
            this.selectedArmyIndex = -1;

            // Set up default game (for testing purposes only)
            World.CreateWorld(WorldTilemap.CreateWorldFromScene().Map);
            armyFactory = ArmyFactory.Create(ArmyKinds);
            CreateDefaultCities();
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
            ActionState result = ActionState.NotStarted;

            int nextCommand = lastCommandId + 1;

            if (!provider.CommandController.CommandExists(nextCommand))
            {
                // Nothing to do
                return;
            }

            // Retrieve next command
            var command = provider.CommandController.GetCommand(nextCommand);
            logger.LogInformation($"Task executing: {command.Id}: {command.GetType()}");
            Debug.Log($"Pre-command GameState: {Game.Current.GameState}");
            Debug.Log($"{command}");

            // Execute next command
            foreach (var processor in this.commandProcessors)
            {
                if (processor.CanExecute(command))
                {
                    result = processor.Execute(command);
                    break;
                }
            }

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

        public void SetTime(float time)
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
            else if (Input.GetKeyDown(KeyCode.N))
            {
                GameManager.SelectNextArmy();
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
                        break;
                    }

                    // Move or attack; can only attack from adjacent tiles
                    var armies = Game.Current.GetSelectedArmies();
                    bool isAttacking = clickedTile.CanAttackHere(armies);
                    bool isAdjacent = clickedTile.IsNeighbor(armies[0].Tile);
                    if (isAttacking && isAdjacent)
                    {
                        // War!
                        GameManager.AttackWithSelectedArmies(clickedTile.X, clickedTile.Y);
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
                        GameManager.MoveSelectedArmies(clickedTile.X, clickedTile.Y);
                    }
                    break;
            }

            Draw();
        }

        private void DrawSelectedArmiesBox()
        {
            if (this.selectedArmyBox == null)
            {
                throw new InvalidOperationException("Selected army box was null.");
            }

            this.selectedArmyBox.Draw(this);
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

        private void CleanupArmies()
        {
            // Find and cleanup stale game objects
            var toDelete = new List<int>(ArmyDictionary.Keys);
            foreach (Player player in Game.Current.Players)
            {
                IList<Army> armies = player.GetArmies();
                foreach (Army army in armies)
                {
                    if (ArmyDictionary.ContainsKey(army.Id))
                    {
                        // Found the army so don't remove it
                        toDelete.Remove(army.Id);
                    }
                }
            }

            // Remove objects missing from the game
            toDelete.ForEach(id =>
            {
                Destroy(ArmyDictionary[id].GameObject);
                ArmyDictionary.Remove(id);
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
                    if (!ArmyDictionary.ContainsKey(army.Id))
                    {
                        Vector3 worldVector = WorldTilemap.ConvertGameToUnityCoordinates(army.X, army.Y);
                        InstantiateArmy(army, worldVector);
                    }
                    else
                    {
                        ArmyDictionary[army.Id].GameObject.SetActive(false);
                    }
                }

                // Draw only the "top" army for each army stack on the map
                foreach (Tile tile in World.Current.Map)
                {
                    int armyId;
                    // Draw visiting armies over stationed armies
                    if (tile.HasVisitingArmies() && ArmyDictionary.ContainsKey(tile.VisitingArmies[0].Id))
                    {
                        armyId = tile.VisitingArmies[0].Id;
                        SetCameraTarget(ArmyDictionary[armyId].GameObject.transform);
                    }
                    else if (tile.HasArmies() && this.ArmyDictionary.ContainsKey(tile.Armies[0].Id))
                    {
                        armyId = tile.Armies[0].Id;
                    }
                    else
                    {
                        // Not a "top" army                        
                        continue;
                    }
                    
                    ArmyGameObject ago = this.ArmyDictionary[armyId];
                    Vector3 vector = WorldTilemap.ConvertGameToUnityCoordinates(ago.Army.X, ago.Army.Y);
                    ago.GameObject.transform.position = vector;
                    ago.GameObject.SetActive(true);
                }
            }

        }

        private void SelectObject(Tile tile, bool selectAll)
        {
            // If no armies on selected tile then center screen
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
            if (Game.Current.ArmiesSelected())
            {
                GameManager.DeselectArmies();
            }

            this.selectedArmyBox.HideSelectedBox();
            this.selectedArmyIndex = -1;
        }

        private void CenterOnTile(Tile clickedTile)
        {
            Debug.Log($"Clicked on {World.Current.Map[clickedTile.X, clickedTile.Y]}");
            Vector3 worldVector = WorldTilemap.ConvertGameToUnityCoordinates(clickedTile.X, clickedTile.Y);
            this.selectedArmyBox.SetActive(false);
            this.selectedArmyBox.transform.position = worldVector;
            SetCameraTarget(this.selectedArmyBox.transform);
        }

        internal void SetCameraTarget(Transform transform)
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
            ArmyDictionary.Add(army.Id, ago);
        }

        /// <summary>
        /// Debug-only game setup
        /// </summary>
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

            player2.HireHero(World.Current.Map[4, 0]);
            player2.HireHero(World.Current.Map[4, 0]);

            player2.HireHero(World.Current.Map[4, 1]);

            player2.HireHero(World.Current.Map[2, 3]);
        }

        private void CreateDefaultCities()
        {
            MapBuilder.AddCitiesToMapFromWorld(World.Current.Map, GameManager.DefaultWorld);
        }
    }
}
