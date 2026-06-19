using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using UnityEngine;
using UnityEngine.EventSystems;
using Wism.Client.Core;

namespace Assets.Scripts.UI
{
    public class MouseOverCursor : MonoBehaviour
    {
        private UnityManager unityManager;
        private WorldTilemap worldTilemap;
        private CursorManager cursorManager;
        private Camera mainCamera;

        public void Start()
        {
            this.unityManager = GameObject.FindGameObjectWithTag("UnityManager")
                .GetComponent<UnityManager>();

            this.cursorManager = this.unityManager.GetComponent<CursorManager>();

            this.worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();

            this.mainCamera = GameObject.FindGameObjectWithTag("MainCamera")
                .GetComponent<Camera>();
        }

        public void OnMouseOver()
        {
            ApplyMouseOverCursor();
        }

        public void Update()
        {
            if (this.transform.name != "WorldTilemap")
            {
                return;
            }

            ApplyMouseOverCursor();
        }

        private void ApplyMouseOverCursor()
        {
            if (!Game.IsInitialized())
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                this.cursorManager.PointCursor();
                return;
            }

            // Order matters
            if (IsPanel())
            {
                this.cursorManager.PointCursor();
            }
            else if (IsProduceable())
            {
                this.cursorManager.ProduceCursor();
            }
            else if (IsAttackable())
            {
                this.cursorManager.AttackCursor();
            }
            else if (IsMoveable())
            {
                this.cursorManager.MoveCursor(GetMoveHeading());
            }
            else if (IsSelectable())
            {
                this.cursorManager.SelectCursor();
            }
            else if (IsInformational())
            {
                this.cursorManager.InfoCursor();
            }
            else if (IsMagnifyable())
            {
                this.cursorManager.MagnifyCursor();
            }
            else
            {
                // Default
                this.cursorManager.PointCursor();
            }
        }

        public void OnMouseExit()
        {
            var cursorManager = GameObject.FindGameObjectWithTag("UnityManager")
                   .GetComponent<CursorManager>();

            cursorManager.PointCursor();
        }

        /// <summary>
        /// Must have entered production mode and hover over a city.
        /// </summary>        
        private bool IsProduceable()
        {
            if (this.unityManager.ProductionMode == ProductionMode.None)
            {
                return false;
            }

            Tile tile = GetCurrentTile();
            return
                tile != null &&
                (tile.HasCity()) &&
                (tile.City.Clan == Game.Current.GetCurrentPlayer().Clan);
        }

        private bool IsMoveable()
        {
            if (this.transform.name != "WorldTilemap" || !Game.Current.ArmiesSelected())
            {
                return false;
            }

            Tile tile = GetCurrentTile();
            if (tile == null)
            {
                return false;
            }

            return tile.CanTraverseHere(Game.Current.GetSelectedArmies());
        }

        private bool IsMagnifyable()
        {
            return this.transform.name == "MinimapPanel";
        }

        private bool IsAttackable()
        {
            if (this.transform.name != "WorldTilemap")
            {
                return false;
            }

            if (!Game.Current.ArmiesSelected())
            {
                return false;
            }

            Tile tile = GetCurrentTile();
            var armies = Game.Current.GetSelectedArmies();
            return tile != null &&
                tile.CanAttackHere(armies) &&
                InputHandler.IsAdjacentForAttack(tile, armies);
        }

        private bool IsInformational()
        {
            return this.transform.name == "WorldTilemap" && GetCurrentTile() != null;
        }

        private bool IsSelectable()
        {
            if (this.transform.name != "WorldTilemap")
            {
                return false;
            }

            Tile tile = GetCurrentTile();

            return
                tile != null &&
                (tile.HasAnyArmies() &&
                (tile.GetAllArmies()[0].Clan == Game.Current.GetCurrentPlayer().Clan));
        }

        private bool IsPanel()
        {
            var mode = this.unityManager.InputManager.GetInputMode();
            return (mode == InputMode.ItemDropPicker ||
                    mode == InputMode.ItemTakePicker ||
                    mode == InputMode.LocationPicker ||
                    mode == InputMode.LoadGamePicker ||
                    mode == InputMode.SaveGamePicker ||
                    mode == InputMode.WaitForKey ||
                    mode == InputMode.UI);
        }

        private Tile GetCurrentTile()
        {
            return this.worldTilemap.GetTileAtScreenPosition(this.mainCamera, Input.mousePosition);
        }

        private Vector3 GetMoveHeading()
        {
            Vector3 targetPosition = this.mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var tile = Game.Current.GetSelectedArmies()[0].Tile;
            Vector3 playerPosition = this.worldTilemap.ConvertGameToUnityVector(tile.X, tile.Y);

            // Gets a vector that points from the player's position to the targets
            Vector3 heading = targetPosition - playerPosition;
            return heading;
        }
    }
}
