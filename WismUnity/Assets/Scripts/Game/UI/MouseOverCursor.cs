﻿using Assets.Scripts.Managers;
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
            unityManager = GameObject.FindGameObjectWithTag("UnityManager")
                .GetComponent<UnityManager>();

            cursorManager = unityManager.GetComponent<CursorManager>();

            worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();

            mainCamera = GameObject.FindGameObjectWithTag("MainCamera")
                .GetComponent<Camera>();           
        }

        public void OnMouseOver()
        {
            // Order matters
            if (IsPanel())
            {
                cursorManager.PointCursor();
            }
            else if (IsProduceable())
            {
                cursorManager.ProduceCursor();
            }            
            else if (IsAttackable())
            {
                cursorManager.AttackCursor();
            }
            else if (IsMoveable())
            {
                cursorManager.MoveCursor(GetMoveHeading());
            }
            else if (IsSelectable())
            {
                cursorManager.SelectCursor();
            }
            else if (IsInformational())
            {
                cursorManager.InfoCursor();
            }
            else if (IsMagnifyable())
            {
                cursorManager.MagnifyCursor();
            }
            else
            {
                // Default
                cursorManager.PointCursor();
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
            if (unityManager.ProductionMode == ProductionMode.None)
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
            return transform.name == "MinimapPanel";
        }

        private bool IsAttackable()
        {
            if (transform.name != "WorldTilemap")
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
            return (transform.name == "WorldTilemap");
        }

        private bool IsSelectable()
        {
            if (transform.name != "WorldTilemap")
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
            var mode = unityManager.InputManager.GetInputMode();
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
            var worldVector = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var gameCoords = worldTilemap.ConvertUnityToGameVector(worldVector);
            var tile = World.Current.Map[
                Mathf.Clamp(gameCoords.x, World.Current.Map.GetLowerBound(0), World.Current.Map.GetUpperBound(0)),
                Mathf.Clamp(gameCoords.y, World.Current.Map.GetLowerBound(1), World.Current.Map.GetUpperBound(1))];
            return tile;
        }

        private Vector3 GetMoveHeading()
        {
            Vector3 targetPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var tile = Game.Current.GetSelectedArmies()[0].Tile;
            Vector3 playerPosition = worldTilemap.ConvertGameToUnityVector(tile.X, tile.Y);

            // Gets a vector that points from the player's position to the targets
            Vector3 heading = targetPosition - playerPosition;
            return heading;
        }
    }
}