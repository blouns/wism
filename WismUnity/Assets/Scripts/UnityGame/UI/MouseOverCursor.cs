using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using UnityEngine;
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
            if (!Game.IsInitialized())
            {
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
                (tile.HasCity()) &&
                (tile.City.Clan == Game.Current.GetCurrentPlayer().Clan);
        }

        private bool IsMoveable()
        {
            return Game.Current.ArmiesSelected();
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

            Tile tile = GetCurrentTile();

            return
                Game.Current.ArmiesSelected() &&
                    (tile.HasArmies() && (tile.Armies[0].Clan != Game.Current.GetCurrentPlayer().Clan) ||
                    (tile.HasCity() && (tile.City.Clan != Game.Current.GetCurrentPlayer().Clan)));
        }

        private bool IsInformational()
        {
            return (this.transform.name == "WorldTilemap");
        }

        private bool IsSelectable()
        {
            if (this.transform.name != "WorldTilemap")
            {
                return false;
            }

            Tile tile = GetCurrentTile();

            return
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
            var worldVector = this.mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var gameCoords = this.worldTilemap.ConvertUnityToGameVector(worldVector);
            var tile = World.Current.Map[
                Mathf.Clamp(gameCoords.x, World.Current.Map.GetLowerBound(0), World.Current.Map.GetUpperBound(0)),
                Mathf.Clamp(gameCoords.y, World.Current.Map.GetLowerBound(1), World.Current.Map.GetUpperBound(1))];
            return tile;
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
