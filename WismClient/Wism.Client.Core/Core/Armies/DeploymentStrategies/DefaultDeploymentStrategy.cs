using System;
using System.Collections.Generic;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core.Armies
{

    public class DefaultDeploymentStrategy : IDeploymentStrategy
    {
        public Tile FindNextOpenTile(Player player, ArmyInfo armyInfo, Tile targetTile)
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

            HashSet<Tile> tilesSeen = new HashSet<Tile>();
            Stack<Tile> tileStack = new Stack<Tile>();
            const int maxAttempts = 10000;
            int attempts = 0;

            // Try current tile
            if (CanDeployHere(player, armyInfo, targetTile))
            {
                return targetTile;
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
                for (int i = 0; i < tiles.Length; i++)
                {
                    if (CanDeployHere(player, armyInfo, tiles[i]))
                    {
                        return tiles[i];
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

                for (int i = 0; i <= tiles.GetUpperBound(0); i++)
                {
                    for (int j = 0; j <= tiles.GetUpperBound(1); j++)
                    {
                        if (tilesSeen.Contains(tiles[i, j]) ||
                            tiles[i, j] == null)
                        {
                            // Cannot deploy here
                            continue;
                        }

                        if (CanDeployHere(player, armyInfo, tiles[i, j]))
                        {
                            // Found a place to deploy
                            return tiles[i, j];
                        }
                        else if (targetTile.CanTraverseHere(player.Clan, armyInfo) &&
                                 IsTileFull(tiles[i, j]))
                        {
                            // Add traversable tiles for further area search
                            tileStack.Push(tiles[i, j]);
                        }

                        tilesSeen.Add(tiles[i, j]);
                    }
                }
            }

            throw new InvalidOperationException("Army cannot be deployed as there are no suitable locations");
        }

        private bool CanDeployHere(Player player, ArmyInfo armyInfo, Tile targetTile)
        {
            return (!IsTileFull(targetTile)) &&
                    targetTile.CanTraverseHere(player.Clan, armyInfo);
        }

        private static bool IsTileFull(Tile targetTile)
        {
            return targetTile.GetAllArmies().Count == Army.MaxArmies;
        }
    }
}
