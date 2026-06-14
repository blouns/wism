using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.AI.Framework;
using Wism.Client.Core;
using Wism.Client.Data.Entities;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Strategic
{
    public sealed class ClassicStrategicPlanner
    {
        private const int DefenseThreatRadius = 4;
        private const int OpeningExpansionCityCount = 4;

        public StrategicPlanEntity Reconcile(World world)
        {
            var player = Game.Current.GetCurrentPlayer();
            if (world == null || player == null || player.IsDead)
            {
                return null;
            }

            var previous = GetPlan(player.Clan.ShortName);
            var staleObjectives = MarkStale(previous, world, player);
            var activeObjectives = new List<StrategicObjectiveEntity>();
            var assignedArmyIds = new HashSet<int>();

            activeObjectives.AddRange(CreateDefenseObjectives(world, player, assignedArmyIds));
            activeObjectives.AddRange(CreateSearchObjectives(world, player, assignedArmyIds));
            activeObjectives.AddRange(CreateExpansionObjectives(world, player, assignedArmyIds));
            activeObjectives.AddRange(CreateSiegeObjectives(world, player, assignedArmyIds));
            activeObjectives.AddRange(CreateProductionObjectives(world, player));

            var objectives = staleObjectives
                .Concat(activeObjectives)
                .OrderByDescending(objective => objective.Priority)
                .ThenBy(objective => objective.Kind)
                .ThenBy(objective => objective.TargetCityShortName)
                .ThenBy(objective => objective.TargetLocationShortName)
                .ThenBy(objective => objective.TargetX ?? int.MaxValue)
                .ThenBy(objective => objective.TargetY ?? int.MaxValue)
                .ToArray();

            var plan = new StrategicPlanEntity
            {
                SchemaVersion = 1,
                ClanShortName = player.Clan.ShortName,
                Turn = player.Turn,
                Revision = (previous?.Revision ?? 0) + 1,
                Posture = SelectPosture(world, player),
                Objectives = objectives
            };

            UpsertPlan(plan);
            return plan;
        }

        public static StrategicPlanEntity GetPlan(string clanShortName)
        {
            if (!Game.IsInitialized() ||
                string.IsNullOrWhiteSpace(clanShortName) ||
                Game.Current.StrategicPlans == null)
            {
                return null;
            }

            return Game.Current.StrategicPlans
                .FirstOrDefault(plan => string.Equals(plan.ClanShortName, clanShortName, StringComparison.OrdinalIgnoreCase));
        }

        private static void UpsertPlan(StrategicPlanEntity plan)
        {
            var plans = (Game.Current.StrategicPlans ?? new StrategicPlanEntity[0])
                .Where(existing => !string.Equals(existing.ClanShortName, plan.ClanShortName, StringComparison.OrdinalIgnoreCase))
                .Concat(new[] { plan })
                .OrderBy(existing => existing.ClanShortName)
                .ToArray();

            Game.Current.StrategicPlans = plans;
        }

        private static string SelectPosture(World world, Player player)
        {
            if (player.GetCities().Count < OpeningExpansionCityCount &&
                world.GetCities().Any(city => IsNeutral(city)))
            {
                return "OpeningExpansion";
            }

            if (player.GetCities().Any(city => CountNearbyEnemyArmies(city.Tile, player, DefenseThreatRadius) > 0))
            {
                return "DefensiveRecovery";
            }

            var viableEnemies = Game.Current.Players.Count(other => other != player && !other.IsDead);
            return viableEnemies <= 1 ? "Conquest" : "BalancedPressure";
        }

        private static IEnumerable<StrategicObjectiveEntity> CreateDefenseObjectives(
            World world,
            Player player,
            HashSet<int> assignedArmyIds)
        {
            foreach (var city in player.GetCities()
                         .Where(city => city?.Tile != null)
                         .Where(city => CountNearbyEnemyArmies(city.Tile, player, DefenseThreatRadius) > 0)
                         .OrderByDescending(city => CountNearbyEnemyArmies(city.Tile, player, DefenseThreatRadius))
                         .ThenBy(city => city.ShortName))
            {
                var assigned = AssignNearestArmies(player, city.Tile, assignedArmyIds, 3);
                yield return CreateObjective(
                    "Defend",
                    priority: 100 + CountNearbyEnemyArmies(city.Tile, player, DefenseThreatRadius),
                    targetCity: city,
                    assignedArmyIds: assigned,
                    assignedCityShortNames: new[] { city.ShortName });
            }
        }

        private static IEnumerable<StrategicObjectiveEntity> CreateSearchObjectives(
            World world,
            Player player,
            HashSet<int> assignedArmyIds)
        {
            var heroes = player.GetHeros()
                .Where(hero => hero != null && !hero.IsDead && hero.Tile != null)
                .ToList();
            if (heroes.Count == 0)
            {
                yield break;
            }

            foreach (var location in world.GetLocations()
                         .Where(location => location != null && location.Tile != null && !location.Searched)
                         .OrderBy(location => heroes.Min(hero => AiUtilities.GetManhattanDistance(hero.Tile, location.Tile)))
                         .ThenBy(location => location.ShortName)
                         .Take(1))
            {
                var assigned = heroes
                    .Where(hero => !assignedArmyIds.Contains(hero.Id))
                    .OrderBy(hero => AiUtilities.GetManhattanDistance(hero.Tile, location.Tile))
                    .ThenBy(hero => hero.Id)
                    .Take(1)
                    .Select(hero =>
                    {
                        assignedArmyIds.Add(hero.Id);
                        return hero.Id;
                    })
                    .ToArray();

                yield return CreateObjective(
                    "Search",
                    priority: 70,
                    targetLocation: location,
                    assignedArmyIds: assigned);
            }
        }

        private static IEnumerable<StrategicObjectiveEntity> CreateExpansionObjectives(
            World world,
            Player player,
            HashSet<int> assignedArmyIds)
        {
            foreach (var city in world.GetCities()
                         .Where(IsNeutral)
                         .OrderBy(city => DistanceToNearestArmy(player, city.Tile))
                         .ThenByDescending(city => city.Income)
                         .ThenBy(city => city.ShortName)
                         .Take(player.GetCities().Count < OpeningExpansionCityCount ? 2 : 1))
            {
                var assigned = AssignNearestArmies(player, city.Tile, assignedArmyIds, 2);
                yield return CreateObjective(
                    "Expand",
                    priority: 60 + city.Income / 10.0,
                    targetCity: city,
                    assignedArmyIds: assigned);
            }
        }

        private static IEnumerable<StrategicObjectiveEntity> CreateSiegeObjectives(
            World world,
            Player player,
            HashSet<int> assignedArmyIds)
        {
            foreach (var city in world.GetCities()
                         .Where(city => city != null && city.Tile != null && city.Clan != null && city.Clan != player.Clan && !IsNeutral(city))
                         .OrderBy(city => DistanceToNearestArmy(player, city.Tile))
                         .ThenByDescending(city => city.Clan.Player != null && city.Clan.Player.GetCities().Count == 1)
                         .ThenByDescending(city => city.Income + city.Defense)
                         .ThenBy(city => city.ShortName)
                         .Take(2))
            {
                var assigned = AssignNearestArmies(player, city.Tile, assignedArmyIds, 4);
                yield return CreateObjective(
                    "Siege",
                    priority: 50 + city.Income / 10.0 + city.Defense / 10.0,
                    targetCity: city,
                    assignedArmyIds: assigned);
            }
        }

        private static IEnumerable<StrategicObjectiveEntity> CreateProductionObjectives(World world, Player player)
        {
            foreach (var city in player.GetCities()
                         .Where(city => city?.Barracks != null && !city.Barracks.ProducingArmy())
                         .OrderBy(city => city.ShortName))
            {
                yield return CreateObjective(
                    "Produce",
                    priority: 30,
                    targetCity: city,
                    assignedCityShortNames: new[] { city.ShortName });
            }
        }

        private static IEnumerable<StrategicObjectiveEntity> MarkStale(
            StrategicPlanEntity previous,
            World world,
            Player player)
        {
            if (previous?.Objectives == null)
            {
                return new StrategicObjectiveEntity[0];
            }

            return previous.Objectives
                .Where(objective => objective != null && string.Equals(objective.Status, "Active", StringComparison.OrdinalIgnoreCase))
                .Select(objective => StaleIfInvalid(objective, world, player))
                .Where(objective => string.Equals(objective.Status, "Stale", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private static StrategicObjectiveEntity StaleIfInvalid(StrategicObjectiveEntity objective, World world, Player player)
        {
            var staleReason = GetStaleReason(objective, world, player);
            if (staleReason == null)
            {
                return objective;
            }

            objective.Status = "Stale";
            objective.StaleReason = staleReason;
            objective.Priority = Math.Min(objective.Priority, 5);
            return objective;
        }

        private static string GetStaleReason(StrategicObjectiveEntity objective, World world, Player player)
        {
            if (objective.AssignedArmyIds != null)
            {
                var liveIds = player.GetArmies()
                    .Where(army => army != null && !army.IsDead)
                    .Select(army => army.Id)
                    .ToHashSet();
                if (objective.AssignedArmyIds.Length > 0 &&
                    objective.AssignedArmyIds.All(id => !liveIds.Contains(id)))
                {
                    return "assigned-armies-dead-or-lost";
                }
            }

            if (!string.IsNullOrWhiteSpace(objective.TargetCityShortName))
            {
                var city = world.GetCities()
                    .FirstOrDefault(candidate => string.Equals(candidate.ShortName, objective.TargetCityShortName, StringComparison.OrdinalIgnoreCase));
                if (city == null)
                {
                    return "target-city-missing";
                }

                if ((objective.Kind == "Expand" || objective.Kind == "Siege") && city.Clan == player.Clan)
                {
                    return "target-city-already-owned";
                }

                if (objective.Kind == "Defend" && city.Clan != player.Clan)
                {
                    return "defense-city-lost";
                }
            }

            if (!string.IsNullOrWhiteSpace(objective.TargetLocationShortName))
            {
                var location = world.GetLocations()
                    .FirstOrDefault(candidate => string.Equals(candidate.ShortName, objective.TargetLocationShortName, StringComparison.OrdinalIgnoreCase));
                if (location == null)
                {
                    return "target-location-missing";
                }

                if (location.Searched)
                {
                    return "target-location-searched";
                }
            }

            return null;
        }

        private static StrategicObjectiveEntity CreateObjective(
            string kind,
            double priority,
            City targetCity = null,
            Location targetLocation = null,
            int[] assignedArmyIds = null,
            string[] assignedCityShortNames = null)
        {
            var targetName = targetCity?.ShortName ?? targetLocation?.ShortName ?? "none";
            return new StrategicObjectiveEntity
            {
                Id = $"{kind}:{targetName}",
                Kind = kind,
                TargetCityShortName = targetCity?.ShortName,
                TargetLocationShortName = targetLocation?.ShortName,
                TargetX = targetCity?.X ?? targetLocation?.X,
                TargetY = targetCity?.Y ?? targetLocation?.Y,
                AssignedArmyIds = assignedArmyIds ?? new int[0],
                AssignedCityShortNames = assignedCityShortNames ?? new string[0],
                Priority = priority,
                Status = "Active",
                StaleReason = null
            };
        }

        private static int[] AssignNearestArmies(
            Player player,
            Tile target,
            HashSet<int> assignedArmyIds,
            int maxArmies)
        {
            if (target == null)
            {
                return new int[0];
            }

            return player.GetArmies()
                .Where(army => army != null && !army.IsDead && army.Tile != null && army.MovesRemaining > 0)
                .Where(army => !assignedArmyIds.Contains(army.Id))
                .OrderBy(army => AiUtilities.GetManhattanDistance(army.Tile, target))
                .ThenByDescending(army => army.Strength + army.Moves)
                .ThenBy(army => army.Id)
                .Take(maxArmies)
                .Select(army =>
                {
                    assignedArmyIds.Add(army.Id);
                    return army.Id;
                })
                .ToArray();
        }

        private static int DistanceToNearestArmy(Player player, Tile target)
        {
            if (target == null)
            {
                return int.MaxValue;
            }

            return player.GetArmies()
                .Where(army => army != null && !army.IsDead && army.Tile != null)
                .Select(army => AiUtilities.GetManhattanDistance(army.Tile, target))
                .DefaultIfEmpty(int.MaxValue)
                .Min();
        }

        private static bool IsNeutral(City city)
        {
            return city != null &&
                   city.Tile != null &&
                   (city.Clan == null || string.Equals(city.Clan.ShortName, "Neutral", StringComparison.OrdinalIgnoreCase));
        }

        private static int CountNearbyEnemyArmies(Tile tile, Player player, int radius)
        {
            if (tile == null || player == null)
            {
                return 0;
            }

            return Game.Current.Players
                .Where(other => other != player && !other.IsDead)
                .SelectMany(other => other.GetArmies())
                .Count(army => army != null &&
                               !army.IsDead &&
                               army.Tile != null &&
                               AiUtilities.GetManhattanDistance(tile, army.Tile) <= radius);
        }
    }
}
