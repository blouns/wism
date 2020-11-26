using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public Army CreateArmy(Player player, ArmyInfo info)
        {
            return ArmyFactory.CreateArmy(player, info);
        }

        /// <summary>
        /// Attempt to move the given army to the (x,y) destination.
        /// </summary>
        /// <param name="armiesToMove">Army to move</param>
        /// <param name="x">X-coordinate to move to</param>
        /// <param name="y">Y-coordinate to move to</param>
        public bool TryMove(List<Army> armiesToMove, Tile targetTile)
        {
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
                // Does the tile has room for the unit of the same team?
                if ((targetTile.Armies[0].Clan == armiesToMove[0].Clan) &&
                    (!targetTile.HasRoom(armiesToMove.Count)))
                {
                    return false;
                }

                // Is it an enemy tile?
                if ((targetTile.HasArmies()) &&
                    (targetTile.Armies[0].Clan != armiesToMove[0].Clan))
                {
                    IWarStrategy war = Game.Current.WarStrategy;

                    // WAR! ...in a senseless mind.

                    // TODO: Do not enagage in combat without explicit AttackCommand
                    if (!war.Attack(armiesToMove, targetTile))
                    {
                        // We have lost!
                        return false;
                    }
                }
                else
                {
                    // TODO: Need to implement a selected cohort within the army
                    //       Perhaps implemented using multiple Army objects in an
                    //       army, or using a new Cohort class.
                    //
                    //       Until then, do not auto-merge armies that are moving.
                    return false;
                }
            }

            // We are clear to advance!
            MoveSelectedArmies(armiesToMove, targetTile);

            return true;
        }

        // TODO: Incorporate the "Visiting Armies" slot
        private void MoveSelectedArmies(List<Army> armiesToMove, Tile targetTile)
        {
            Tile originatingTile = armiesToMove[0].Tile;

            targetTile.Armies = armiesToMove;
            armiesToMove.ForEach(a =>
            {
                a.Tile = targetTile;
                a.MovesRemaining -= targetTile.Terrain.MovementCost;   // TODO: Account for bonuses
            });

            //targetTile.Armies.Sort(new ByUnitViewingOrder());

            RemoveArmiesFromOrginatingTile(armiesToMove, originatingTile);
        }

        private static void RemoveArmiesFromOrginatingTile(List<Army> armiesToRemove, Tile originatingTile)
        {
            // If moving entire army (not a subset)
            if (originatingTile.Armies.Count == armiesToRemove.Count)
            {
                originatingTile.Armies = null;
                return;
            }

            // Otherwise remove moved units from original army
            foreach (Army armyToRemove in armiesToRemove)
            {
                originatingTile.Armies.Remove(armyToRemove);
            }

            if (originatingTile.Armies.Count == 0)
            {
                originatingTile.Armies = null;
            }
            else
            {
                //originatingTile.Army.units.Sort(new ByUnitViewingOrder());
            }
        }
    }
}
