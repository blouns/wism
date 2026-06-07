using System.Collections.Generic;
using System.Linq;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Tactical
{
    public class CityDefenseModule : ITacticalModule
    {
        private const int ThreatDistance = 2;
        private const int StandardThreatenedCityDefenders = 2;
        private const int HighPressureThreatenedCityDefenders = 4;
        private const int HighPressureNearbyEnemyCount = 4;
        private const double ThreatenedCityDefenseUtility = 9.5;
        private const double MinimumCounterAttackWinProbability = 0.75;

        private readonly ArmyController armyController;
        private readonly CombatEstimator combatEstimator;
        private readonly IWismLogger logger;

        public CityDefenseModule(ArmyController armyController, IWismLogger logger)
            : this(armyController, new CombatEstimator(), logger)
        {
        }

        public CityDefenseModule(
            ArmyController armyController,
            CombatEstimator combatEstimator,
            IWismLogger logger)
        {
            this.armyController = armyController;
            this.combatEstimator = combatEstimator;
            this.logger = logger;
        }

        public IEnumerable<IBid> GenerateBids(World world)
        {
            var bids = new List<IBid>();
            var player = Game.Current.GetCurrentPlayer();
            if (player == null || player.IsDead)
            {
                return bids;
            }

            var threatenedCities = player.GetCities()
                .Where(city => city != null && IsThreatened(city, player))
                .ToList();

            if (threatenedCities.Count == 0)
            {
                return bids;
            }

            foreach (var city in threatenedCities)
            {
                var defendersNeeded = GetDefendersNeeded(city, player);
                if (defendersNeeded <= 0)
                {
                    continue;
                }

                var defendingStacks = city.GetTiles()
                    .Where(tile => tile != null && tile.HasArmies())
                    .Select(tile => tile.Armies
                    .Where(army =>
                            army.Player == player &&
                            army.MovesRemaining > 0 &&
                            !army.IsDefending)
                        .ToList())
                    .Where(stack => stack.Count > 0 && !HasFavorableAdjacentCounterAttack(stack));

                foreach (var stack in defendingStacks)
                {
                    var defenseArmies = SelectDefenseArmies(stack, defendersNeeded);
                    if (defenseArmies.Count == 0)
                    {
                        continue;
                    }

                    defendersNeeded -= defenseArmies.Count;
                    logger.LogInformation(
                        $"[Defense] Bidding {defenseArmies.Count} army/armies at ({stack[0].Tile.X},{stack[0].Tile.Y}) to defend threatened city {city.ShortName}.");
                    bids.Add(new SimpleBid(defenseArmies, this, ThreatenedCityDefenseUtility));

                    if (defendersNeeded <= 0)
                    {
                        break;
                    }
                }
            }

            return bids;
        }

        public IEnumerable<ICommandAction> GenerateCommands(List<Army> armies, World world)
        {
            var commands = new List<ICommandAction>();
            if (armies == null || armies.Count == 0)
            {
                return commands;
            }

            var player = armies[0].Player;
            var city = armies[0].Tile?.City;
            if (player == null ||
                city == null ||
                city.Clan != player.Clan ||
                !IsThreatened(city, player))
            {
                return commands;
            }

            armies = armies
                .Where(army => army.Player == player && army.MovesRemaining > 0 && !army.IsDefending)
                .ToList();
            if (armies.Count == 0 || HasFavorableAdjacentCounterAttack(armies))
            {
                return commands;
            }

            armies = SelectDefenseArmies(armies, GetDefendersNeeded(city, player));
            if (armies.Count == 0)
            {
                return commands;
            }

            logger.LogInformation(
                $"[Defense] Defending threatened city {city.ShortName} with {armies.Count} army/armies at ({armies[0].Tile.X},{armies[0].Tile.Y}).");
            AiUtilities.GenerateDefendCommands(this.armyController, armies, commands);
            return commands;
        }

        private static List<Army> SelectDefenseArmies(List<Army> armies, int defendersNeeded)
        {
            if (armies == null || defendersNeeded <= 0)
            {
                return new List<Army>();
            }

            return armies
                .OrderBy(GetMobilityPriority)
                .ThenBy(army => army.Id)
                .Take(defendersNeeded)
                .ToList();
        }

        private static int GetDefendersNeeded(City city, Player player)
        {
            if (city == null || player == null)
            {
                return 0;
            }

            var defendingCount = city.MusterArmies()
                .Count(army => army.Player == player && army.IsDefending);
            var target = GetThreatenedCityDefenderTarget(city, player);
            return System.Math.Max(0, target - defendingCount);
        }

        private static int GetThreatenedCityDefenderTarget(City city, Player player)
        {
            return CountNearbyEnemies(city, player) >= HighPressureNearbyEnemyCount
                ? HighPressureThreatenedCityDefenders
                : StandardThreatenedCityDefenders;
        }

        private static int CountNearbyEnemies(City city, Player player)
        {
            var cityTiles = city.GetTiles().Where(tile => tile != null).ToList();
            return Game.Current.Players
                .Where(other => other != player && !other.IsDead)
                .SelectMany(other => other.GetArmies())
                .Where(army => army.Tile != null)
                .Count(enemy => cityTiles.Any(tile =>
                    AiUtilities.GetManhattanDistance(tile, enemy.Tile) <= ThreatDistance));
        }

        private static int GetMobilityPriority(Army army)
        {
            var priority = army.Strength + army.MovesRemaining;
            if (army is Hero)
            {
                priority += 100;
            }

            if (army.IsSpecial())
            {
                priority += 25;
            }

            return priority;
        }

        private bool HasFavorableAdjacentCounterAttack(List<Army> stack)
        {
            if (stack == null || stack.Count == 0)
            {
                return false;
            }

            var player = stack[0].Player;
            return Game.Current.Players
                .Where(other => other != player && !other.IsDead)
                .SelectMany(other => other.GetArmies())
                .Where(enemy => enemy.Tile != null)
                .Select(enemy => enemy.Tile)
                .Distinct()
                .Where(tile => tile.CanAttackHere(stack) && AiUtilities.IsInAttackRange(stack, tile))
                .Any(tile =>
                    this.combatEstimator.EstimateAttack(stack, tile).WinProbability >=
                    MinimumCounterAttackWinProbability);
        }

        private static bool IsThreatened(City city, Player player)
        {
            return CountNearbyEnemies(city, player) > 0;
        }
    }
}
