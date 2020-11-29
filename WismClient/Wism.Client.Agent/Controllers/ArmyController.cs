using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Pathing;

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

            this.logger = loggerFactory.CreateLogger<ArmyController>();
        }

        /// <summary>
        /// Attempt to move the given army to the (x,y) destination.
        /// </summary>
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

            ArmyUtilities.VerifyArmies(logger, armiesToMove);

            // Can we traverse in that terrain?            
            if (!armiesToMove.TrueForAll(
                army => targetTile.CanTraverseHere(army)))
            {
                logger.LogInformation($"{ArmiesToString(armiesToMove)} cannot traverse {targetTile}");
                return false;
            }

            // Do we have enough moves?
            // TODO: Account for terrain bonuses
            // TODO: Shouldn't this check the entire path instead of target tile?
            if (armiesToMove.Any<Army>(army => army.MovesRemaining < targetTile.Terrain.MovementCost))
            {
                logger.LogInformation($"{ArmiesToString(armiesToMove)} has insuffient moves to reach {targetTile}");
                return false;
            }
            
            if (targetTile.HasArmies())
            {
                // Does the tile have room for the unit of the same team?
                if ((targetTile.Armies[0].Clan == armiesToMove[0].Clan) &&
                    (!targetTile.HasRoom(armiesToMove.Count)))
                {
                    logger.LogInformation($"{targetTile} has too many units to move there");
                    return false;
                }

                // Is it an enemy tile?
                if ((targetTile.HasArmies()) &&
                    (targetTile.Armies[0].Clan != armiesToMove[0].Clan))
                {
                    logger.LogInformation(
                        $"Army cannot move {ArmiesToString(armiesToMove)} to {targetTile} as it occupied by {targetTile.Armies[0].Clan}");
                    return false;
                }
            }

            // We are clear to advance!
            MoveSelectedArmies(armiesToMove, targetTile);

            return true;
        }

        /// <summary>
        /// Advance the army one step along the shortest route.
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        public bool TryMoveOneStep(List<Army> armiesToMove, Tile targetTile, ref IList<Tile> path, out float distance)
        {
            if (armiesToMove is null || armiesToMove.Count == 0)
            {
                throw new ArgumentNullException(nameof(armiesToMove));
            }

            if (targetTile is null)
            {
                throw new ArgumentNullException(nameof(targetTile));
            }

            if (path != null && path.Count == 1)
            {
                // We have arrived
                logger.LogInformation($"{ArmiesToString(armiesToMove)} arrived at its destination.");
                path.Clear();
                distance = 0;
                return false;
            }

            IList<Tile> myPath = path;

            float myDistance;
            if (myPath == null)
            {
                // No current path; calculate the shortest route
                IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
                pathingStrategy.FindShortestRoute(World.Current.Map, armiesToMove, targetTile, out myPath, out _);

                if (myPath == null || myPath.Count == 0)
                {
                    // Impossible route
                    logger.LogInformation($"Path between {ArmiesToString(armiesToMove)} and {targetTile} is impassable.");

                    path = null;
                    distance = 0.0f;
                    return false;
                }
            }

            // Now we have a route
            bool moveSuccessful = TryMove(armiesToMove, myPath[1]);
            if (moveSuccessful)
            {
                // Pop the starting location and return updated path and distance
                myPath.RemoveAt(0);
                myDistance = CalculateDistance(myPath);
            }
            else
            {
                logger.LogWarning($"Move failed during path traversal to: {myPath[1]}");
                myPath = null;
                myDistance = 0;
            }

            path = myPath;
            distance = myDistance;
            return moveSuccessful;
        }

        /// <summary>
        /// Selects the army to prepare to move or attack (changes from "Armies" to "VisitingArmies".
        /// </summary>
        /// <param name="armies">Armies to select</param>
        public void SelectArmy(List<Army> armies)
        {
            if (armies is null || armies.Count == 0)
            {
                throw new ArgumentNullException(nameof(armies));
            }

            ArmyUtilities.VerifyArmies(logger, armies);

            Tile tile = armies[0].Tile;
            if (tile.HasVisitingArmies())
            {
                throw new InvalidOperationException(
                    $"Tile already has visiting armies: {ArmiesToString(tile.VisitingArmies)}");
            }

            // Move selected armies to Visiting Armies
            logger.LogInformation($"Selecting army: {ArmiesToString(armies)}");
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
        /// Deselects armies (changes from "VisitingArmies" to "Armies").
        /// </summary>
        /// <param name="armies">Armies to commit to current tile</param>
        public void DeselectArmy(List<Army> armies)
        {
            if (armies is null || armies.Count == 0)
            {
                throw new ArgumentNullException(nameof(armies));
            }

            // Simplify deselect for attack scenarios
            var armiesToDeselect = RemoveDeadArmies(armies);

            ArmyUtilities.VerifyArmies(logger, armiesToDeselect);

            armiesToDeselect[0].Tile.CommitVisitingArmies();
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

            ArmyUtilities.VerifyArmies(logger, armiesToAttackWith);

            var attackingFromTile = armiesToAttackWith[0].Tile;

            logger.LogInformation($"{ArmiesToString(armiesToAttackWith)} attacking {targetTile}");
            var war = Game.Current.WarStrategy;
            var result = war.Attack(armiesToAttackWith, targetTile);
            CleanupAfterBattle(targetTile, attackingFromTile, result);

            return result;
        }

        private static void CleanupAfterBattle(Tile targetTile, Tile attackingFromTile, bool result)
        {
            if (result)
            {
                // Attacker won
                targetTile.Armies = null;
            }
            else
            {
                // Defender won
                attackingFromTile.VisitingArmies = null;
            }
        }

        private static void MoveSelectedArmies(List<Army> armiesToMove, Tile targetTile)
        {
            Tile originatingTile = armiesToMove[0].Tile;

            targetTile.VisitingArmies = armiesToMove;
            armiesToMove.ForEach(a =>
            {
                a.Tile = targetTile;

                // TODO: Account for bonuses
                a.MovesRemaining -= targetTile.Terrain.MovementCost;   
            });

            targetTile.VisitingArmies.Sort(new ByArmyViewingOrder());

            RemoveArmiesFromOrginatingTile(armiesToMove, originatingTile);
        }

        private static void RemoveArmiesFromOrginatingTile(List<Army> armiesToRemove, Tile originatingTile)
        {
            originatingTile.VisitingArmies = null;
        }

        private static int CalculateDistance(IList<Tile> myPath)
        {
            // TODO: Calculate based on true unit and affiliation cost; for now, static
            return myPath.Sum<Tile>(tile => tile.Terrain.MovementCost);
        }

        private static string ArmiesToString(List<Army> armies)
        {
            return $"Armies[{armies.Count}:{armies[0]}]";
        }

        private static List<Army> RemoveDeadArmies(List<Army> armies)
        {
            var armiesToReturn = new List<Army>(armies);
            foreach (Army army in armies)
            {
                if (!army.IsDead)
                {
                    armiesToReturn.Add(army);
                }
            }

            return armiesToReturn;
        }
    }
}
