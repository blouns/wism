using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Common;
using Wism.Client.MapObjects;
using Wism.Client.Pathing;

namespace Wism.Client.Core.Controllers
{
    public enum MoveResult
    {
        Moved,
        InsuffientMoves,
        Blocked    
    }

    public enum AttackResult
    {
        NotStarted,
        AttackerWinsRound,
        AttackerWinsBattle,
        DefenderWinsRound,
        DefenderWinBattle
    }

    public class ArmyController
    {
        private readonly ILogger logger;

        public ArmyController(ILoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
        }

        public ActionState PrepareForBattle()
        {
            if (!Game.Current.ArmiesSelected())
            {
                logger.LogInformation("Cannot prepare for battle when no armies are selected.");
                return ActionState.Failed;
            }

            logger.LogInformation("Transitioning to AttackingArmy state.");
            Game.Current.Transition(GameState.AttackingArmy);

            return ActionState.Succeeded;
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
        public MoveResult TryMove(List<Army> armiesToMove, Tile targetTile)
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
                return MoveResult.Blocked;
            }

            // Do we have enough moves?
            // TODO: Account for terrain bonuses
            if (armiesToMove.Any(army => army.MovesRemaining < targetTile.Terrain.MovementCost))
            {
                logger.LogInformation($"{ArmiesToString(armiesToMove)} has insuffient moves to reach {targetTile}");
                Game.Current.DeselectArmies();
                return MoveResult.InsuffientMoves;
            }
            
            if (targetTile.HasArmies())
            {
                // Does the tile have room for the unit of the same team?
                if ((targetTile.Armies[0].Clan == armiesToMove[0].Clan) &&
                    (!targetTile.HasRoom(armiesToMove.Count)))
                {
                    logger.LogInformation($"{targetTile} has too many units to move there");
                    Game.Current.Transition(GameState.SelectedArmy);
                    return MoveResult.Blocked;
                }

                // Is it an enemy tile?
                if ((targetTile.HasArmies()) &&
                    (targetTile.Armies[0].Clan != armiesToMove[0].Clan))
                {
                    logger.LogInformation(
                        $"Army cannot move {ArmiesToString(armiesToMove)} to {targetTile} " +
                        $"as it occupied by {targetTile.Armies[0].Clan}");
                    Game.Current.Transition(GameState.SelectedArmy);
                    return MoveResult.Blocked;
                }
            }

            // We are clear to advance!
            logger.LogInformation($"Moving {ArmiesToString(armiesToMove)} to {targetTile}");
            MoveSelectedArmies(armiesToMove, targetTile);

            return MoveResult.Moved;
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
            var moveResult = TryMove(armiesToMove, myPath[1]);
            if (moveResult == MoveResult.Moved)
            {
                logger.LogInformation($"Moved {ArmiesToString(armiesToMove)} to {targetTile}");

                // Pop the starting location and return updated path and distance
                myPath.RemoveAt(0);
                myDistance = CalculateDistance(myPath);
                result = ActionState.InProgress;
            }
            else if (moveResult == MoveResult.InsuffientMoves)
            {
                // Could not move due to lack of moves remaining
                logger.LogWarning($"Could not move due to lack of moves remaining to: {myPath[1]}");
                result = ActionState.Failed;
            }
            else
            {
                logger.LogWarning($"Move was blocked: {myPath[1]}");
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

        public AttackResult AttackOnce(List<Army> armiesToAttackWith, Tile targetTile)
        {
            AttackResult result;
            Tile attackingFromTile = armiesToAttackWith[0].Tile;

            if (armiesToAttackWith is null || armiesToAttackWith.Count == 0)
            {
                throw new ArgumentNullException(nameof(armiesToAttackWith));
            }

            if (targetTile is null)
            {
                throw new ArgumentNullException(nameof(targetTile));
            }

            if (Game.Current.GameState != GameState.AttackingArmy)
            {
                throw new InvalidOperationException("Cannot attack without preparing for battle.");
            }

            if (!targetTile.CanAttackHere(armiesToAttackWith))
            {
                throw new ArgumentException("Target tile must have armies of another clan.");
            }

            logger.LogInformation($"{ArmiesToString(armiesToAttackWith)} attacking {targetTile} once");

            // Attack!
            var war = Game.Current.WarStrategy;
            var attackSucceeded = war.AttackOnce(armiesToAttackWith, targetTile);
            
            if (war.BattleContinues(targetTile.MusterArmy(), armiesToAttackWith))
            {
                // Battle continues
                result = (attackSucceeded) ? AttackResult.AttackerWinsRound : AttackResult.DefenderWinsRound;
            }
            else
            {               
                // Battle is over
                Game.Current.Transition(GameState.CompletedBattle);
                if (attackSucceeded)
                {
                    targetTile.Armies = null;
                    result = AttackResult.AttackerWinsBattle;
                }
                else
                {
                    attackingFromTile.VisitingArmies = null;
                    result = AttackResult.DefenderWinBattle;
                }                
            }            
                        
            return result;
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

        public ActionState CompleteBattle(List<Army> attackingArmies, Tile targetTile, bool attackerWon)
        {
            if (Game.Current.GameState != GameState.CompletedBattle)
            {
                throw new InvalidOperationException("Cannot complete the battle in this state: " + Game.Current.GameState);
            }            

            if (attackerWon)
            {
                // Attacker won                
                targetTile.Armies = null;
                if (targetTile.HasCity())
                {
                    var player = attackingArmies[0].Player;
                    player.ClaimCity(targetTile.City);
                }
                var remainingAttackers = attackingArmies[0].Tile.VisitingArmies;
                MoveSelectedArmies(remainingAttackers, targetTile);
                Game.Current.Transition(GameState.SelectedArmy);
            }
            else
            {
                // Defender won                
                attackingArmies[0].Tile.VisitingArmies = null;
                Game.Current.Transition(GameState.Ready);
            }

            return ActionState.Succeeded;
        }
    }
}
