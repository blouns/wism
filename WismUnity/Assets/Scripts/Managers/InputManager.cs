using Assets.Scripts.UI;
using System.Timers;
using UnityEngine;
using Wism.Client.Core;

namespace Assets.Scripts.Managers
{
    public class InputManager : MonoBehaviour
    {
        private UnityManager unityManager;
        private GameManager gameManager;
        private InputHandler inputHandler;

        private readonly Timer mouseSingleLeftClickTimer = new Timer();
        private bool singleLeftClickProcessed;
        private readonly Timer mouseRightClickHoldTimer = new Timer();
        private bool holdingRightButton;
        private bool acceptingInput = true;
        private bool skipInput;

        public GameManager GameManager { get => gameManager; set => gameManager = value; }
        public UnityManager UnityManager { get => unityManager; set => unityManager = value; }
        public InputHandler InputHandler { get => inputHandler; set => inputHandler = value; }

        public void Start()
        {
            this.UnityManager = UnityUtilities.GameObjectHardFind("UnityManager")
                .GetComponent<UnityManager>();
            this.GameManager = UnityManager.GetComponent<GameManager>();
            this.InputHandler = new InputHandler(this.UnityManager);

            // Mouse click timing
            mouseSingleLeftClickTimer.Interval = 400;
            mouseSingleLeftClickTimer.Elapsed += SingleLeftClick;
            mouseRightClickHoldTimer.Interval = 200;
            mouseRightClickHoldTimer.Elapsed += SingleRightClick;
        }

        public void Update()
        {
            if (!UnityManager.IsInitalized())
            {
                return;
            }

            HandleInput();
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
            if (UnityManager.SelectingArmies || !this.acceptingInput || this.skipInput)
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
                }
            }
            else
            {
                HandleKeyboard();
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
                }

                holdingRightButton = false;
            }

            UnityManager.Draw();
        }

        private void HandleKeyboard()
        {
            // Army actions
            if (Input.GetKeyDown(KeyCode.M))
            {
                UnityManager.HandleArmyPicker();
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                UnityManager.ToggleMinimap();
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                GameManager.SelectNextArmy();
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                GameManager.DefendSelectedArmies();
            }            
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                GameManager.SearchLocationWithSelectedArmies();
            }
            // TODO: Add Quit action for armies
            // TODO: Add Disband action for armies
            // TODO: Add Find armies action

            // Hero actions
            else if (Input.GetKeyDown(KeyCode.T))
            {
                UnityManager.HandleItemPicker(true);
            }
            else if (Input.GetKeyDown(KeyCode.O))
            {
                UnityManager.HandleItemPicker(false);
            }
            // TODO: Add Inventory action
            // TODO: Add Find heros (k) action

            // City actions
            else if (Input.GetKeyDown(KeyCode.P))
            {
                UnityManager.SetProductionMode(ProductionMode.SelectCity);
            }
            // TODO: Add build (b) actions
            // TODO: Add raze (r) action

            // Game actions
            else if (Input.GetKeyDown(KeyCode.E))
            {
                GameManager.EndTurn();
            }
            // TODO: Add save (or auto-save) and load game actions 
            // TODO: Add resign action
            // TODO: Add change control action (human, AI)
            // TODO: Add reports actions (winning, cities, gold, etc.)

            // Navigation actions
            else if (Input.GetKeyDown(KeyCode.C))
            {
                UnityManager.GoToCapitol();
            }
            // TODO: Add center-on-selected (space?) action
        }

        private void HandleRightClick()
        {
            // Cancel object selection
            if (Game.Current.GameState == GameState.SelectedArmy)
            {
                InputHandler.DeselectObject();
            }

            // Cancel city production selection
            if (UnityManager.ProductionMode == ProductionMode.SelectCity)
            {
                UnityManager.ProductionMode = ProductionMode.None;
            }
        }

        private void HandleLeftClick(bool isDoubleClick = false)
        {
            var camera = this.unityManager.GetMainCamera();
            Tile clickedTile = this.unityManager.WorldTilemap
                .GetClickedTile(camera);
            InputHandler.HandleArmyClick(isDoubleClick, clickedTile);
            InputHandler.HandleCityClick(clickedTile);
        }

        internal bool IsAcceptingInput()
        {
            return this.acceptingInput;
        }
    }
}
