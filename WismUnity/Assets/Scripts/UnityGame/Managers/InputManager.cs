using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.Managers
{
    public enum InputMode
    {
        Game,
        ItemDropPicker,
        ItemTakePicker,
        LocationPicker,
        LoadGamePicker,
        SaveGamePicker,
        UI,
        WaitForKey
    }

    public class InputManager : MonoBehaviour
    {
        private UnityManager unityManager;
        private GameManager gameManager;
        private InputHandler inputHandler;

        private readonly Timer mouseSingleLeftClickTimer = new Timer();
        private bool singleLeftClickProcessed;
        private readonly Timer mouseRightClickHoldTimer = new Timer();
        private bool holdingRightButton;
        private InputMode inputMode = InputMode.Game;
        private bool skipInput;

        public GameManager GameManager { get => this.gameManager; set => this.gameManager = value; }
        public UnityManager UnityManager { get => this.unityManager; set => this.unityManager = value; }
        public InputHandler InputHandler { get => this.inputHandler; set => this.inputHandler = value; }
        public InputMode InputMode { get => this.inputMode; private set => this.inputMode = value; }

        public delegate void AnyKeyPressed();

        public AnyKeyPressed KeyPressed;

        public void Start()
        {
            this.UnityManager = UnityUtilities.GameObjectHardFind("UnityManager")
                .GetComponent<UnityManager>();
            this.GameManager = this.UnityManager.GetComponent<GameManager>();
            this.InputHandler = new InputHandler(this.UnityManager);

            // Mouse click timing
            this.mouseSingleLeftClickTimer.Interval = 400;
            this.mouseSingleLeftClickTimer.Elapsed += SingleLeftClick;
            this.mouseRightClickHoldTimer.Interval = 200;
            this.mouseRightClickHoldTimer.Elapsed += SingleRightClick;
        }

        public void Update()
        {
            if (!this.UnityManager.IsInitalized())
            {
                return;
            }

            HandleInput();
        }

        private void SingleLeftClick(object o, System.EventArgs e)
        {
            this.mouseSingleLeftClickTimer.Stop();

            this.singleLeftClickProcessed = true;
        }

        private void SingleRightClick(object o, EventArgs e)
        {
            this.holdingRightButton = true;
        }

        public void SetInputMode(InputMode mode)
        {
            this.InputMode = mode;
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
            switch (this.InputMode)
            {
                case InputMode.Game:
                    HandleGameInput();
                    break;
                case InputMode.LocationPicker:
                    HandleLocationPicker();
                    break;
                case InputMode.ItemDropPicker:
                    HandleItemPicker(false);
                    break;
                case InputMode.ItemTakePicker:
                    HandleItemPicker(true);
                    break;
                case InputMode.SaveGamePicker:
                    HandleSaveLoadPicker(true);
                    break;
                case InputMode.LoadGamePicker:
                    HandleSaveLoadPicker(false);
                    break;
                case InputMode.WaitForKey:
                    HandleWaitForKey();
                    break;
                case InputMode.UI:
                // Handled by Event System
                default:
                    break;
            }
        }

        private void HandleWaitForKey()
        {
            if (this.KeyPressed != null &&
                Input.anyKeyDown)
            {
                this.KeyPressed();
            }
        }

        private void HandleGameInput()
        {
            if (this.skipInput ||
                this.unityManager.ExecutionMode != ExecutionMode.Running)
            {
                this.skipInput = false;
                return;
            }

            if (this.singleLeftClickProcessed)
            {
                // Single left click performed
                HandleLeftClick();
                this.singleLeftClickProcessed = false;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                // Detect single vs. double-click
                if (this.mouseSingleLeftClickTimer.Enabled == false)
                {
                    this.mouseSingleLeftClickTimer.Start();
                    // Wait for double click
                    return;
                }
                else
                {
                    // Double click performed, so cancel single click
                    this.mouseSingleLeftClickTimer.Stop();

                    HandleLeftClick(true);
                }
            }
            else
            {
                HandleKeyboard();
            }

            // Handle right-click (drag)
            if (Input.GetMouseButtonDown(1))
            {
                if (this.mouseRightClickHoldTimer.Enabled == false)
                {
                    this.mouseRightClickHoldTimer.Start();
                    // Wait for mouse up
                    return;
                }
            }
            else if (Input.GetMouseButtonUp(1))
            {
                this.mouseRightClickHoldTimer.Stop();

                if (!this.holdingRightButton)
                {
                    HandleRightClick();
                }

                this.holdingRightButton = false;
            }

            this.UnityManager.Draw();
        }

        private void HandleKeyboard()
        {
            // Army actions
            if (Input.GetKeyDown(KeyCode.M))
            {
                this.UnityManager.HandleArmyPicker();
            }
            else if (Input.GetKeyDown(KeyCode.Period) ||
                     Input.GetKeyDown(KeyCode.KeypadPeriod))
            {
                this.UnityManager.ToggleMinimap();
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                this.GameManager.SelectNextArmy();
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                this.GameManager.DefendSelectedArmies();
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                this.GameManager.QuitSelectedArmies();
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                this.GameManager.SearchLocation();
            }
            // TODO: Add Disband action for armies
            // TODO: Add Find armies action

            // Hero actions
            else if (Input.GetKeyDown(KeyCode.T))
            {
                this.UnityManager.HandleItemPicker(true);
            }
            else if (Input.GetKeyDown(KeyCode.O))
            {
                this.UnityManager.HandleItemPicker(false);
            }
            else if (Input.GetKey(KeyCode.C))
            {
                this.UnityManager.HandlePetCompanion();
            }
            // TODO: Add Inventory action
            // TODO: Add Find heros (k) action

            // City actions
            else if (Input.GetKeyDown(KeyCode.P))
            {
                this.UnityManager.SetProductionMode(ProductionMode.SelectCity);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                this.GameManager.RazeCity();
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                this.GameManager.Build();
            }

            // Game actions
            else if (Input.GetKeyDown(KeyCode.E))
            {
                this.GameManager.EndTurn();
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                this.UnityManager.HandleSaveLoadPicker(true);
            }
            else if (Input.GetKeyDown(KeyCode.L))
            {
                this.UnityManager.HandleSaveLoadPicker(false);
            }
            else if (Input.GetKeyDown(KeyCode.Slash))
            {
                this.UnityManager.ToggleHelp();
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                Application.Quit();
            }
            // TODO: Remove temp debug code
            else if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                // Toggle Djikstra pathing vs. A-Star pathing
                this.UnityManager.TogglePathing();

            }
            // TODO: Add resign action
            // TODO: Add change control action (human, AI)
            // TODO: Add reports actions (winning, cities, gold, etc.)

            // Navigation actions
            else if (Input.GetKeyDown(KeyCode.C))
            {
                this.UnityManager.GoToCapitol(Game.Current.GetCurrentPlayer());
            }
            // TODO: Add center-on-selected (space?) action

            // TODO: Remove these Debug-only actions
            else if (Input.GetKeyDown(KeyCode.Comma))
            {
                this.UnityManager.GoToLocation();
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                this.UnityManager.DebugManager.ToggleDebug();
            }
        }

        private void HandleRightClick()
        {
            // Cancel object selection
            if (Game.Current.GameState == GameState.SelectedArmy)
            {
                this.InputHandler.DeselectObject();
            }

            // Cancel city production selection
            if (this.UnityManager.ProductionMode == ProductionMode.SelectCity)
            {
                this.UnityManager.ProductionMode = ProductionMode.None;
            }
        }

        private void HandleLeftClick(bool isDoubleClick = false)
        {
            var camera = this.unityManager.GetMainCamera();
            Tile clickedTile = this.unityManager.WorldTilemap
                .GetClickedTile(camera);
            this.InputHandler.HandleArmyClick(isDoubleClick, clickedTile);
            this.InputHandler.HandleCityClick(clickedTile);
        }

        public InputMode GetInputMode()
        {
            return this.InputMode;
        }

        public void HandleSaveLoadPicker(bool isSaving)
        {
            var saveLoadPicker = this.unityManager.SaveLoadPicker;
            if (isSaving)
            {
                // Launch the SaveLoad picker
                if (saveLoadPicker.OkCancelResult == OkCancel.None)
                {
                    this.unityManager.NotifyUser("Saving the game...");
                    saveLoadPicker.Initialize(this.unityManager, true);
                    SetInputMode(InputMode.SaveGamePicker);
                }
                // Cancelled
                else if (saveLoadPicker.OkCancelResult == OkCancel.Cancel)
                {
                    saveLoadPicker.Clear();
                    SetInputMode(InputMode.Game);
                }
                // Save the game
                else if (saveLoadPicker.OkCancelResult == OkCancel.Ok)
                {
                    var fileName = String.Format(saveLoadPicker.DefaultFilenameFormat, saveLoadPicker.SelectedIndex + 1);
                    var saveName = saveLoadPicker.GetCurrentSaveName();
                    this.GameManager.SaveGame(fileName, saveName);
                    saveLoadPicker.Clear();
                    SetInputMode(InputMode.Game);
                }
            }
            else
            {
                // Launch the SaveLoad picker
                if (saveLoadPicker.OkCancelResult == OkCancel.None)
                {
                    this.unityManager.NotifyUser("Loading the game...");
                    saveLoadPicker.Initialize(this.unityManager, false);
                    SetInputMode(InputMode.LoadGamePicker);
                }
                // Cancelled
                else if (saveLoadPicker.OkCancelResult == OkCancel.Cancel)
                {
                    saveLoadPicker.Clear();
                    SetInputMode(InputMode.Game);
                }
                // Load the game
                else if (saveLoadPicker.OkCancelResult == OkCancel.Ok)
                {
                    var filename = saveLoadPicker.GetCurrentFilename();
                    this.GameManager.LoadGame(filename);
                    saveLoadPicker.Clear();
                    SetInputMode(InputMode.Game);
                }
            }
        }

        private void HandleItemPicker(bool takingItems)
        {
            if (!Game.Current.ArmiesSelected())
            {
                this.unityManager.NotifyUser("You must have a hero selected for that!");
                SetInputMode(InputMode.Game);
                return;
            }

            Army army = Game.Current.GetSelectedArmies()
                .Find(army => army is Hero);
            if (army == null)
            {
                this.unityManager.NotifyUser("You must have a hero selected for that!");
                SetInputMode(InputMode.Game);
                return;
            }

            Hero hero = (Hero)army;
            var itemPicker = this.unityManager.ItemPicker;
            List<MapObject> itemsToPick;
            if (takingItems)
            {
                // Launch the item picker
                if (itemPicker.OkCancelResult == OkCancel.None)
                {
                    this.unityManager.NotifyUser("Taking an item...");
                    itemsToPick = new List<MapObject>(hero.Tile.Items);
                    itemPicker.Initialize(this.unityManager, itemsToPick);
                }
                // Cancelled
                else if (itemPicker.OkCancelResult == OkCancel.Cancel)
                {
                    itemPicker.Clear();
                    SetInputMode(InputMode.Game);
                }
                // Take the items
                else if (itemPicker.OkCancelResult == OkCancel.Ok)
                {
                    var item = itemPicker.GetSelectedItem();
                    var items = new List<Artifact> { (Artifact)item };
                    this.GameManager.TakeItems(hero, items);
                    itemPicker.Clear();
                    SetInputMode(InputMode.Game);
                }
            }
            else
            {
                // Launch the item picker
                if (itemPicker.OkCancelResult == OkCancel.None)
                {
                    if (hero.Items == null || hero.Items.Count == 0)
                    {
                        this.unityManager.NotifyUser("No items to drop!");
                        return;
                    }
                    this.unityManager.NotifyUser("Dropping an item...");
                    itemsToPick = new List<MapObject>(hero.Items);
                    itemPicker.Initialize(this.unityManager, itemsToPick);
                }
                // Cancelled
                else if (itemPicker.OkCancelResult == OkCancel.Cancel)
                {
                    itemPicker.Clear();
                    SetInputMode(InputMode.Game);
                }
                // Drop the items
                else if (itemPicker.OkCancelResult == OkCancel.Ok)
                {
                    var item = itemPicker.GetSelectedItem();
                    var items = new List<Artifact> { (Artifact)item };
                    this.GameManager.DropItems(hero, items);
                    itemPicker.Clear();
                    SetInputMode(InputMode.Game);
                }
            }
        }

        private void HandleLocationPicker()
        {
            var itemPicker = this.unityManager.ItemPicker;
            List<MapObject> itemsToPick;

            // Launch the location picker
            if (itemPicker.OkCancelResult == OkCancel.None)
            {
                if (Game.Current.ArmiesSelected())
                {
                    this.gameManager.DeselectArmies();
                }
                this.unityManager.NotifyUser("Goto location...");
                itemsToPick = new List<MapObject>(World.Current.GetLocations());
                itemPicker.Initialize(this.unityManager, itemsToPick);
            }
            // Cancelled
            else if (itemPicker.OkCancelResult == OkCancel.Cancel)
            {
                itemPicker.Clear();
                SetInputMode(InputMode.Game);
            }
            // Center on the location chosen
            else if (itemPicker.OkCancelResult == OkCancel.Ok)
            {
                var item = itemPicker.GetSelectedItem();
                itemPicker.Clear();

                this.unityManager.NotifyUser("Going to " + item.DisplayName);
                this.InputHandler.CenterOnTile(((Location)item).Tile);
                SetInputMode(InputMode.Game);
            }
            // User is still selecting the item
            else
            {
                // Do nothing
            }

        }
    }
}
