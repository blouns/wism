using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.AI.Framework;
using Wism.Client.AI.InfluenceMaps;
using Wism.Client.Core;
using Wism.Client.Data.Entities;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;
using Wism.Client.Pathing;

namespace Wism.Client.AI.Strategic
{
    public sealed class ClassicStrategicPlanner
    {
        private const int DefenseThreatRadius = 4;
        private const int OpeningExpansionCityCount = 4;
        private const double InfluenceThreatFloor = 0.02;
        private const double InfluenceThreatTensionCeiling = 0.05;
        private const int RouteDistancePathingTileLimit = 2500;

        private readonly ISpatialAdvisor spatialAdvisor;
        private readonly IPathingStrategy pathingStrategy;

        public ClassicStrategicPlanner()
            : this(null, null)
        {
        }

        public ClassicStrategicPlanner(ISpatialAdvisor spatialAdvisor)
            : this(spatialAdvisor, null)
        {
        }

        public ClassicStrategicPlanner(ISpatialAdvisor spatialAdvisor, IPathingStrategy pathingStrategy)
        {
            this.spatialAdvisor = spatialAdvisor;
            this.pathingStrategy = pathingStrategy;
        }

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
            var personality = player.Clan.Info?.Personality ?? ClanPersonalityInfo.Balanced;

            activeObjectives.AddRange(CreateDefenseObjectives(world, player, assignedArmyIds));
            activeObjectives.AddRange(CreateSearchObjectives(world, player, assignedArmyIds));
            activeObjectives.AddRange(CreateExpansionObjectives(world, player, assignedArmyIds));
            activeObjectives.AddRange(CreateSiegeObjectives(world, player, assignedArmyIds));
            activeObjectives.AddRange(CreateProductionObjectives(world, player));

            var objectives = staleObjectives
                .Concat(activeObjectives)
                .Select(objective => ApplyPersonality(objective, personality))
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
                PersonalityProfile = ResolvePersonalityProfile(personality),
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

        private string SelectPosture(World world, Player player)
        {
            if (player.GetCities().Any(city => IsCityUnderThreat(city.Tile, player)))
            {
                return "DefensiveRecovery";
            }

            if (player.GetCities().Count < OpeningExpansionCityCount &&
                world.GetCities().Any(city => IsNeutral(city)))
            {
                return "OpeningExpansion";
            }

            var viableEnemies = Game.Current.Players.Count(other => other != player && !other.IsDead);
            return viableEnemies <= 1 ? "Conquest" : "BalancedPressure";
        }

        private IEnumerable<StrategicObjectiveEntity> CreateDefenseObjectives(
            World world,
            Player player,
            HashSet<int> assignedArmyIds)
        {
            foreach (var city in player.GetCities()
                         .Where(city => city?.Tile != null)
                         .Where(city => IsCityUnderThreat(city.Tile, player))
                         .OrderByDescending(city => GetDefensePressure(city.Tile, player))
                         .ThenBy(city => city.ShortName))
            {
                var assigned = AssignNearestArmies(player, city.Tile, assignedArmyIds, 3);
                yield return CreateObjective(
                    "Defend",
                    priority: 100 + GetDefensePressure(city.Tile, player),
                    targetCity: city,
                    assignedArmyIds: assigned,
                    assignedCityShortNames: new[] { city.ShortName });
            }
        }

        private IEnumerable<StrategicObjectiveEntity> CreateSearchObjectives(
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

        private IEnumerable<StrategicObjectiveEntity> CreateExpansionObjectives(
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

        private IEnumerable<StrategicObjectiveEntity> CreateSiegeObjectives(
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

        private static StrategicObjectiveEntity ApplyPersonality(
            StrategicObjectiveEntity objective,
            ClanPersonalityInfo personality)
        {
            if (objective == null || personality == null)
            {
                return objective;
            }

            objective.Priority *= ResolveWeight(objective.Kind, personality);
            return objective;
        }

        private static double ResolveWeight(string objectiveKind, ClanPersonalityInfo personality)
        {
            double weight;
            switch (objectiveKind)
            {
                case "Defend":
                    weight = personality.Defender;
                    break;
                case "Search":
                    weight = personality.Explorer;
                    break;
                case "Expand":
                    weight = (personality.Explorer + personality.Opportunist) / 2.0;
                    break;
                case "Siege":
                    weight = (personality.Aggressive + personality.Raider) / 2.0;
                    break;
                case "Produce":
                    weight = personality.Economy;
                    break;
                default:
                    weight = 1.0;
                    break;
            }

            if (double.IsNaN(weight) || double.IsInfinity(weight))
            {
                return 1.0;
            }

            return Math.Max(0.25, Math.Min(3.0, weight));
        }

        private static string ResolvePersonalityProfile(ClanPersonalityInfo personality)
        {
            return string.IsNullOrWhiteSpace(personality.Profile)
                ? "balanced"
                : personality.Profile.Trim();
        }

        private int[] AssignNearestArmies(
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
                .OrderBy(army => EstimateRouteDistance(army, target))
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

        private int DistanceToNearestArmy(Player player, Tile target)
        {
            if (target == null)
            {
                return int.MaxValue;
            }

            return player.GetArmies()
                .Where(army => army != null && !army.IsDead && army.Tile != null)
                .Select(army => EstimateRouteDistance(army, target))
                .DefaultIfEmpty(int.MaxValue)
                .Min();
        }

        private bool IsCityUnderThreat(Tile tile, Player player)
        {
            if (tile == null)
            {
                return false;
            }

            var enemyInfluence = spatialAdvisor?.GetEnemy(tile) ?? 0.0;
            if (enemyInfluence > InfluenceThreatFloor)
            {
                return (spatialAdvisor?.GetTension(tile) ?? 0.0) <= InfluenceThreatTensionCeiling;
            }

            return CountNearbyEnemyArmies(tile, player, DefenseThreatRadius) > 0;
        }

        private double GetDefensePressure(Tile tile, Player player)
        {
            var enemyInfluence = spatialAdvisor?.GetEnemy(tile) ?? 0.0;
            if (enemyInfluence > InfluenceThreatFloor)
            {
                var tension = spatialAdvisor?.GetTension(tile) ?? 0.0;
                return 1.0 + (enemyInfluence * 10.0) + Math.Max(0.0, -tension * 10.0);
            }

            return CountNearbyEnemyArmies(tile, player, DefenseThreatRadius);
        }

        private int EstimateRouteDistance(Army army, Tile target)
        {
            if (army?.Tile == null || target == null)
            {
                return int.MaxValue;
            }

            var map = World.Current?.Map;
            if (pathingStrategy == null || map == null || IsLargeRouteEstimateMap(map))
            {
                return AiUtilities.GetManhattanDistance(army.Tile, target);
            }

            try
            {
                pathingStrategy.FindShortestRoute(
                    map,
                    new List<Army> { army },
                    target,
                    out var path,
                    out var distance,
                    ignoreClan: true);

                if (path == null || path.Count == 0 || float.IsInfinity(distance) || float.IsNaN(distance))
                {
                    return int.MaxValue;
                }

                return (int)Math.Ceiling(distance);
            }
            catch
            {
                return AiUtilities.GetManhattanDistance(army.Tile, target);
            }
        }

        private static bool IsLargeRouteEstimateMap(Tile[,] map)
        {
            return map.GetLength(0) * map.GetLength(1) > RouteDistancePathingTileLimit;
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
