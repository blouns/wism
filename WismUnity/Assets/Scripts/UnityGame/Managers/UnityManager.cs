using Assets.Scripts.CommandProcessors;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.UI;
using Assets.Scripts.UnityGame.Persistance.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.Entities;
using Wism.Client.MapObjects;
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
        private bool showDebugError = true;
        private ExecutionMode executionMode;
        private ProductionMode productionMode;

        public List<Army> CurrentAttackers { get; set; }
        public List<Army> CurrentDefenders { get; set; }        

        public GameManager GameManager { get => gameManager; set => gameManager = value; }
        public WorldTilemap WorldTilemap { get => worldTilemap; set => worldTilemap = value; }
        public WarPanel WarPanel { get => warPanel; set => warPanel = value; }
        public ProductionMode ProductionMode { get => productionMode; set => productionMode = value; }
        public ExecutionMode ExecutionMode { get => executionMode; set => executionMode = value; }
        public InputManager InputManager { get => inputManager; set => inputManager = value; }
        public ItemPicker ItemPicker { get => itemPicker; set => itemPicker = value; }
        public SaveLoadPicker SaveLoadPicker { get => saveLoadPicker; set => saveLoadPicker = value; }
        public int LastCommandId { get => lastCommandId; set => lastCommandId = value; }
        public DebugManager DebugManager { get => debugManager; set => debugManager = value; }        

        public void Awake()
        {
            // For calling UnityManager from other scenes
            DontDestroyOnLoad(this);
        }

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
                    DebugManager.LogInformation("Failed to initialize");
                    this.showDebugError = false;
                }

                result = false;
            }

            return result;
        }

        public void Initialize(UnityNewGameEntity gameSettings)
        {
            if (gameSettings == null)
            {
                gameSettings = GameFactory.CreateDefaultGameSettings();
            }

            IntializeWismApi();
            InitializeCommandProcessors();
            InitializeUI();
            IntializeWismGame(gameSettings);
            
            DebugManager.LogInformation("Initialization complete");

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
                DebugManager.LogInformation("Starting a new game...");
                GetComponent<GameFactory>().CreateGame(gameSettings);
            }
            else
            {
                DebugManager.LogInformation("Loading a game...");
                GetComponent<GameFactory>().LoadGame(gameSettings.WorldName);
            }
        }

        private void InitializeUI()
        {
            SetTime(GameManager.StandardTime);
            SetupCameras();

            this.armyManager = GetComponent<ArmyManager>();
            this.inputManager = GetComponent<InputManager>();

            WarPanel = this.warPanelPrefab.GetComponent<WarPanel>();
            armyPicker = this.armyPickerPrefab.GetComponent<ArmyPicker>();
            ItemPicker = this.itemPickerPrefab.GetComponent<ItemPicker>();
            SaveLoadPicker = this.saveLoadPickerPrefab.GetComponent<SaveLoadPicker>();
            productionPanel = UnityUtilities.GameObjectHardFind("CityProductionPanel");
            DebugManager.LogInformation("Initialized UI");
        }

        private void InitializeCommandProcessors()
        {
            this.commandProcessors = new List<ICommandProcessor>()
            {
                // General processors
                new SelectArmyProcessor(GameManager.LoggerFactory, this),
                
                // Battle processors
                new PrepareForBattleProcessor(GameManager.LoggerFactory, this),
                new BattleProcessor(GameManager.LoggerFactory, this),
                new CompleteBattleProcessor(GameManager.LoggerFactory, this),
                
                // Turn processors
                new StartTurnProcessor(GameManager.LoggerFactory, this),
                new RecruitHeroProcessor(GameManager.LoggerFactory, this),
                new HireHeroProcessor(GameManager.LoggerFactory, this),
                new RenewProductionProcessor(GameManager.LoggerFactory, this),
                new EndTurnProcessor(GameManager.LoggerFactory, this),

                // Search processors
                new SearchTempleProcessor(GameManager.LoggerFactory, this),
                new SearchRuinsProcessor(GameManager.LoggerFactory, this),
                new SearchLibraryProcessor(GameManager.LoggerFactory, this),
                new SearchSageProcessor(GameManager.LoggerFactory, this),

                // City processors
                new BuildCityDefensesProcessor(GameManager.LoggerFactory, this),
                new RazeCityDefensesProcessor(GameManager.LoggerFactory, this),

                // Game processors
                new LoadGameProcessor(GameManager.LoggerFactory, this),

                // Default processor
                new StandardProcessor(GameManager.LoggerFactory)
            };
            DebugManager.LogInformation("Initialized Command Processors");
        }

        private void IntializeWismApi()
        {
            this.GameManager = GetComponent<GameManager>();
            this.GameManager.Initialize();
            this.logger = this.GameManager.LoggerFactory.CreateLogger();
            this.provider = this.GameManager.ControllerProvider;

            this.DebugManager = GetComponent<DebugManager>();
            this.DebugManager.Initialize(this.GameManager.LoggerFactory);
            DebugManager.LogInformation("Initialized GameManager: " + GameManager.DefaultModPath);
        }

        /// <summary>
        /// Resets the game state for new games, game loads.
        /// </summary>
        public void Reset()
        {
            InputManager.SetInputMode(InputMode.Game);
            this.armyManager.Reset();
            GetComponent<CityManager>().Reset();
            GetComponent<ItemManager>().Reset();
            NotifyUser("Game loaded successfully!");
        }

        public void GoToCapitol(Player player)
        {
            var inputHandler = this.GetComponent<InputManager>().InputHandler;
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
                        var startingTile = Game.Current.GetCurrentPlayer().Capitol.Tile;
                        Vector3 worldVector = WorldTilemap.ConvertGameToUnityVector(startingTile.X, startingTile.Y);
                        this.selectedArmyBox = Instantiate<GameObject>(SelectedBoxPrefab, worldVector, Quaternion.identity, WorldTilemap.transform).GetComponent<SelectedArmyBox>();

                        // Start first turn
                        GameManager.StartTurn(Game.Current.GetCurrentPlayer());
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
                DebugManager.LogInformation(ex.Message);
                throw;
            }
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

            int nextCommand = LastCommandId + 1;
            if (!provider.CommandController.CommandExists(nextCommand))
            {
                // Nothing to do
                return;
            }

            // Retrieve next command
            var command = provider.CommandController.GetCommand(nextCommand);
            DebugManager.LogInformation($"{command}");

            // Execute next command
            foreach (var processor in this.commandProcessors)
            {
                if (processor.CanExecute(command))
                {
                    result = processor.Execute(command);
                    break;
                }
            }          

            // Process the result
            if (result == ActionState.Succeeded)
            {
                logger.LogInformation($"Task successful");
                LastCommandId = command.Id;
            }
            else if (result == ActionState.Failed)
            {
                logger.LogInformation($"Task failed");
                LastCommandId = command.Id;
            }
            else if (result == ActionState.InProgress)
            {
                logger.LogInformation("Task started and in progress");
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
            productionPanel.GetComponent<CityProduction>()
                            .Initialize(this, city);
            productionPanel.SetActive(true);
        }

        internal Camera GetMainCamera()
        {
            return this.mainCamera;
        }

        internal void HideSelectedBox()
        {
            selectedArmyBox.HideSelectedBox();
        }

        internal void SetSelectedBoxPosition(Vector3 worldVector, bool isActive)
        {
            this.selectedArmyBox.SetActive(isActive);
            this.selectedArmyBox.transform.position = worldVector;
        }

        internal Vector2Int GetSelectedBoxGamePosition()
        {
            return worldTilemap.ConvertUnityToGameVector(this.selectedArmyBox.transform.position);
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
                throw new InvalidOperationException("Selected army box was null.");
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
                armyPicker.Initialize(this, armiesToPick);
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
                    mainCamera = camera;
                }
            }

            if (mainCamera == null)
            {
                throw new InvalidOperationException("Could not find the MainCamera.");
            }
        }

        internal void SetCameraTarget(Transform transform)
        {     
            cameraFollow.target = transform;
        }        
     }
}
