using Assets.Scripts.CommandProcessors;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
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

        // Game managers
        [SerializeField]
        private WorldTilemap worldTilemap;
        private InputManager inputManager;
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
        private ProductionMode productionMode;

        public List<Army> CurrentAttackers { get; set; }
        public List<Army> CurrentDefenders { get; set; }        

        public GameManager GameManager { get => gameManager; set => gameManager = value; }
        public WorldTilemap WorldTilemap { get => worldTilemap; set => worldTilemap = value; }
        public WarPanel WarPanel { get => warPanel; set => warPanel = value; }
        public ProductionMode ProductionMode { get => productionMode; set => productionMode = value; }
        public InputManager InputManager { get => inputManager; set => inputManager = value; }
        public ItemPicker ItemPicker { get => itemPicker; set => itemPicker = value; }
        public SaveLoadPicker SaveLoadPicker { get => saveLoadPicker; set => saveLoadPicker = value; }
        public int LastCommandId { get => lastCommandId; set => lastCommandId = value; }

        public void Start()
        {
            Initialize();
        }

        internal bool IsInitalized()
        {
            bool result = true;

            if (!Game.IsInitialized() ||
                !this.isInitialized)
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

        public void Initialize()
        {            
            // Initialize WISM API
            this.GameManager = GetComponent<GameManager>();
            this.GameManager.Initialize();
            this.logger = this.GameManager.LoggerFactory.CreateLogger();
            this.provider = this.GameManager.ControllerProvider;

            // Create command processors
            this.commandProcessors = new List<ICommandProcessor>()
            {
                new SelectArmyProcessor(GameManager.LoggerFactory, this),
                new PrepareForBattleProcessor(GameManager.LoggerFactory, this),
                new BattleProcessor(GameManager.LoggerFactory, this),
                new CompleteBattleProcessor(GameManager.LoggerFactory, this),
                new StartTurnProcessor(GameManager.LoggerFactory, this),
                new EndTurnProcessor(GameManager.LoggerFactory, this),
                new SearchTempleProcessor(GameManager.LoggerFactory, this),
                new SearchRuinsProcessor(GameManager.LoggerFactory, this),
                new SearchLibraryProcessor(GameManager.LoggerFactory, this),               
                new SearchSageProcessor(GameManager.LoggerFactory, this),                
                new StandardProcessor(GameManager.LoggerFactory)
            };

            // Set up game UI            
            SetTime(GameManager.StandardTime);
            SetupCameras();

            this.armyManager = GetComponent<ArmyManager>();
            this.inputManager = GetComponent<InputManager>();

            WarPanel = this.warPanelPrefab.GetComponent<WarPanel>();
            armyPicker = this.armyPickerPrefab.GetComponent<ArmyPicker>();
            ItemPicker = this.itemPickerPrefab.GetComponent<ItemPicker>();
            SaveLoadPicker = this.saveLoadPickerPrefab.GetComponent<SaveLoadPicker>();
            productionPanel = UnityUtilities.GameObjectHardFind("CityProductionPanel");

            // Set up default game (for testing purposes only)
            GetComponent<GameFactory>().CreateDefaultGame();

            var startingTile = Game.Current.GetCurrentPlayer().Capitol.Tile;
            Vector3 worldVector = WorldTilemap.ConvertGameToUnityVector(startingTile.X, startingTile.Y);
            this.selectedArmyBox = Instantiate<GameObject>(SelectedBoxPrefab, worldVector, Quaternion.identity, WorldTilemap.transform).GetComponent<SelectedArmyBox>();

            GameManager.StartTurn(Game.Current.GetCurrentPlayer());

            this.isInitialized = true;
        }

        internal void GoToCapitol()
        {
            Player player = Game.Current.GetCurrentPlayer();
            var inputHandler = this.GetComponent<InputManager>().InputHandler;
            inputHandler.CenterOnTile(player.Capitol.Tile);
        }

        internal void GoToLocation()
        {
            this.inputManager.SetInputMode(InputMode.LocationPicker);
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
                this.armyManager.CleanupArmies();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
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
            logger.LogInformation($"Task executing: {command.Id}: {command.GetType()}");
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

        internal void SetCameraToSelectedBox()
        {
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
