using System;
using System.Collections.Generic;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Core.Armies.DeploymentStrategies
{
    public class DefaultDeploymentStrategy : IDeploymentStrategy
    {
        public Tile FindNextOpenTile(Player player, ArmyInfo armyInfo, Tile targetTile)
        {
            if (this.TryFindNextOpenTile(player, armyInfo, targetTile, out var openTile))
            {
                return openTile;
            }

            throw new InvalidOperationException("Army cannot be deployed as there are no suitable locations");
        }

        public bool TryFindNextOpenTile(Player player, ArmyInfo armyInfo, Tile targetTile, out Tile openTile)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (armyInfo is null)
            {
                throw new ArgumentNullException(nameof(armyInfo));
            }

            if (targetTile is null)
            {
                throw new ArgumentNullException(nameof(targetTile));
            }

            openTile = null;
            var tilesSeen = new HashSet<Tile>();
            var tileStack = new Stack<Tile>();
            const int maxAttempts = 10000;
            var attempts = 0;

            // Try current tile
            if (this.CanDeployHere(player, armyInfo, targetTile))
            {
                openTile = targetTile;
                return true;
            }

            // If no city then search around target tile (testing only)
            if (!targetTile.HasCity())
            {
                tileStack.Push(targetTile);
            }

            // Try all city tiles
            if (targetTile.HasCity() &&
                targetTile.City.Clan == player.Clan)
            {
                var tiles = targetTile.City.GetTiles();
                for (var i = 0; i < tiles.Length; i++)
                {
                    if (this.CanDeployHere(player, armyInfo, tiles[i]))
                    {
                        openTile = tiles[i];
                        return true;
                    }

                    // Save tile for future search
                    tileStack.Push(tiles[i]);
                    tilesSeen.Add(tiles[i]);
                }
            }

            // Place outside city
            // Search until max attempts reached
            while (attempts++ < maxAttempts)
            {
                // Are there no more options?
                if (tileStack.Count == 0)
                {
                    break;
                }

                var tile = tileStack.Pop();
                var tiles = tile.GetNineGrid();

                for (var i = 0; i <= tiles.GetUpperBound(0); i++)
                {
                    for (var j = 0; j <= tiles.GetUpperBound(1); j++)
                    {
                        if (tilesSeen.Contains(tiles[i, j]) ||
                            tiles[i, j] == null)
                        {
                            // Cannot deploy here
                            continue;
                        }

                        if (this.CanDeployHere(player, armyInfo, tiles[i, j]))
                        {
                            // Found a place to deploy
                            openTile = tiles[i, j];
                            return true;
                        }

                        if (tiles[i, j].CanTraverseHere(player.Clan, armyInfo) &&
                            IsTileFull(tiles[i, j]))
                        {
                            // Add traversable tiles for further area search
                            tileStack.Push(tiles[i, j]);
                        }

                        tilesSeen.Add(tiles[i, j]);
                    }
                }
            }

            return false;
        }

        private bool CanDeployHere(Player player, ArmyInfo armyInfo, Tile targetTile)
        {
            return !IsTileFull(targetTile) &&
                   IsFriendlyDeploymentArea(player, targetTile) &&
                   targetTile.CanTraverseHere(player.Clan, armyInfo);
        }

        private static bool IsTileFull(Tile targetTile)
        {
            return targetTile.GetAllArmies().Count == Army.MaxArmies;
        }

        private static bool IsFriendlyDeploymentArea(Player player, Tile targetTile)
        {
            if (targetTile.HasCity())
            {
                var cityTiles = targetTile.City.GetTiles();
                for (var i = 0; i < cityTiles.Length; i++)
                {
                    if (!ContainsOnlyFriendlyArmies(player, cityTiles[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return ContainsOnlyFriendlyArmies(player, targetTile);
        }

        private static bool ContainsOnlyFriendlyArmies(Player player, Tile targetTile)
        {
            foreach (var army in targetTile.GetAllArmies())
            {
                if (army.Player != player)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
