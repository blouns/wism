using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.UI;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts
{
    public class WismInputHandler
    {
        private UnityManager unityManager;
        private GameManager gameManager;
        private WorldTilemap worldTilemap;

        // Selected objects        
        private int selectedArmyIndex;
        private Tile currentTile;

        public WismInputHandler(UnityManager unityManager)
        {
            this.unityManager = unityManager;
            this.gameManager = unityManager.GetComponent<GameManager>();
            this.worldTilemap = UnityUtilities.GameObjectHardFind("WorldTilemap")
                .GetComponent<WorldTilemap>();

            Initialize();
        }

        private void Initialize()
        {            
            this.selectedArmyIndex = -1;
        }

        internal void HandleArmyClick(bool isDoubleClick, Tile clickedTile)
        {
            switch (Game.Current.GameState)
            {
                case GameState.Ready:
                    SelectObject(clickedTile, isDoubleClick);
                    break;

                case GameState.SelectedArmy:
                    if (Game.Current.ArmiesSelected() &&
                        clickedTile == Game.Current.GetSelectedArmies()[0].Tile)
                    {
                        // Clicking on already selected tile
                        SelectObject(clickedTile, isDoubleClick);
                        break;
                    }

                    // Move or attack; can only attack from adjacent tiles
                    var armies = Game.Current.GetSelectedArmies();
                    bool isAttacking = clickedTile.CanAttackHere(armies);
                    bool isAdjacent = IsAdjacentForAttack(clickedTile, armies);
                    if (isAttacking && isAdjacent)
                    {
                        // War!
                        gameManager.AttackWithSelectedArmies(clickedTile.X, clickedTile.Y);
                    }
                    // Cannot attack from non-adjacent tile
                    else if (isAttacking & !isAdjacent)
                    {
                        // Do nothing
                        Debug.Log("Too far away to attack.");
                    }
                    else if (!isAttacking)
                    {
                        // Move
                        gameManager.MoveSelectedArmies(clickedTile.X, clickedTile.Y);
                    }
                    break;
            }
        }

        internal static bool IsAdjacentForAttack(Tile clickedTile, List<Army> armies)
        {
            var isAdjacent = clickedTile.IsNeighbor(armies[0].Tile);

            if (clickedTile.HasCity())
            {
                var cityTiles = clickedTile.City.GetTiles();
                for (int i = 0; i < cityTiles.Length; i++)
                {
                    isAdjacent |= cityTiles[i].IsNeighbor(armies[0].Tile);
                }
            }

            return isAdjacent;
        }

        internal void HandleCityClick(Tile tile)
        {
            switch (unityManager.ProductionMode)
            {
                case ProductionMode.SelectCity:
                    if (tile.HasCity() &&
                        tile.City.Clan == Game.Current.GetCurrentPlayer().Clan)
                    {
                        unityManager.ShowProductionPanel(tile.City);
                    }
                    break;
                default:
                    // Do nothing
                    break;
            }
        }

        internal void SelectObject(Tile tile, bool selectAll)
        {
            // If no owned armies on selected tile then center screen
            if ((Game.Current.GameState == GameState.Ready) &&
                !tile.HasVisitingArmies() &&
                (!tile.HasArmies() ||
                    (tile.HasArmies() &&
                    tile.Armies[0].Player != Game.Current.GetCurrentPlayer())))
            {
                CenterOnTile(tile);
                return;
            }

            var armiesToSelect = new List<Army>();
            if (selectAll)
            {
                // Selecting all armies on tile
                this.selectedArmyIndex = -1;
                armiesToSelect = tile.GetAllArmies();
            }
            else if ((tile.HasVisitingArmies() && tile.VisitingArmies.Count > 1) ||
                     (!tile.HasVisitingArmies() && tile.HasArmies()))
            {
                // Select "top" army on tile
                var allArmies = tile.GetAllArmies();
                allArmies.Sort(new ByArmyViewingOrder());

                this.selectedArmyIndex = 0;
                armiesToSelect.Add(allArmies[0]);
            }
            else if (tile.HasVisitingArmies() && tile.VisitingArmies.Count == 1 &&
                     tile.HasArmies())
            {
                // Cycle next army on tile
                var allArmies = tile.GetAllArmies();
                allArmies.Sort(new ByArmyViewingOrder());

                // Now there are only Armies (no Visiting Armies)
                this.selectedArmyIndex = (this.selectedArmyIndex + 1) % allArmies.Count;
                armiesToSelect.Add(allArmies[this.selectedArmyIndex]);
            }

            if (armiesToSelect.Count > 0)
            {
                armiesToSelect.Sort(new ByArmyViewingOrder());
                gameManager.SelectArmies(armiesToSelect);
                CenterOnTile(tile);
            }
        }

        internal void DeselectObject()
        {
            if (Game.Current.ArmiesSelected())
            {
                gameManager.DeselectArmies();
            }

            this.unityManager.HideSelectedBox();
            this.selectedArmyIndex = -1;
            this.currentTile = null;
        }

        internal void CenterOnTile(Tile clickedTile)
        {
            Vector3 worldVector = worldTilemap.ConvertGameToUnityVector(clickedTile.X, clickedTile.Y);
            this.unityManager.SetSelectedBoxPosition(worldVector, false);

            if (clickedTile.X >= 0 && clickedTile.X <= World.Current.Map.GetUpperBound(0) &&
                clickedTile.Y >= 0 && clickedTile.Y <= World.Current.Map.GetUpperBound(1))
            {
                this.currentTile = clickedTile;
                Debug.Log(this.currentTile);
            }
        }

        internal Tile GetCurrentTile()
        {
            return this.currentTile;
        }

        internal void SetCurrentTile(Tile tile)
        {
            this.currentTile = tile;
        }
    }
}
