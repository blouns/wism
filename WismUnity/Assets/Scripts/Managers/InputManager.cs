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

        public void Awake()
        {
            this.UnityManager = GameObject.FindGameObjectWithTag("UnityManager")
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
            else if (Input.GetKeyDown(KeyCode.E))
            {
                GameManager.EndTurn();
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                UnityManager.SetProductionMode(ProductionMode.SelectCity);
            }
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
