using Assets.Scripts.CommandProcessors;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.UI;
using Assets.Scripts.UnityGame.Persistance.Entities;
using System;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using Wism.Client.CommandProcessors; // Updated
using Wism.Client.Commands; // Updated
using Wism.Client.Controllers; // Updated
using Wism.Client.MapObjects;
using Wism.Client.Pathing;
using ILogger = Wism.Client.Common.ILogger;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Unity game is the primary GameObject and bridge to the Unity UI and WISM API via GameManager
    /// </summary>
    public class UnityManager : MonoBehaviour
    {
        private int lastCommandId = 0;
        private ILogger logger;
        private ControllerProvider provider;
        private List<ICommandProcessor> commandProcessors;

        // Bootstrapping
        private static UnityNewGameEntity gameSettings;

        // Game managers
        [SerializeField]
        private WorldTilemap worldTilemap;
        private InputManager inputManager;
        private DebugManager debugManager;
        private GameManager gameManager;
        private ArmyManager armyManager;

        // UI panels
        [SerializeField]
        private GameObject warPanelPrefab;
        [SerializeField]
        private WarPanel warPanel;
        [SerializeField]
        private GameObject armyPickerPrefab;
        private ArmyPicker armyPicker;
        [SerializeField]
        private GameObject itemPickerPrefab;

        private ItemPicker itemPicker;
        [SerializeField]
        private GameObject saveLoadPickerPrefab;
        private SaveLoadPicker saveLoadPicker;
        private GameObject productionPanel;

        // UI elements
        public GameObject SelectedBoxPrefab;
        private SelectedArmyBox selectedArmyBox;
        private Camera mainCamera;
        private CameraFollow cameraFollow;

        private bool isInitialized;
        private bool showDebugError = false;
        private ExecutionMode executionMode;
        private ProductionMode productionMode;
        private bool interactiveUI = true;

        public List<Army> CurrentAttackers { get; set; }
        public List<Army> CurrentDefenders { get; set; }

        public GameManager GameManager { get => this.gameManager; set => this.gameManager = value; }
        public WorldTilemap WorldTilemap { get => this.worldTilemap; set => this.worldTilemap = value; }
        public WarPanel WarPanel { get => this.warPanel; set => this.warPanel = value; }
        public ProductionMode ProductionMode { get => this.productionMode; set => this.productionMode = value; }
        public ExecutionMode ExecutionMode { get => this.executionMode; set => this.executionMode = value; }
        public InputManager InputManager { get => this.inputManager; set => this.inputManager = value; }
        public ItemPicker ItemPicker { get => this.itemPicker; set => this.itemPicker = value; }
        public SaveLoadPicker SaveLoadPicker { get => this.saveLoadPicker; set => this.saveLoadPicker = value; }
        public int LastCommandId { get => this.lastCommandId; set => this.lastCommandId = value; }
        public DebugManager DebugManager { get => this.debugManager; set => this.debugManager = value; }
        public bool InteractiveUI { get => this.interactiveUI; set => this.interactiveUI = value; }

        public void Start()
        {
            Initialize(gameSettings);
        }

        public static void SetNewGameSettings(UnityNewGameEntity settings)
        {
            gameSettings = settings;
        }

        internal bool IsInitalized()
        {
            bool result = true;

            if (!Game.IsInitialized() ||
                !this.isInitialized ||
                this.ExecutionMode == ExecutionMode.NotStarted)
            {
                if (this.showDebugError)
                {
                    Debug.LogError("Game not initialized");
                    this.DebugManager.LogInformation("Failed to initialize");
                    this.showDebugError = false;
                }

                result = false;
            }

            return result;
        }

#if UNITY_EDITOR
        public void InitializeEditor()
        {
            IntializeWismApi();

            this.DebugManager.LogInformation("Editor initialization complete");

            this.isInitialized = true;
            this.ExecutionMode = ExecutionMode.Editor;
        }
#endif

        public void Initialize(UnityNewGameEntity gameSettings)
        {
            if (gameSettings == null)
            {
                gameSettings = UnityGameFactory.CreateDefaultGameSettings();
            }

            IntializeWismApi();
            InitializeCommandProcessors();
            InitializeUI();
            IntializeWismGame(gameSettings);

            this.DebugManager.LogInformation("Initialization complete");

            this.isInitialized = true;
            this.ExecutionMode = ExecutionMode.Bootstrap;
        }

        private void IntializeWismGame(UnityNewGameEntity gameSettings)
        {
            if (gameSettings == null)
            {
                throw new ArgumentNullException(nameof(gameSettings));
            }


            if (gameSettings.IsNewGame)
            {
                this.DebugManager.LogInformation("Starting a new game...");
                GetComponent<UnityGameFactory>().CreateGame(gameSettings);
            }
            else
            {
                this.DebugManager.LogInformation("Loading a game...");
                GetComponent<UnityGameFactory>().LoadNewGame();
            }
        }

        private void InitializeUI()
        {
            SetTime(this.GameManager.StandardTime);
            SetupCameras();

            this.armyManager = GetComponent<ArmyManager>();
            this.inputManager = GetComponent<InputManager>();

            this.WarPanel = this.warPanelPrefab.GetComponent<WarPanel>();
            this.armyPicker = this.armyPickerPrefab.GetComponent<ArmyPicker>();
            this.ItemPicker = this.itemPickerPrefab.GetComponent<ItemPicker>();
            this.SaveLoadPicker = this.saveLoadPickerPrefab.GetComponent<SaveLoadPicker>();
            this.productionPanel = UnityUtilities.GameObjectHardFind("CityProductionPanel");
            this.DebugManager.LogInformation("Initialized UI");
        }

        private void InitializeCommandProcessors()
        {
            this.commandProcessors = new List<ICommandProcessor>()
            {
                // General processors
                new SelectArmyProcessor(this.GameManager.LoggerFactory, this),
                
                // Battle processors
                new PrepareForBattleProcessor(this.GameManager.LoggerFactory, this),
                new BattleProcessor(this.GameManager.LoggerFactory, this),
                new CompleteBattleProcessor(this.GameManager.LoggerFactory, this),
                
                // Turn processors
                new StartTurnProcessor(this.GameManager.LoggerFactory, this),
                new RecruitHeroProcessor(this.GameManager.LoggerFactory, this),
                new HireHeroProcessor(this.GameManager.LoggerFactory, this),
                new RenewProductionProcessor(this.GameManager.LoggerFactory, this),
                new EndTurnProcessor(this.GameManager.LoggerFactory, this),

                // Search processors
                new SearchTempleProcessor(this.GameManager.LoggerFactory, this),
                new SearchRuinsProcessor(this.GameManager.LoggerFactory, this),
                new SearchLibraryProcessor(this.GameManager.LoggerFactory, this),
                new SearchSageProcessor(this.GameManager.LoggerFactory, this),

                // City processors
                new BuildCityDefensesProcessor(this.GameManager.LoggerFactory, this),
                new RazeCityDefensesProcessor(this.GameManager.LoggerFactory, this),

                // Game processors
                new LoadGameProcessor(this.GameManager.LoggerFactory, this),

                // Default processor
                new StandardProcessor(this.GameManager.LoggerFactory)
            };
            this.DebugManager.LogInformation("Initialized Command Processors");
        }

        private void IntializeWismApi()
        {
            this.GameManager = GetComponent<GameManager>();
            this.GameManager.Initialize();
            this.logger = this.GameManager.LoggerFactory.CreateLogger();
            this.provider = this.GameManager.ControllerProvider;

            this.DebugManager = GetComponent<DebugManager>();
            this.DebugManager.Initialize(this.GameManager.LoggerFactory);
            this.DebugManager.LogInformation("Initialized GameManager: " + this.GameManager.ModPath);
        }

        /// <summary>
        /// Resets the game state for new games, game loads.
        /// </summary>
        public void Reset()
        {
            this.InputManager.SetInputMode(InputMode.Game);
            this.armyManager.Reset();
            GetComponent<CityManager>().Reset();
            GetComponent<ItemManager>().Reset();
            NotifyUser("Game loaded successfully!");
        }

        internal void TogglePathing()
        {
            if (Game.Current.PathingStrategy is DijkstraPathingStrategy)
            {
                Game.Current.PathingStrategy = new AStarPathingStrategy();
                Debug.Log("Switched to A* Pathing Strategy");
            }
            else
            {
                Game.Current.PathingStrategy = new DijkstraPathingStrategy();
                Debug.Log("Switched to Dijkstra Pathing Strategy");
            }
        }

        public void GoToCapitol(Player player)
        {
            var inputHandler = GetComponent<InputManager>().InputHandler;
            inputHandler.CenterOnTile(player.Capitol.Tile);
        }

        public void GoToLocation()
        {
            this.inputManager.SetInputMode(InputMode.LocationPicker);
        }

        public void FixedUpdate()
        {
            try
            {
                switch (this.ExecutionMode)
                {
                    // Bootstrap game
                    case ExecutionMode.Bootstrap:
                        DoTasks();
                        this.ExecutionMode = ExecutionMode.Starting;
                        break;

                    case ExecutionMode.Starting:
                        InitializeSelectedArmyBox();

                        // TESTING ONLY //////////
                        // TODO: Remove                        
                        Game.Current.PathingStrategy = new AStarPathingStrategy();
                        //////////////////////////

                        // Start first turn
                        this.GameManager.StartTurn(Game.Current.GetCurrentPlayer());
                        this.ExecutionMode = ExecutionMode.Running;
                        break;

                    // Standard game loop
                    case ExecutionMode.Running:
                        Draw();
                        DoTasks();
                        this.armyManager.CleanupArmies();
                        break;

                    case ExecutionMode.NotStarted:
                    default:
                        // Do nothing
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                this.DebugManager.LogInformation(ex.Message);
                throw;
            }
        }

        private void InitializeSelectedArmyBox()
        {
            var startingTile = Game.Current.GetCurrentPlayer().Capitol.Tile;
            var worldVector = this.WorldTilemap.ConvertGameToUnityVector(startingTile.X, startingTile.Y);
            this.selectedArmyBox = Instantiate<GameObject>(this.SelectedBoxPrefab, worldVector, Quaternion.identity, this.WorldTilemap.transform).GetComponent<SelectedArmyBox>();
        }

        internal void Draw()
        {
            DrawSelectedArmiesBox();
        }

        /// <summary>
        /// Execute the commands from the UI, AI, or other devices
        /// </summary>
        private void DoTasks()
        {
            ActionState result = ActionState.NotStarted;

            int nextCommand = this.LastCommandId + 1;
            if (!this.provider.CommandController.CommandExists(nextCommand))
            {
                // Nothing to do
                return;
            }

            // Retrieve next command
            var command = this.provider.CommandController.GetCommand(nextCommand);
            this.DebugManager.LogInformation($"{command}");

            // Execute next command
            foreach (var processor in this.commandProcessors)
            {
                if (processor.CanExecute(command))
                {
                    var startTime = DateTime.Now;
                    result = processor.Execute(command);
                    var stopTime = DateTime.Now;
                    this.logger.LogInformation("Command duration: " + (stopTime - startTime).TotalMilliseconds + "ms");
                    break;
                }
            }

            // Process the result
            if (result == ActionState.Succeeded)
            {
                this.logger.LogInformation($"Task successful");
                this.LastCommandId = command.Id;
            }
            else if (result == ActionState.Failed)
            {
                this.logger.LogInformation($"Task failed");
                this.LastCommandId = command.Id;
            }
            else if (result == ActionState.InProgress)
            {
                this.logger.LogInformation("Task started and in progress");
                // Do nothing; do not advance Command ID
            }
        }

        internal void LogInformation(string message, params object[] args)
        {
            if (this.DebugManager != null)
            {
                this.DebugManager.LogInformation(message, args);
            }
            else
            {
                Debug.Log(String.Format(message, args));
            }
        }

        internal void SetCameraToSelectedBox()
        {
            this.cameraFollow.ResetCamera();
            SetCameraTarget(this.selectedArmyBox.transform);
        }

        internal void ShowProductionPanel(City city)
        {
            this.productionPanel.GetComponent<CityProduction>()
                            .Initialize(this, city);
            this.productionPanel.SetActive(true);
        }

        internal Camera GetMainCamera()
        {
            return this.mainCamera;
        }

        internal void HideSelectedBox()
        {
            this.selectedArmyBox.HideSelectedBox();
        }

        internal void SetSelectedBoxPosition(Vector3 worldVector, bool isActive)
        {
            this.selectedArmyBox.SetActive(isActive);
            this.selectedArmyBox.transform.position = worldVector;
        }

        internal Vector2Int GetSelectedBoxGamePosition()
        {
            return this.worldTilemap.ConvertUnityToGameVector(this.selectedArmyBox.transform.position);
        }

        public void SetTime(float time)
        {
            Time.fixedDeltaTime = time;
        }

        public void SetProductionMode(ProductionMode mode)
        {
            if (Game.Current.GameState == GameState.SelectedArmy)
            {
                this.inputManager
                    .InputHandler.DeselectObject();
            }

            this.ProductionMode = mode;
        }

        internal void ClearInfoPanel()
        {
            this.inputManager.InputHandler.SetCurrentTile(null);
            var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                   .GetComponent<NotificationBox>();
            messageBox.Notify("");
        }

        public void NotifyUser(string message, params object[] args)
        {
            var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                   .GetComponent<NotificationBox>();
            messageBox.Notify(String.Format(message, args));
        }

        private void DrawSelectedArmiesBox()
        {
            if (this.selectedArmyBox == null)
            {
                return;
            }

            if (this.InputManager.GetInputMode() != InputMode.Game)
            {
                return;
            }

            this.selectedArmyBox.Draw(this);
        }

        internal void HandleArmyPicker()
        {
            if (Game.Current.GameState == GameState.SelectedArmy)
            {
                var armiesToPick = Game.Current.GetSelectedArmies()[0].Tile.GetAllArmies();
                this.armyPicker.Initialize(this, armiesToPick);
            }
        }

        internal void HandleItemPicker(bool takingItems)
        {
            var mode = (takingItems) ? InputMode.ItemTakePicker : InputMode.ItemDropPicker;
            this.InputManager.SetInputMode(mode);
        }

        internal void HandleSaveLoadPicker(bool saving)
        {
            var mode = (saving) ? InputMode.SaveGamePicker : InputMode.LoadGamePicker;
            this.InputManager.SetInputMode(mode);
        }

        public void HandlePetCompanion()
        {
            if (Game.Current.GameState == GameState.SelectedArmy)
            {
                var selected = Game.Current.GetSelectedArmies();
                var hero = selected.Find(army => army is Hero);
                if (hero != null)
                {
                    NotifyUser(
                        ((Hero)hero).GetCompanionInteraction());
                }
            }
        }

        internal void ToggleMinimap()
        {
            GameObject map = UnityUtilities.GameObjectHardFind("MinimapPanel");
            map.SetActive(!map.activeSelf);
        }

        internal void ToggleHelp()
        {
            GameObject help = UnityUtilities.GameObjectHardFind("HelpText");
            help.SetActive(!help.activeSelf);
        }

        private void SetupCameras()
        {
            this.cameraFollow = UnityUtilities.GameObjectHardFind("MainCamera")
                .GetComponent<CameraFollow>();

            foreach (Camera camera in Camera.allCameras)
            {
                if (camera.name == "MainCamera")
                {
                    this.mainCamera = camera;
                }
            }

            if (this.mainCamera == null)
            {
                throw new InvalidOperationException("Could not find the MainCamera.");
            }
        }

        internal void SetCameraTarget(Transform transform)
        {
            this.cameraFollow.target = transform;
        }
    }
}
