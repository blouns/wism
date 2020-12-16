using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Pathing;

namespace Wism.Client.Core.Controllers
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

        public void DefendArmy(List<Army> armies)
        {
            logger.LogInformation("Setting armies to defensive sentry.");
            Game.Current.DefendSelectedArmies();
        }

        public bool SelectNextArmy()
        {
            logger.LogInformation("Selecting next non-defending army with moves.");
            return Game.Current.SelectNextArmy();
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

            // Can we traverse in that terrain?            
            if (!armiesToMove.TrueForAll(
                army => targetTile.CanTraverseHere(army)))
            {
                logger.LogInformation($"{ArmiesToString(armiesToMove)} cannot traverse {targetTile}");
                Game.Current.Transition(GameState.SelectedArmy);
                return false;
            }

            // Do we have enough moves?
            // TODO: Account for terrain bonuses
            if (armiesToMove.Any(army => army.MovesRemaining < targetTile.Terrain.MovementCost))
            {
                logger.LogInformation($"{ArmiesToString(armiesToMove)} has insuffient moves to reach {targetTile}");
                Game.Current.Transition(GameState.Ready);
                return false;
            }
            
            if (targetTile.HasArmies())
            {
                // Does the tile have room for the unit of the same team?
                if ((targetTile.Armies[0].Clan == armiesToMove[0].Clan) &&
                    (!targetTile.HasRoom(armiesToMove.Count)))
                {
                    logger.LogInformation($"{targetTile} has too many units to move there");
                    Game.Current.Transition(GameState.SelectedArmy);
                    return false;
                }

                // Is it an enemy tile?
                if ((targetTile.HasArmies()) &&
                    (targetTile.Armies[0].Clan != armiesToMove[0].Clan))
                {
                    logger.LogInformation(
                        $"Army cannot move {ArmiesToString(armiesToMove)} to {targetTile} " +
                        $"as it occupied by {targetTile.Armies[0].Clan}");
                    Game.Current.Transition(GameState.SelectedArmy);
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
        /// <param name="armiesToMove">Armies to move</param>
        /// <param name="targetTile">Tile to move towards</param>
        /// <param name="path">Path to the tile (null input will find a new route)</param>
        /// <param name="distance">Distance in moves to tile</param>
        /// <returns>
        /// Action state:
        ///   * Success if move completed
        ///   * Failed if blocked or out of moves
        ///   * In-Progress if destination not yet reached
        /// </returns>
        public ActionState MoveOneStep(List<Army> armiesToMove, Tile targetTile, ref IList<Tile> path, out float distance)
        {
            if (armiesToMove is null || armiesToMove.Count == 0)
            {
                throw new ArgumentNullException(nameof(armiesToMove));
            }

            if (targetTile is null)
            {
                throw new ArgumentNullException(nameof(targetTile));
            }

            // Have we arrived at our destination?
            if (AtDestination(path))
            {
                PrepareArmiesForArrival(armiesToMove, path, out distance);
                return ActionState.Succeeded;
            }

            // Get a route            
            float myDistance = 0.0f;
            IList<Tile> myPath = path;
            if (NoPathYet(myPath))
            {
                myPath = FindPath(armiesToMove, targetTile, ref myDistance);
                if (myPath == null)
                {
                    // Impossible route
                    distance = myDistance;
                    return ActionState.Failed;
                }
            }

            // Now we have a route; move ahead
            ActionState result;
            if (TryMove(armiesToMove, myPath[1]))
            {
                // Pop the starting location and return updated path and distance
                myPath.RemoveAt(0);
                myDistance = CalculateDistance(myPath);
                result = ActionState.InProgress;
            }
            else
            {
                logger.LogWarning($"Move failed during path traversal to: {myPath[1]}");
                myPath = null;
                myDistance = 0;
                result = ActionState.Failed;
            }

            path = myPath;
            distance = myDistance;
            return result;
        }

        private IList<Tile> FindPath(List<Army> armiesToMove, Tile targetTile, ref float distance)
        {
            IList<Tile> path;

            // Calculate the shortest route
            IPathingStrategy pathingStrategy = new DijkstraPathingStrategy();
            pathingStrategy.FindShortestRoute(World.Current.Map, armiesToMove, targetTile, out path, out _);

            if (path == null || path.Count == 0)
            {
                // Impossible route
                logger.LogInformation($"Path between {ArmiesToString(armiesToMove)} and {targetTile} is impassable.");

                distance = 0.0f;
                path = null;                
                Game.Current.Transition(GameState.SelectedArmy);
            }

            return path;
        }

        private static bool NoPathYet(IList<Tile> myPath)
        {
            return myPath == null;
        }

        private static bool AtDestination(IList<Tile> path)
        {
            return path != null && path.Count == 1;
        }

        private void PrepareArmiesForArrival(List<Army> armiesToMove, IList<Tile> path, out float distance)
        {
            logger.LogInformation($"{ArmiesToString(armiesToMove)} arrived at its destination.");
            path.Clear();
            distance = 0;

            Game.Current.Transition(GameState.SelectedArmy);
            if (armiesToMove.Any(a => a.MovesRemaining == 0))
            {
                Game.Current.DeselectArmies();
            }
        }

        /// <summary>
        /// Selects the army to prepare to move or attack (changes from "Armies" to "VisitingArmies").
        /// </summary>
        /// <param name="armies">Armies to select</param>
        public void SelectArmy(List<Army> armies)
        {
            if (armies is null || armies.Count == 0)
            {
                throw new ArgumentNullException(nameof(armies));
            }

            Game.Current.SelectArmies(armies);
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

            Game.Current.DeselectArmies();
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

            if (!targetTile.CanAttackHere(armiesToAttackWith))
            {
                throw new ArgumentException("Target tile must have armies of another clan.");
            }

            var attackingFromTile = armiesToAttackWith[0].Tile;

            logger.LogInformation($"{ArmiesToString(armiesToAttackWith)} attacking {targetTile}");
            Game.Current.Transition(GameState.AttackingArmy);
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
                if (targetTile.HasCity())
                {
                    var player = attackingFromTile.VisitingArmies[0].Player;
                    player.ClaimCity(targetTile.City);
                }
                Game.Current.Transition(GameState.SelectedArmy);
            }
            else
            {
                // Defender won                
                attackingFromTile.VisitingArmies = null;
                Game.Current.Transition(GameState.Ready);
            }
        }

        private static void MoveSelectedArmies(List<Army> armiesToMove, Tile targetTile)
        {
            Game.Current.Transition(GameState.MovingArmy);

            Tile originatingTile = armiesToMove[0].Tile;

            targetTile.VisitingArmies = armiesToMove;
            armiesToMove.ForEach(a =>
            {
                a.Tile = targetTile;

                // TODO: Account for bonuses
                a.MovesRemaining -= targetTile.Terrain.MovementCost;
            });

            targetTile.VisitingArmies.Sort(new ByArmyViewingOrder());
            originatingTile.VisitingArmies = null;

            if (armiesToMove.Any(a => a.MovesRemaining == 0))
            {
                // Ran out of moves so just stop here
                Game.Current.DeselectArmies();
            }

            Game.Current.Transition(GameState.SelectedArmy);
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
    }
}
