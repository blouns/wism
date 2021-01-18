using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using System;
using UnityEngine;
using UnityEngine.UI;
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
            if (IsProduceable())
            {
                cursorManager.ProduceCursor();
            }
            else if (IsSelectable())
            {
                cursorManager.SelectCursor();
            }
            else if (IsAttackable())
            {
                cursorManager.AttackCursor();
            }
            else if (IsMoveable())
            {
                cursorManager.MoveCursor();
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
            if (!unityManager.IsProductionMode())
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
            return false;
        }

        private bool IsMagnifyable()
        {
            return transform.name == "MinimapPanel";
        }

        private bool IsAttackable()
        {
            return false;
        }

        private bool IsInformational()
        {
            return false;
        }

        private bool IsSelectable()
        {
            Tile tile = GetCurrentTile();

            // TODO: Check for certain game states
            return 
                (tile.HasAnyArmies() &&
                (tile.GetAllArmies()[0].Clan == Game.Current.GetCurrentPlayer().Clan));
        }

        private Tile GetCurrentTile()
        {
            var worldVector = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var gameCoords = worldTilemap.ConvertUnityToGameCoordinates(worldVector);
            var tile = World.Current.Map[gameCoords.Item1, gameCoords.Item2];
            return tile;
        }
    }
}
