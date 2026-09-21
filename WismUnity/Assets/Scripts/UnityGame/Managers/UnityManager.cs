using Assets.Scripts.CommandProcessors;
using Assets.Scripts.Telemetry;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.UI;
using Assets.Scripts.UnityGame.Persistance.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Wism.Client.AI.CommandProviders;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.Api.Telemetry;
using Wism.Client.CommandProcessors; // Updated
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Players;
using Wism.Client.Controllers; // Updated
using Wism.Client.Core;
using Wism.Client.Core.Telemetry;
using Wism.Client.MapObjects;
using Wism.Client.Pathing;
using Wism.Companion.Shared.Events;
using IWismLogger = Wism.Client.Common.IWismLogger;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Unity game is the primary GameObject and bridge to the Unity UI and WISM API via GameManager
    /// </summary>
    public class UnityManager : MonoBehaviour
    {
        private int lastCommandId = 0;
        private IWismLogger logger;
        private ControllerProvider provider;
        private List<ICommandProcessor> commandProcessors;

        // Bootstrapping
        private static UnityNewGameEntity gameSettings;
        public bool AwaitingInitialLoad { get; private set; }

        // Telemetry
        private IMapSnapshotBroadcaster snapshotBroadcaster;
        private UnitySocketTelemetryPublisher socketTelemetryPublisher;
        private float nextSnapshotTime = 0f;
        private const float snapshotInterval = 0.5f; // in seconds

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
        private Camera minimapCamera;
        private RenderTexture ownedMinimapTexture;
        private RectTransform minimapPanel;
        private Vector2Int minimapViewport;
        private Vector2 minimapParentSize;
        private CameraFollow cameraFollow;

        private bool isInitialized;
        private bool showDebugError = false;
        private ExecutionMode executionMode;
        private ProductionMode productionMode;
        private bool interactiveUI = true;
        private bool runAiBeforeInitialHumanTurn;
        private bool repaintCityTilesAfterNewGame;

        // AI
        private AdaptaCommandProvider adaptaProvider;
        private float nextAiGenerationTime;
        private const float AiGenerationRetryDelaySeconds = 0.05f;
        private const double SlowAiGenerationWarningMs = 250d;

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
        public bool ShowAiCombat { get; set; } = true;
        public WismGameMenu GameMenu { get; private set; }

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

            this.runAiBeforeInitialHumanTurn = gameSettings.IsNewGame && gameSettings.InteractiveUI;
            this.repaintCityTilesAfterNewGame = gameSettings.IsNewGame;
            this.AwaitingInitialLoad = !gameSettings.IsNewGame;

            IntializeWismApi();            
            InitializeCommandProcessors();
            InitializeUI();
            InitializeWismGame(gameSettings);
            InitializeAI();
            InitializeSnapshotBroadcaster();
            
            this.DebugManager.LogInformation("Initialization complete");

            this.isInitialized = true;
            this.ExecutionMode = ExecutionMode.Bootstrap;
            if (this.AwaitingInitialLoad) StartCoroutine(OpenInitialLoadPicker(gameSettings.LoadFilename));
        }

        private System.Collections.IEnumerator OpenInitialLoadPicker(string filename)
        {
            yield return null;
            if (string.IsNullOrWhiteSpace(filename)) this.InputManager.HandleSaveLoadPicker(false);
            else this.GameManager.LoadGame(filename);
        }

        public void CancelInitialLoad()
        {
            if (!this.AwaitingInitialLoad) return;
            this.ExecutionMode = ExecutionMode.NotStarted;
            SetNewGameSettings(null);
            Game.Unload();
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameSetup");
        }

        private void InitializeAI()
        {
            //var aiPlayer = Game.Current.Players.FirstOrDefault(p => !p.IsHuman);
            //if (aiPlayer == null)
            //{
            //    // No AI player found, skipping AI initialization
            //    return;
            //}

            this.adaptaProvider = WarlordsClassicAiFactory.CreateCommandProvider(this.provider, this.logger);
            this.DebugManager.LogInformation($"ADAPTA AI initialized.");
        }

        private void InitializeSnapshotBroadcaster()
        {
            var builder = new MapSnapshotBuilder();
            var telemetryContext = CreateTelemetryContext();
            this.socketTelemetryPublisher?.Dispose();
            this.socketTelemetryPublisher = new UnitySocketTelemetryPublisher(
                this.GameManager.LoggerFactory,
                telemetryContext,
                new NamedPipeTelemetryPublisher(this.GameManager.LoggerFactory, telemetryContext));
            var emitter = new MapSnapshotEmitter(
                this.GameManager.LoggerFactory,
                telemetryContext,
                this.socketTelemetryPublisher);
            this.snapshotBroadcaster = new UnityMapSnapshotBroadcaster(builder, emitter);
        }

        private void OnDestroy()
        {
            this.socketTelemetryPublisher?.Dispose();
            if (this.ownedMinimapTexture != null)
            {
                this.ownedMinimapTexture.Release();
                Destroy(this.ownedMinimapTexture);
            }
        }

        private static TelemetryContext CreateTelemetryContext()
        {
            var instanceId = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
            var sourceName = string.IsNullOrWhiteSpace(Application.productName)
                ? "WismUnity"
                : Application.productName;

            return new TelemetryContext
            {
                ChannelId = $"unity:{sourceName}:{instanceId}",
                SessionId = $"unity:{Guid.NewGuid():N}",
                SourceKind = "Unity",
                SourceName = sourceName,
                InstanceId = instanceId,
                StartedAtUtc = DateTime.UtcNow
            };
        }

        private void InitializeWismGame(UnityNewGameEntity gameSettings)
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
            this.GameMenu = GetComponent<WismGameMenu>() ?? gameObject.AddComponent<WismGameMenu>();
            this.GameMenu.Initialize(this);
            this.DebugManager.LogInformation("Initialized UI");
        }

        private void InitializeCommandProcessors()
        {
            this.commandProcessors = new List<ICommandProcessor>()
            {
                // General processors
                new SelectArmyProcessor(this.GameManager.LoggerFactory, this),
                new ItemTransferProcessor(this),
                
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
                new RazeTowerProcessor(this.GameManager.LoggerFactory, this),

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
            this.GameMenu?.ResetForGame();
            this.CampaignResultText = null;
            this.presentedCompletedGame = null;
            this.nextAiGenerationTime = 0f;
            this.InputManager.CompleteEndTurn();
            this.InputManager.SetInputMode(InputMode.Game);
            this.armyManager.Reset();
            GetComponent<CityManager>().Reset();
            GetComponent<ItemManager>().Reset();
            ConfigureWorldCameras();
            if (this.AwaitingInitialLoad)
            {
                this.AwaitingInitialLoad = false;
                InitializeSelectedArmyBox();
                this.ExecutionMode = ExecutionMode.Running;
            }
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
            if (!this.InteractiveUI)
            {
                return;
            }

            var inputManager = GetComponent<InputManager>();
            var inputHandler = inputManager != null ? inputManager.InputHandler : null;
            if (inputHandler == null || player == null || player.Capitol == null || player.Capitol.Tile == null)
            {
                Debug.LogWarning("Cannot center camera on capitol because the interactive camera target is unavailable.");
                return;
            }

            inputHandler.CenterOnTile(player.Capitol.Tile);
        }

        public void GoToLocation()
        {
            this.inputManager.SetInputMode(InputMode.LocationPicker);
        }

        private void LateUpdate()
        {
            var viewport = new Vector2Int(Screen.width, Screen.height);
            if (this.minimapPanel != null && this.minimapPanel.parent is RectTransform parent &&
                (viewport != this.minimapViewport || parent.rect.size != this.minimapParentSize))
            {
                DockMinimapToViewport(this.minimapPanel);
                this.minimapViewport = viewport;
                this.minimapParentSize = parent.rect.size;
            }

            if (this.ExecutionMode != ExecutionMode.Running ||
                this.inputManager == null || this.inputManager.InputMode != InputMode.Game ||
                !Game.IsInitialized() || Game.Current.GetCurrentPlayer()?.IsHuman != true)
                return;

            // Complete only front-of-queue selection work before rendering.
            // Two commands allow a deselect/select pair, never skipping a game action.
            bool changed = false;
            for (int i = 0; i < 2; i++)
            {
                if (Game.Current.GameState != GameState.Ready && Game.Current.GameState != GameState.SelectedArmy)
                    break;
                int next = this.LastCommandId + 1;
                if (!this.provider.CommandController.CommandExists(next)) break;
                var command = this.provider.CommandController.GetCommand(next);
                if (!(command is SelectArmyCommand || command is DeselectArmyCommand || command is SelectNextArmyCommand))
                    break;
                DoTasks();
                if (this.LastCommandId < next) break;
                changed = true;
            }
            if (changed) Draw();
        }

        private Game presentedCompletedGame;
        public string CampaignResultText { get; private set; }

        private void PresentCompletedCampaign()
        {
            if (ReferenceEquals(this.presentedCompletedGame, Game.Current))
                return;

            this.presentedCompletedGame = Game.Current;
            this.InputManager.CompleteEndTurn();
            this.InputManager.SetInputMode(InputMode.Game);
            var winner = Game.Current.VictoryOutcome?.WinnerClanDisplayName;
            this.CampaignResultText = string.IsNullOrEmpty(winner)
                ? "The war is over. You may inspect the realm."
                : $"{winner} have won the war. You may inspect the realm.";
            LogInformation(this.CampaignResultText);
            var notification = GameObject.FindGameObjectWithTag("NotificationBox")?.GetComponent<NotificationBox>();
            if (notification != null)
                notification.Notify(this.CampaignResultText, double.PositiveInfinity);
        }

        public void FixedUpdate()
        {
            if (this.GameMenu != null && this.GameMenu.IsOpen) return;
            // The loader's placeholder world must never start a turn or run AI.
            // Only the queued load command may replace it with playable state.
            if (this.AwaitingInitialLoad)
            {
                if (this.isInitialized) DoTasks();
                return;
            }
            try
            {
                switch (this.ExecutionMode)
                {
                    // Bootstrap game
                    case ExecutionMode.Bootstrap:
                        DoTasks();
                        ConfigureWorldCameras();
                        RepaintCityTilesAfterNewGameIfReady();
                        MoveInitialTurnToAiBeforeHumanHandoff();
                        this.ExecutionMode = ExecutionMode.Starting;
                        break;

                    case ExecutionMode.Starting:
                        InitializeSelectedArmyBox();

                        // Start first turn
                        this.GameManager.StartTurn(Game.Current.GetCurrentPlayer());
                        this.ExecutionMode = ExecutionMode.Running;
                        break;

                    // Standard game loop
                    case ExecutionMode.Running:
                        Draw();
                        if (Game.Current.GameState == GameState.GameOver)
                        {
                            PresentCompletedCampaign();
                            // Discard stale gameplay, but still service a requested load.
                            DoTasks();
                            break;
                        }
                        GenerateAICommands();
                        DoTasks();
                        if (Game.Current.GameState != GameState.GameOver)
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

        private void GenerateAICommands()
        {
            if (Game.Current.GameState == GameState.GameOver)
                return;
            var currentPlayer = Game.Current.GetCurrentPlayer();
            if (!currentPlayer.IsHuman)
            {
                if (this.InputManager.InputMode != InputMode.AITurn)
                {
                    this.InputManager.SetInputMode(InputMode.AITurn);
                }

                if (!this.provider.CommandController.CommandExists(this.LastCommandId + 1))
                {
                    if (Time.unscaledTime < this.nextAiGenerationTime)
                    {
                        return;
                    }

                    var start = System.Diagnostics.Stopwatch.StartNew();
                    this.adaptaProvider.GenerateCommands();
                    start.Stop();

                    if (start.Elapsed.TotalMilliseconds >= SlowAiGenerationWarningMs)
                    {
                        LogInformation(
                            "AI command generation for {0} took {1:0}ms.",
                            currentPlayer.Clan.ShortName,
                            start.Elapsed.TotalMilliseconds);
                    }

                    if (!this.provider.CommandController.CommandExists(this.LastCommandId + 1))
                    {
                        this.nextAiGenerationTime = Time.unscaledTime + AiGenerationRetryDelaySeconds;
                    }
                    else
                    {
                        this.nextAiGenerationTime = 0f;
                    }
                }
            }
        }

        private void InitializeSelectedArmyBox()
        {
            var player = Game.Current.GetCurrentPlayer();
            // A valid saved realm can have armies but no remaining capital.
            var startingTile = player.Capitol?.Tile ?? player.GetArmies().FirstOrDefault()?.Tile ?? World.Current.Map[0, 0];
            var worldVector = this.WorldTilemap.ConvertGameToUnityVector(startingTile.X, startingTile.Y);
            this.selectedArmyBox = Instantiate<GameObject>(this.SelectedBoxPrefab, worldVector, Quaternion.identity, this.WorldTilemap.transform).GetComponent<SelectedArmyBox>();
        }

        private void MoveInitialTurnToAiBeforeHumanHandoff()
        {
            if (!this.runAiBeforeInitialHumanTurn)
            {
                return;
            }

            this.runAiBeforeInitialHumanTurn = false;
            if (!Game.IsInitialized() ||
                Game.Current.Players == null ||
                Game.Current.Players.Count < 2)
            {
                return;
            }

            var firstHuman = Game.Current.GetCurrentPlayer();
            if (firstHuman == null || !firstHuman.IsHuman)
            {
                return;
            }

            var firstHumanIndex = Game.Current.Players.IndexOf(firstHuman);
            var firstAiIndex = FindNextAiPlayerIndex(firstHumanIndex);
            if (firstAiIndex < 0)
            {
                return;
            }

            SetCurrentPlayerIndex(firstAiIndex);
            this.InputManager.SetInputMode(InputMode.AITurn);
            LogInformation(
                "Initial turn order: running AI turns before handing off to {0}.",
                firstHuman.Clan.DisplayName);
        }

        private void RepaintCityTilesAfterNewGameIfReady()
        {
            if (!this.repaintCityTilesAfterNewGame || !Game.IsInitialized())
            {
                return;
            }

            GetComponent<CityManager>().Reset();
            this.repaintCityTilesAfterNewGame = false;
        }

        private static int FindNextAiPlayerIndex(int startIndex)
        {
            var players = Game.Current.Players;
            for (var offset = 1; offset < players.Count; offset++)
            {
                var index = (startIndex + offset) % players.Count;
                var player = players[index];
                if (player != null && !player.IsHuman && !player.IsDead)
                {
                    return index;
                }
            }

            return -1;
        }

        private static void SetCurrentPlayerIndex(int index)
        {
            var property = typeof(Game).GetProperty(
                "CurrentPlayerIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
            {
                throw new InvalidOperationException("Could not update initial turn order.");
            }

            property.SetValue(Game.Current, index);
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
            if (!command.CanExecuteInCurrentState)
            {
                // Do not invoke a processor: it can have side effects before Execute.
                this.LastCommandId = command.Id;
                return;
            }
            this.DebugManager.LogInformation($"{command}");
            //LogPlayerKind(command);

            // Execute next command
            foreach (var processor in this.commandProcessors)
            {
                if (processor.CanExecute(command))
                {
                    var startTime = DateTime.Now;
                    result = processor.Execute(command);
                    var stopTime = DateTime.Now;
                    this.logger.LogInformation("Command duration: " + (stopTime - startTime).TotalMilliseconds + "ms");

                    // Emit snapshot if the command is a game state change
                    EmitSnapshot();
                    break;
                }
            }

            // Process the result
            switch (result)
            {
                case ActionState.Succeeded:
                    this.logger.LogInformation("Task successful");
                    AdvanceCommand(command);
                    break;

                case ActionState.Failed:
                    this.logger.LogInformation("Task failed");
                    AdvanceCommand(command);
                    break;

                case ActionState.InProgress:
                    this.logger.LogInformation("Task started and in progress");
                    // Do not advance command ID
                    break;
            }
        }

        /// <summary>
        /// Advances the game state based on the specified command.
        /// </summary>
        /// <remarks>If the command is an <see cref="EndTurnCommand"/>, the method transitions the game to
        /// the next player's turn. If the next player is human and the current input mode is set to AI turn, the input
        /// mode is switched to game mode.</remarks>
        /// <param name="command">The command to process, which determines the next game action.</param>
        void AdvanceCommand(Command command)
        {
            this.LastCommandId = command.Id;

            if (command is EndTurnCommand)
            {
                this.InputManager.CompleteEndTurn();
                QueueStartTurnAfterEndTurnIfNeeded();

                var nextPlayer = Game.Current.GetCurrentPlayer();
                if (nextPlayer.IsHuman && this.InputManager.GetInputMode() == InputMode.AITurn)
                {
                    this.InputManager.SetInputMode(InputMode.Game);
                }
            }
        }

        private void QueueStartTurnAfterEndTurnIfNeeded()
        {
            if (Game.Current.GameState != GameState.StartingTurn ||
                this.provider.CommandController.CommandExists(this.LastCommandId + 1))
            {
                return;
            }

            this.GameManager.StartTurn(Game.Current.GetCurrentPlayer());
        }

        private void LogPlayerKind(Command command)
        {
            var isHuman = Game.Current.GetCurrentPlayer().IsHuman;
            var playerKind = (isHuman) ? "Human" : "AI";
            this.logger.LogInformation($"{playerKind} task executing: {command.Id}: {command.GetType()}");
        }

        private void EmitSnapshot()
        {
            if (Time.time >= nextSnapshotTime)
            {
                snapshotBroadcaster.TryEmitSnapshot();
                nextSnapshotTime = Time.time + snapshotInterval;
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

        internal void ShowProductionManagementPanel(Player player)
        {
            this.productionPanel.GetComponent<CityProduction>()
                            .InitializeManagement(this, player);
            this.productionPanel.SetActive(true);
        }

        internal void ShowProductionManagementPanelForCurrentPlayer()
        {
            var player = Game.Current?.GetCurrentPlayer();
            if (player == null || player.GetCities().Count == 0)
            {
                NotifyUser("No owned cities to manage.");
                return;
            }

            SetProductionMode(ProductionMode.CitySelected);
            ShowProductionManagementPanel(player);
        }

        internal void SelectProductionDestination(City city)
        {
            this.productionPanel.GetComponent<CityProduction>()
                            .SelectDestination(city);
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
            if (!this.InteractiveUI)
            {
                return;
            }

            if (this.inputManager != null && this.inputManager.InputHandler != null)
            {
                this.inputManager.InputHandler.SetCurrentTile(null);
            }

            var messageBoxObject = GameObject.FindGameObjectWithTag("NotificationBox");
            var messageBox = messageBoxObject != null
                ? messageBoxObject.GetComponent<NotificationBox>()
                : null;
            if (messageBox != null)
            {
                messageBox.Notify("");
            }
        }

        public void NotifyUser(string message, params object[] args)
        {
            var formatted = String.Format(message, args);
            if (!this.InteractiveUI)
            {
                LogInformation(formatted);
                return;
            }

            var messageBoxObject = GameObject.FindGameObjectWithTag("NotificationBox");
            var messageBox = messageBoxObject != null
                ? messageBoxObject.GetComponent<NotificationBox>()
                : null;
            if (messageBox == null)
            {
                Debug.LogWarning("Cannot notify user because the notification box is unavailable.");
                return;
            }

            messageBox.Notify(formatted);
        }

        internal bool ShouldPresentFor(Player player)
        {
            return this.InteractiveUI && (player?.IsHuman ?? false);
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
                else if (camera.CompareTag("MinimapCamera") || camera.name == "MinimapCamera")
                {
                    this.minimapCamera = camera;
                }
            }

            if (this.mainCamera == null)
            {
                throw new InvalidOperationException("Could not find the MainCamera.");
            }
        }

        private void ConfigureWorldCameras()
        {
            if (!Game.IsInitialized() || World.Current?.Map == null)
            {
                return;
            }

            var mapWidth = World.Current.Map.GetLength(0);
            var mapHeight = World.Current.Map.GetLength(1);
            if (mapWidth <= 0 || mapHeight <= 0)
            {
                return;
            }

            this.cameraFollow?.ConfigureBoundsFromCurrentWorld();

            if (this.minimapCamera == null)
            {
                var minimapObject = GameObject.FindGameObjectWithTag("MinimapCamera");
                if (minimapObject != null)
                {
                    this.minimapCamera = minimapObject.GetComponent<Camera>();
                }
            }

            if (this.minimapCamera == null)
            {
                return;
            }

            this.minimapCamera.orthographic = true;
            ConfigureMinimapGeometry(mapWidth, mapHeight);
            var aspect = this.minimapCamera.targetTexture != null && this.minimapCamera.targetTexture.height > 0
                ? this.minimapCamera.targetTexture.width / (float)this.minimapCamera.targetTexture.height
                : this.minimapCamera.aspect;
            aspect = Mathf.Max(0.01f, aspect);

            this.minimapCamera.orthographicSize = Mathf.Max(mapHeight / 2f, (mapWidth / 2f) / aspect);
            this.minimapCamera.transform.position = new Vector3(
                mapWidth / 2f,
                mapHeight / 2f,
                this.minimapCamera.transform.position.z == 0f ? -10f : this.minimapCamera.transform.position.z);
            ConfigureMinimapCityOverlay();
        }

        private void ConfigureMinimapCityOverlay()
        {
            var map = GameObject.Find("Minimap")?.GetComponent<RectTransform>();
            if (map == null) return;
            var overlay = map.GetComponentInChildren<MinimapCityOverlay>(true);
            if (overlay == null)
            {
                var layer = new GameObject("CityMarkers", typeof(RectTransform), typeof(MinimapCityOverlay));
                layer.transform.SetParent(map, false);
                overlay = layer.GetComponent<MinimapCityOverlay>();
                var rect = overlay.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            overlay.transform.SetAsFirstSibling();
            overlay.Bind(World.Current, this.minimapCamera, this.WorldTilemap);
        }

        private void ConfigureMinimapGeometry(int mapWidth, int mapHeight)
        {
            var map = GameObject.Find("Minimap")?.GetComponent<RectTransform>();
            var panel = GameObject.Find("MinimapPanel")?.GetComponent<RectTransform>();
            if (map == null || panel == null) return;

            const float frameBorder = 0.58f;
            float height = map.rect.height > 0f ? map.rect.height : 10.44f;
            float width = height * mapWidth / mapHeight;
            map.anchorMin = map.anchorMax = map.pivot = new Vector2(.5f, .5f);
            map.sizeDelta = new Vector2(width, height);
            map.anchoredPosition = Vector2.zero;
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width + 2f * frameBorder);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height + 2f * frameBorder);
            var collider = panel.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.size = map.rect.size;
                collider.offset = Vector2.zero;
            }

            // Fit both the UI and camera target. Resizing only the frame would
            // stretch the map; mutating the shared asset would affect other worlds.
            int pixelsPerTile = Mathf.Clamp(1024 / Mathf.Max(mapWidth, mapHeight), 1, 4);
            int textureWidth = mapWidth * pixelsPerTile;
            int textureHeight = mapHeight * pixelsPerTile;
            if (this.ownedMinimapTexture == null || this.ownedMinimapTexture.width != textureWidth ||
                this.ownedMinimapTexture.height != textureHeight)
            {
                if (this.ownedMinimapTexture != null)
                {
                    this.ownedMinimapTexture.Release();
                    Destroy(this.ownedMinimapTexture);
                }
                this.ownedMinimapTexture = new RenderTexture(textureWidth, textureHeight, 24)
                {
                    name = "WorldMinimap", filterMode = FilterMode.Point
                };
                this.ownedMinimapTexture.Create();
            }
            this.minimapCamera.targetTexture = this.ownedMinimapTexture;
            var image = map.GetComponent<UnityEngine.UI.RawImage>();
            if (image != null) image.texture = this.ownedMinimapTexture;
            this.minimapPanel = panel;
            DockMinimapToViewport(panel);
            this.minimapViewport = new Vector2Int(Screen.width, Screen.height);
        }

        private static void DockMinimapToViewport(RectTransform panel)
        {
            Canvas.ForceUpdateCanvases();
            if (!(panel.parent is RectTransform parent) || Screen.width <= 0 || Screen.height <= 0) return;
            // Anchors follow the camera-backed canvas even when its dimensions
            // settle after world startup. Keep the existing pivot/collider origin.
            panel.anchorMin = panel.anchorMax = Vector2.one;
            var margin = new Vector2(4f * parent.rect.width / Screen.width, 4f * parent.rect.height / Screen.height);
            panel.anchoredPosition = -Vector2.Scale(panel.rect.size, Vector2.one - panel.pivot) - margin;
        }

        internal void SetCameraTarget(Transform transform)
        {
            this.cameraFollow.target = transform;
        }
    }
}
