using System;
using System.Collections.Generic;
using Wism.Client.AI.Task;
using Wism.Client.Api.Commands;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Adapta.TacticalModules
{
    public class ConquerNeutralCitiesTactic : TacticalModule
    {
        public ConquerNeutralCitiesTactic(ControllerProvider provider) : base(provider)
        {
        }

        //private readonly SimplePriorityQueue<Bid> bidQueue = new SimplePriorityQueue<Bid>();

        public override Dictionary<Army, Bid> GenerateArmyBids(List<Army> myArmies, List<City> myCities,
            TargetPortfolio targets)
        {
            var armyBidMap = new Dictionary<Army, Bid>();

            // No more neutral cities to conquer?
            if (targets.NeutralCities.Count == 0)
            {
                return armyBidMap;
            }

            // TODO: Identify the highest "value" target; within reasonable window
            //       Must be achievable (cannot walk on water)

            // Pick closest neutral city for each army
            armyBidMap = this.AddBidsByArmyForNearbyCities(myArmies, targets.NeutralCities);

            return armyBidMap;
        }

        public override void AssignAssets(Bid bid)
        {
            var commandController = this.ControllerProvider.CommandController;
            var armyController = this.ControllerProvider.ArmyController;

            // Select armies
            var armies = new List<Army>(bid.Assets);
            AddSelectArmyCommand(commandController, armyController, armies);

            // If adjacent to target, attack
            var targetTile = bid.Target.Tile;
            var armyTile = armies[0].Tile;
            if (targetTile.IsNeighbor(armyTile))
            {
                AddAttackCommand(commandController, armyController, armies, targetTile);
            }
            // Otherwise move to target
            else
            {
                if (bid.PathToTarget == null)
                {
                    throw new InvalidOperationException("No path to target.");
                }

                //  1. First Pass: Attack anything in path within MovesRemaining (walk forwards in path)
                var armyClanName = armies[0].Clan.ShortName;
                var moveCoordinator = Game.Current.MovementCoordinator;
                var armiesWithApplicableMoves = moveCoordinator.GetArmiesWithApplicableMoves(armies, targetTile);
                var targetReached = false;
                // Start at 1 as Path starts at army location
                for (var i = 1; i < bid.PathToTarget.Count; i++)
                {
                    var tile = bid.PathToTarget[i];

                    // Attack if this step has an enemy army or city
                    // TODO: Consider routing around enemies since this is not the primary goal of this tactic
                    if ((tile.HasArmies() && tile.Armies[0].Clan.ShortName != armyClanName) ||
                        (tile.HasCity() && tile.City.Clan.ShortName != armyClanName))
                    {
                        // Move right before enemy
                        AddMoveCommand(commandController, armyController, armies, bid.PathToTarget[i - 1]);

                        // Attack enemy
                        AddAttackCommand(commandController, armyController, armies, tile);

                        // Cities are blocks of 4 Tiles, so dont attack twice!
                        if (tile.HasCity() && targetTile.HasCity() && tile.City == targetTile.City)
                        {
                            targetReached = true;
                            break;
                        }
                    }
                }

                //  2. Second Pass: Move as close to target as possible (walk backwards from target in path)
                if (!targetReached)
                {
                    for (var i = bid.PathToTarget.Count - 1; i >= 0; i--)
                    {
                        var tile = bid.PathToTarget[i];
                        if (tile.CanTraverseHere(armiesWithApplicableMoves))
                        {
                            AddMoveCommand(commandController, armyController, armies, tile);
                        }
                    }
                }
            }

            // Deselect the armies
            AddDeselectArmyCommand(commandController, armyController, armies);
        }

        private static void AddDeselectArmyCommand(CommandController commandController, ArmyController armyController,
            List<Army> armies)
        {
            commandController.AddCommand(new DeselectArmyCommand(armyController, armies));
        }

        private static void AddSelectArmyCommand(CommandController commandController, ArmyController armyController,
            List<Army> armies)
        {
            commandController.AddCommand(new SelectArmyCommand(armyController, armies));
        }

        private static void AddMoveCommand(CommandController commandController, ArmyController armyController,
            List<Army> armies, Tile targetTile)
        {
            commandController.AddCommand(new MoveOnceCommand(armyController, armies, targetTile.X, targetTile.Y));
        }

        private static void AddAttackCommand(CommandController commandController, ArmyController armyController,
            List<Army> armies, Tile targetTile)
        {
            commandController.AddCommand(
                new PrepareForBattleCommand(armyController, armies, targetTile.X, targetTile.Y));
            var attackCommand = new AttackOnceCommand(armyController, armies, targetTile.X, targetTile.Y);
            commandController.AddCommand(attackCommand);
            commandController.AddCommand(new CompleteBattleCommand(armyController, attackCommand));
        }

        private Dictionary<Army, Bid> AddBidsByArmyForNearbyCities(List<Army> myArmies, List<City> neutralCities)
        {
            var armyBidMap = new Dictionary<Army, Bid>();

            if (neutralCities.Count == 0 || myArmies.Count == 0)
            {
                return armyBidMap;
            }

            // Assign a bid for each army
            Bid bid = null;
            var armiesToAllocate = new List<Army>(myArmies);
            foreach (var army in armiesToAllocate)
            {
                // TODO: Only look near the army (not world) for perf
                var nearestCity = this.FindNearestNeutralCity(army, neutralCities, out var path, out var distance);
                if (nearestCity == null)
                {
                    continue;
                }

                var turnsToComplete = 0;
                if (distance > army.MovesRemaining)
                {
                    turnsToComplete = (distance - army.MovesRemaining) / army.Moves;
                }

                bid = new Bid(this)
                {
                    Assets = new List<Army> { army },
                    Target = nearestCity,
                    UtilityValue = -distance, // Inverse as closer is better
                    TurnsToComplete = turnsToComplete,
                    PathToTarget = path
                };

                armyBidMap.Add(army, bid);
            }

            return armyBidMap;
        }

        private City FindNearestNeutralCity(Army army, List<City> neutralCities, out List<Tile> path, out int distance)
        {
            path = null;
            distance = 0;

            var closestCityDistance = int.MaxValue;
            City closestCity = null;
            foreach (var city in neutralCities)
            {
                path = new List<Tile>(Game.Current.MovementCoordinator.FindPath(new List<Army> { army }, city.Tile,
                    ref distance, true));
                if (path != null &&
                    distance < closestCityDistance)
                {
                    closestCity = city;
                }
            }

            return closestCity;
        }

        //private void AddBidsByArmyCityDistance(List<Army> myArmies, TargetPortfolio targets)
        //{            
        //    foreach (var army in myArmies)
        //    {
        //        int armyX = army.X;
        //        int armyY = army.Y;
        //        foreach (var cityTO in targets.NeutralCities)
        //        {
        //            int cityX = cityTO.X;
        //            int cityY = cityTO.Y;
        //            int deltaX = (cityX - armyX);
        //            int deltaY = (cityY - armyY);
        //            // Ignore sqrt as just need sort order (perf)
        //            int distanceSqrd = (deltaX * deltaX) + (deltaY * deltaY);

        //            // Make a bid
        //            var bid = new Bid(this)
        //            {
        //                Assets = new List<Army>() { army },
        //                Target = cityTO,
        //                UtilityValue = distanceSqrd
        //            };

        //            // Priority is inversely proportionaly to distance
        //            this.bidQueue.Enqueue(bid, -distanceSqrd);
        //        }
        //    }
        //}
    }
}