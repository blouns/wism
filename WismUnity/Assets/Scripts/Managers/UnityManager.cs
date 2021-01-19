using Assets.Scripts.Armies;
using Assets.Scripts.CommandProcessors;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.UI;
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

namespace Assets.Scripts.Managers
{

    /// <summary>
    /// Unity game is the primary game loop and bridge to the Unity UI and WISM API via GameManager
    /// </summary>
    public class UnityManager : MonoBehaviour
    {
        private int lastCommandId = 0;
        private ILogger logger;
        private ControllerProvider provider;

        [SerializeField]
        private WorldTilemap worldTilemap;        
        [SerializeField]
        private CityManager cityManager;
        private GameManager gameManager;
        private ArmyManager armyManager;

        [SerializeField]
        private GameObject warPanelPrefab;     
        [SerializeField]
        private WarPanel warPanel;
        [SerializeField]
        private GameObject armyPickerPrefab;
        private ArmyPicker armyPickerPanel;
        private GameObject productionPanel;

        private List<ICommandProcessor> commandProcessors;

        private Camera followCamera;
        private readonly Dictionary<int, ArmyGameObject> armyDictionary = new Dictionary<int, ArmyGameObject>();

        public GameObject SelectedBoxPrefab;
        private SelectedArmyBox selectedArmyBox;
        private int selectedArmyIndex;

        // Input handling
        private readonly Timer mouseSingleLeftClickTimer = new Timer();
        private bool singleLeftClickProcessed;
        private readonly Timer mouseRightClickHoldTimer = new Timer();
        private bool holdingRightButton;
        private bool acceptingInput = true;
        private bool skipInput;

        private bool showDebugError = true;
        private ProductionMode productionMode;

        public List<Army> CurrentAttackers { get; set; }
        public List<Army> CurrentDefenders { get; set; }

        public bool SelectingArmies { get; set; }

        public Dictionary<int, ArmyGameObject> ArmyDictionary => armyDictionary;

        public GameManager GameManager { get => gameManager; set => gameManager = value; }
        public WorldTilemap WorldTilemap { get => worldTilemap; set => worldTilemap = value; }
        public WarPanel WarPanel { get => warPanel; set => warPanel = value; }
        public ProductionMode ProductionMode { get => productionMode; set => productionMode = value; }

        public void Start()
        {
            // Initialize WISM API
            this.GameManager = GetComponent<GameManager>();
            this.GameManager.Initialize();

            // Initialize Unity Game
            Initialize(GameManager.LoggerFactory, GameManager.ControllerProvider);
        }        

        private void Update()
        {
            if (!IsInitalized())
            {
                return;
            }

            HandleInput();
        }        

        public void FixedUpdate()
        {
            if (!IsInitalized())
            {     
                return;
            }

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

        private bool IsInitalized()
        {
            bool result = true;

            if (!Game.IsInitialized())
            {
                if (this.showDebugError)
                {
                    Debug.LogError("Game not initialized");
                    this.showDebugError = false;
                }

                result = false;
            }

            return result;
        }

        void SingleLeftClick(object o, System.EventArgs e)
        {
            mouseSingleLeftClickTimer.Stop();

            singleLeftClickProcessed = true;
        }

        void SingleRightClick(object o, System.EventArgs e)
        {
            holdingRightButton = true;
        }

        public void SetAcceptingInput(bool acceptingInput)
        {
            this.acceptingInput = acceptingInput;
        }

        public void SkipInput()
        {
            this.skipInput = true;
        }

        /// <summary>
        /// Process keyboard and mouse input, including single and double click handling
        /// </summary>
        private void HandleInput()
        {
            if (SelectingArmies || !this.acceptingInput || this.skipInput)
            {
                // Army picker or another control has focus
                this.skipInput = false;
                return;
            }                        

            if (singleLeftClickProcessed)
            {
                // Single left click performed
                HandleLeftClick();
                singleLeftClickProcessed = false;
                Draw();
            }
            else if (Input.GetMouseButtonDown(0))
            {
                // Detect single vs. double-click
                if (mouseSingleLeftClickTimer.Enabled == false)
                {
                    mouseSingleLeftClickTimer.Start();
                    // Wait for double click
                    return;
                }
                else
                {
                    // Double click performed, so cancel single click
                    mouseSingleLeftClickTimer.Stop();

                    HandleLeftClick(true);
                    Draw();
                }
            }            
            else
            {
                HandleKeyboard();
                Draw();
            }

            // Handle right-click (drag)
            if (Input.GetMouseButtonDown(1))
            {
                if (mouseRightClickHoldTimer.Enabled == false)
                {
                    mouseRightClickHoldTimer.Start();
                    // Wait for mouse up
                    return;
                }
            }
            else if (Input.GetMouseButtonUp(1))
            {
                mouseRightClickHoldTimer.Stop();

                if (!holdingRightButton)
                {
                    HandleRightClick();
                    Draw();
                }

                holdingRightButton = false;
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
            armyPickerPanel = this.armyPickerPrefab.GetComponent<ArmyPicker>();
            productionPanel = UnityUtilities.GameObjectHardFind("CityProductionPanel");

            // Create command processors
            this.commandProcessors = new List<ICommandProcessor>()
            {
                new SelectArmyProcessor(loggerFactory, this),
                new PrepareForBattleProcessor(loggerFactory, this),
                new BattleProcessor(loggerFactory, this),
                new CompleteBattleProcessor(loggerFactory, this),
                new StartTurnProcessor(loggerFactory, this),
                new StandardProcessor(loggerFactory)
            };

            Vector3 worldVector = WorldTilemap.ConvertGameToUnityCoordinates(1, 1);
            this.selectedArmyBox = Instantiate<GameObject>(SelectedBoxPrefab, worldVector, Quaternion.identity, WorldTilemap.transform).GetComponent<SelectedArmyBox>();
            this.selectedArmyIndex = -1;

            this.armyManager = GameObject.FindGameObjectWithTag("ArmyManager")
                .GetComponent<ArmyManager>();

            // Set up default game (for testing purposes only)
            World.CreateWorld(WorldTilemap.CreateWorldFromScene().Map);
            CreateDefaultCities();
            CreateDefaultArmies();            
            DrawArmyGameObjects();
            SelectObject(World.Current.Map[2, 1], true);

            mouseSingleLeftClickTimer.Interval = 400;
            mouseSingleLeftClickTimer.Elapsed += SingleLeftClick;

            mouseRightClickHoldTimer.Interval = 200;
            mouseRightClickHoldTimer.Elapsed += SingleRightClick;
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
                    armyPickerPanel.Initialize(this, armiesToPick);
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
            else if (Input.GetKeyDown(KeyCode.E))
            {
                GameManager.EndTurn();
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                SetProductionMode(ProductionMode.SelectCity);
            }
        }

        private void SetProductionMode(ProductionMode mode)
        {
            if (Game.Current.GameState == GameState.SelectedArmy)
            {
                DeselectObject();
            }

            this.ProductionMode = mode;
        }

        private void HandleRightClick()
        {
            // Cancel object selection
            if (Game.Current.GameState == GameState.SelectedArmy)
            {
                DeselectObject();
            }

            // Cancel city production selection
            if (this.ProductionMode == ProductionMode.SelectCity)
            {
                this.ProductionMode = ProductionMode.None;
            }
        }

        private void HandleLeftClick(bool isDoubleClick = false)
        {
            Tile clickedTile = WorldTilemap.GetClickedTile(followCamera);
            HandleArmyClick(isDoubleClick, clickedTile);
            HandleCityClick(clickedTile);
            Draw();
        }

        private void HandleArmyClick(bool isDoubleClick, Tile clickedTile)
        {
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
        }

        private void HandleCityClick(Tile tile)
        {
            switch (this.ProductionMode)
            {
                case ProductionMode.SelectCity:
                    if (tile.HasCity() &&
                        tile.City.Clan == Game.Current.GetCurrentPlayer().Clan)
                    {
                        // Launch production panel
                        productionPanel.GetComponent<CityProduction>()
                            .Initialize(this, tile.City);
                        productionPanel.SetActive(true);
                    }
                    break;
                default:
                    // Do nothing
                    break;
            }
        }

        private void DrawSelectedArmiesBox()
        {
            if (this.selectedArmyBox == null)
            {
                throw new InvalidOperationException("Selected army box was null.");
            }

            if (!this.acceptingInput)
            {
                return;
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
            GameObject map = UnityUtilities.GameObjectHardFind("MinimapPanel");
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
            // If no owned armies on selected tile then center screen
            if ((Game.Current.GameState == GameState.Ready) &&
                !tile.HasVisitingArmies() &&
                (!tile.HasArmies() || 
                    (tile.HasArmies() && 
                    tile.Armies[0].Player != Game.Current.GetCurrentPlayer())))
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
                CenterOnTile(tile);
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

        internal void CenterOnTile(Tile clickedTile)
        {
            Debug.Log(World.Current.Map[clickedTile.X, clickedTile.Y]);
            Vector3 worldVector = WorldTilemap.ConvertGameToUnityCoordinates(clickedTile.X, clickedTile.Y);

            followCamera.GetComponent<CameraFollow>()
                .SetCameraTarget(worldVector);

            this.selectedArmyBox.SetActive(false);
            this.selectedArmyBox.transform.position = worldVector;
        }

        internal void SetCameraTarget(Transform transform)
        {
            CameraFollow camera = this.followCamera.GetComponent<CameraFollow>();
            camera.target = transform;
        }

        internal void InstantiateArmy(Army army, Vector3 worldVector)
        {
            var armyGO = armyManager.Instantiate(army, worldVector, WorldTilemap.transform);
            armyGO.SetActive(false);

            // Add to the instantiated army to dictionary for tracking
            ArmyGameObject ago = new ArmyGameObject(army, armyGO);
            ArmyDictionary.Add(army.Id, ago);
        }

        /// <summary>
        /// Debug-only game setup
        /// </summary>
        private void CreateDefaultArmies()
        {
            // Ready Player One
            Player sirians = Game.Current.Players[0];
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), World.Current.Map[1, 2]);
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), World.Current.Map[1, 2]);

            sirians.HireHero(World.Current.Map[2, 1]);
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("Devils"), World.Current.Map[2, 1]);
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("Devils"), World.Current.Map[2, 1]);            
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), World.Current.Map[2, 1]);
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), World.Current.Map[2, 1]);
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), World.Current.Map[2, 1]);
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), World.Current.Map[2, 1]);

            sirians.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), World.Current.Map[9, 17]);

            sirians.ConscriptArmy(ModFactory.FindArmyInfo("Wizards"), World.Current.Map[9, 18]);
            sirians.ConscriptArmy(ModFactory.FindArmyInfo("Wizards"), World.Current.Map[9, 18]);

            // Ready Player Two
            Player stormGiants = Game.Current.Players[1];
            stormGiants.Clan.IsHuman = false;
            stormGiants.HireHero(World.Current.Map[18, 10]);
            stormGiants.ConscriptArmy(ModFactory.FindArmyInfo("GiantWarriors"), World.Current.Map[18, 10]);
            stormGiants.ConscriptArmy(ModFactory.FindArmyInfo("GiantWarriors"), World.Current.Map[18, 10]);
            stormGiants.ConscriptArmy(ModFactory.FindArmyInfo("GiantWarriors"), World.Current.Map[18, 10]);

            stormGiants.HireHero(World.Current.Map[4, 0]);
            stormGiants.HireHero(World.Current.Map[4, 0]);

            stormGiants.HireHero(World.Current.Map[4, 1]);

            stormGiants.HireHero(World.Current.Map[2, 3]);


            Player lordBane = Game.Current.Players[2];
            lordBane.ConscriptArmy(ModFactory.FindArmyInfo("Dragons"), World.Current.Map[6, 1]);
            lordBane.ConscriptArmy(ModFactory.FindArmyInfo("WolfRiders"), World.Current.Map[6, 1]);
            lordBane.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), World.Current.Map[6, 1]);
            lordBane.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), World.Current.Map[6, 1]);

            Player selentines = Game.Current.Players[3];
            selentines.HireHero(World.Current.Map[7, 1]);
            selentines.ConscriptArmy(ModFactory.FindArmyInfo("Demons"), World.Current.Map[7, 1]);
            selentines.ConscriptArmy(ModFactory.FindArmyInfo("Demons"), World.Current.Map[7, 1]);

            Player elvallie = Game.Current.Players[4];
            elvallie.ConscriptArmy(ModFactory.FindArmyInfo("ElvenArchers"), World.Current.Map[8, 1]);
            elvallie.ConscriptArmy(ModFactory.FindArmyInfo("ElvenArchers"), World.Current.Map[8, 1]);
            elvallie.ConscriptArmy(ModFactory.FindArmyInfo("ElvenArchers"), World.Current.Map[8, 1]);
            elvallie.ConscriptArmy(ModFactory.FindArmyInfo("ElvenArchers"), World.Current.Map[8, 1]);

        }

        private void CreateDefaultCities()
        {
            MapBuilder.AddCitiesToMapFromWorld(World.Current.Map, GameManager.DefaultWorld);
        }
    }
}
