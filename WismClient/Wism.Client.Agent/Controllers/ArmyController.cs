using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Agent.Factories;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.War;

namespace Wism.Client.Agent.Controllers
{
    public class ArmyController
    {
        private readonly ILogger logger;

        public ArmyController(ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger<CommandController>();
        }

        /// <summary>
        /// Attempt to move the given army to the (x,y) destination.
        /// </summary>
        /// <param name="armiesToMove">Army to move</param>
        /// <param name="x">X-coordinate to move to</param>
        /// <param name="y">Y-coordinate to move to</param>
        public bool TryMove(List<Army> armiesToMove, Tile targetTile)
        {
            if (targetTile is null)
            {
                throw new ArgumentNullException(nameof(targetTile));
            }

            if (armiesToMove is null || armiesToMove.Count == 0)
            {
                throw new ArgumentNullException(nameof(armiesToMove));
            }

            VerifyArmiesStillAlive(armiesToMove);

            // Can we traverse in that terrain?            
            if (!armiesToMove.TrueForAll(
                army => targetTile.CanTraverseHere(army)))
            {
                return false;
            }

            // Do we have enough moves?
            // TODO: Account for terrain bonuses
            // TODO: Shouldn't this check the entire path instead of target tile?
            if (armiesToMove.Any<Army>(army => army.MovesRemaining < targetTile.Terrain.MovementCost))
            {
                return false;
            }
            
            if (targetTile.HasArmies())
            {
                // Does the tile have room for the unit of the same team?
                if ((targetTile.Armies[0].Clan == armiesToMove[0].Clan) &&
                    (!targetTile.HasRoom(armiesToMove.Count)))
                {
                    return false;
                }

                // Is it an enemy tile?
                if ((targetTile.HasArmies()) &&
                    (targetTile.Armies[0].Clan != armiesToMove[0].Clan))
                {
                    return false;
                }
            }

            // We are clear to advance!
            MoveSelectedArmies(armiesToMove, targetTile);

            return true;
        }

        /// <summary>
        /// Prepares the army to move (changes from "Armies" to "VisitingArmies".
        /// </summary>
        /// <param name="armies">Armies to move</param>
        public void StartMoving(List<Army> armies)
        {
            if (armies is null || armies.Count == 0)
            {
                throw new ArgumentNullException(nameof(armies));
            }

            VerifyArmiesStillAlive(armies);

            var tile = armies[0].Tile;
            if (!armies.TrueForAll(a => a.Tile == tile))
            {
                throw new ArgumentException("All armies must originate from the same location.");
            }

            if (tile.HasVisitingArmies())
            {
                throw new InvalidOperationException("Tile already has visiting armies.");
            }

            // Move selected armies to Visiting Armies
            tile.VisitingArmies = new List<Army>(armies);
            foreach (Army army in armies)
            {
                tile.Armies.Remove(army);
            }

            // Clean up tile's armies
            if (tile.HasArmies())
            {
                tile.Armies.Sort(new ByArmyViewingOrder());
            }
            else
            {
                tile.Armies = null;
            }
        }

        /// <summary>
        /// Commits the armies to their current tile (changes from "VisitingArmies" to "Armies").
        /// </summary>
        /// <param name="armies">Armies to commit to current tile</param>
        public void StopMoving(List<Army> armies)
        {
            if (armies is null || armies.Count == 0)
            {
                throw new ArgumentNullException(nameof(armies));
            }

            VerifyArmiesStillAlive(armies);

            var tile = armies[0].Tile;
            if (!armies.TrueForAll(a => a.Tile == tile))
            {
                throw new ArgumentException("All armies must originate from the same location.");
            }

            tile.CommitVisitingArmies();
        }

        /// <summary>
        /// WAR! ...in a senseless mind. Attack until win or lose.
        /// </summary>
        /// <param name="armies">Attacking armies</param>
        /// <param name="targetTile">Defending tile</param>
        /// <returns>True if attacker wins; else False</returns>
        public bool TryAttack(List<Army> armiesToAttackWith, Tile targetTile)
        {
            if (armiesToAttackWith is null || armiesToAttackWith.Count == 0)
            {
                throw new ArgumentNullException(nameof(armiesToAttackWith));
            }

            if (targetTile is null)
            {
                throw new ArgumentNullException(nameof(targetTile));
            }

            VerifyArmiesStillAlive(armiesToAttackWith);

            var tile = armiesToAttackWith[0].Tile;
            if (!armiesToAttackWith.TrueForAll(a => a.Tile == tile))
            {
                throw new ArgumentException("All attacking armies must originate from the same location.");
            }

            var war = Game.Current.WarStrategy;
            return war.Attack(armiesToAttackWith, targetTile);            
        }

        // TODO: Incorporate the "Visiting Armies" slot
        private void MoveSelectedArmies(List<Army> armiesToMove, Tile targetTile)
        {
            Tile originatingTile = armiesToMove[0].Tile;

            targetTile.VisitingArmies = armiesToMove;
            armiesToMove.ForEach(a =>
            {
                a.Tile = targetTile;
                a.MovesRemaining -= targetTile.Terrain.MovementCost;   // TODO: Account for bonuses
            });

            targetTile.VisitingArmies.Sort(new ByArmyViewingOrder());

            RemoveArmiesFromOrginatingTile(armiesToMove, originatingTile);
        }

        private static void RemoveArmiesFromOrginatingTile(List<Army> armiesToRemove, Tile originatingTile)
        {
            originatingTile.VisitingArmies = null;
        }

        private static void VerifyArmiesStillAlive(List<Army> armies)
        {
            if (armies.Any<Army>(army => army.IsDead))
            {
                throw new InvalidOperationException("Cannot move a dead army!");
            }
        }
    }
}
