using System.Collections.Generic;
using System;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands;
using Wism.Client.Commands.Cities;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using System.Linq;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.Common;

public class CaptureModule : ITacticalModule, IBlockedReasonProvider
{
    private const double ImmediateCaptureUtility = 10.0;
    private const int MaxCandidateCitiesPerStack = 24;

    private readonly ArmyController armyController;
    private readonly CityController cityController;
    private readonly GarrisonPolicy garrisonPolicy;
    private readonly CityTargetEvaluator cityTargetEvaluator;
    private readonly CombatEstimator combatEstimator;
    private readonly IWismLogger logger;
    private readonly bool shouldLog;
    private readonly Dictionary<string, string> bidTargetByArmyKey = new Dictionary<string, string>();
    private readonly Dictionary<string, City> bidTargetCityByArmyKey = new Dictionary<string, City>();
    private readonly Dictionary<int, City> bidTargetCityByLeadArmyId = new Dictionary<int, City>();

    public string LastBlockingReason { get; private set; }

    public CaptureModule(ArmyController armyController, IWismLogger logger)
        : this(armyController, null, GarrisonPolicy.None, logger)
    {
    }

    public CaptureModule(ArmyController armyController, CityController cityController, IWismLogger logger)
        : this(armyController, cityController, GarrisonPolicy.None, logger)
    {
    }

    public CaptureModule(ArmyController armyController, CityController cityController, GarrisonPolicy garrisonPolicy, IWismLogger logger)
        : this(armyController, cityController, garrisonPolicy, new CityTargetEvaluator(), new CombatEstimator(), logger)
    {
    }

    public CaptureModule(
        ArmyController armyController,
        CityController cityController,
        GarrisonPolicy garrisonPolicy,
        CityTargetEvaluator cityTargetEvaluator,
        CombatEstimator combatEstimator,
        IWismLogger logger)
    {
        this.armyController = armyController;
        this.cityController = cityController;
        this.garrisonPolicy = garrisonPolicy;
        this.cityTargetEvaluator = cityTargetEvaluator;
        this.combatEstimator = combatEstimator;
        this.logger = logger;
        this.shouldLog = logger != null &&
                         !string.Equals(logger.GetType().Name, "SilentWismLogger", StringComparison.Ordinal);
    }

    public IEnumerable<IBid> GenerateBids(World world)
    {
        var bids = new List<IBid>();
        var player = Game.Current.GetCurrentPlayer();
        this.bidTargetByArmyKey.Clear();
        this.bidTargetCityByArmyKey.Clear();
        this.bidTargetCityByLeadArmyId.Clear();

        var cities = world.GetCities()
            .Where(c => c.Clan != player.Clan)
            .ToList();

        if (cities.Count == 0)
        {
            LogInformation("[Capture] No capturable cities found.");
            return bids;
        }

        if (shouldLog)
        {
            foreach (var city in cities)
            {
                LogInformation($"[Capture] Found city at ({city.Tile.X},{city.Tile.Y}) owned by {city.Clan?.ShortName ?? "Neutral"}.");
            }
        }

        var stacks = player.GetArmies()
            .Where(IsUsableArmy)
            .GroupBy(a => (a.Tile.X, a.Tile.Y));

        foreach (var group in stacks)
        {
            var stack = GetUsableSameTileArmies(this.garrisonPolicy.GetMobileArmies(group.ToList()));
            if (stack.Count == 0)
                continue;

            var leader = stack[0];

            var targetCity = FindBestCapturableCity(stack, CandidateCitiesForStack(leader.Tile, cities));
            if (targetCity == null)
            {
                if (shouldLog)
                {
                    logger.LogInformation($"[Capture] No reachable cities found for stack at ({leader.Tile.X},{leader.Tile.Y}).");
                }

                continue;
            }

            var captureArmies = SelectDirectCaptureArmies(stack, targetCity);
            var bidArmies = captureArmies.Count > 0 ? captureArmies : SelectCityPressureArmies(stack);
            var targetScore = this.cityTargetEvaluator.Score(bidArmies, targetCity);
            var utility = captureArmies.Count > 0
                ? ImmediateCaptureUtility + targetScore
                : targetScore;

            if (shouldLog)
            {
                logger.LogInformation($"[Capture] Bidding {bidArmies.Count} army/armies at ({leader.Tile.X},{leader.Tile.Y}) to target city at ({targetCity.Tile.X},{targetCity.Tile.Y}) with utility {utility:0.000}.");
            }

            RememberBidTarget(bidArmies, targetCity);

            bids.Add(new StrategicBid(
                bidArmies,
                this,
                utility,
                targetCity.Clan == null || string.Equals(targetCity.Clan.ShortName, "Neutral", StringComparison.OrdinalIgnoreCase) ? "Expand" : "Siege",
                targetCity.ShortName,
                targetX: targetCity.X,
                targetY: targetCity.Y,
                reason: captureArmies.Count > 0
                    ? $"Adjacent legal capture of {targetCity.ShortName}."
                    : $"Best reachable city pressure target {targetCity.ShortName}."));
        }

        return bids;
    }

    public IEnumerable<ICommandAction> GenerateCommands(List<Army> armies, World world)
    {
        LastBlockingReason = null;
        var commands = new List<ICommandAction>();
        if (armies == null || armies.Count == 0)
        {
            LastBlockingReason = BlockedReasonCategories.NoSelectedAssets;
            return commands;
        }

        armies = GetUsableSameTileArmies(this.garrisonPolicy.GetMobileArmies(armies));
        if (armies.Count == 0)
        {
            LastBlockingReason = BlockedReasonCategories.NoSelectedAssets;
            return commands;
        }

        // 1) Snapshot current selection
        var current = Game.Current.ArmiesSelected()
            ? Game.Current.GetSelectedArmies()
            : new List<Army>();

        var army = armies[0];
        var target = FindRememberedBidTarget(armies, world);
        if (target == null)
        {
            var capturableCities = world.GetCities()
                .Where(c => c.Clan != army.Player.Clan)
                .OrderBy(c => this.cityTargetEvaluator.GetDistanceToCity(army.Tile, c))
                .ThenByDescending(c => c.Income + c.Defense)
                .ThenBy(c => c.ShortName)
                .Take(MaxCandidateCitiesPerStack)
                .ToList();

            target = FindBestCapturableCity(armies, capturableCities);
        }
        if (target == null)
        {
            LastBlockingReason = BlockedReasonCategories.TargetInvalidated;
            return commands;
        }

        var captureArmies = SelectDirectCaptureArmies(armies, target);
        if (captureArmies.Count > 0)
        {
            if (shouldLog)
            {
                logger.LogInformation($"[Capture] Army capturing city at ({target.Tile.X},{target.Tile.Y})");
            }

            commands.Add(new CaptureCityCommand(cityController, army.Player, captureArmies, target));
            return commands;
        }

        var attackTile = FindAttackableCityTile(armies, target);

        // 2) If in range, generate full attack pipeline, then filter it
        if (attackTile != null)
        {
            var estimate = this.combatEstimator.EstimateAttack(armies, attackTile);
            var minimumWinProbability = GetMinimumCityAttackWinProbability(armies, target);
            if (estimate.DefenderCount > 0 && estimate.WinProbability < minimumWinProbability)
            {
                LastBlockingReason = BlockedReasonCategories.LowOddsCityAttack;
                if (shouldLog)
                {
                    logger.LogInformation(
                        $"[Capture] Holding before low-odds city attack at ({attackTile.X},{attackTile.Y}); win probability {estimate.WinProbability:0.000}, required {minimumWinProbability:0.000}.");
                }

                return commands;
            }

            if (shouldLog)
            {
                logger.LogInformation($"[Capture] Army attacking city tile at ({attackTile.X},{attackTile.Y})");
            }

            var raw = AiUtilities.GenerateAttackCommands(
                armyController, armies, new List<ICommandAction>(), attackTile);

            foreach (var cmd in raw)
            {
                // skip redundant select
                if (cmd is SelectArmyCommand sel
                    && sel.Armies.Count == current.Count
                    && !sel.Armies.Except(current).Any())
                {
                    LogInformation("[Capture] Skipping duplicate SelectArmyCommand");
                    continue;
                }

                commands.Add(cmd);
                if (cmd is SelectArmyCommand s)
                    current = s.Armies;
            }

            return commands;
        }

        // 3) Otherwise just move toward the city
        var attackPosition = AiUtilities.FindAttackPosition(
            target.Tile, armies, Game.Current.PathingStrategy, logger);

        if (attackPosition != null)
        {
            if (attackPosition == army.Tile)
            {
                LastBlockingReason = BlockedReasonCategories.AlreadyAtAttackPosition;
                if (shouldLog)
                {
                    logger.LogInformation($"[Capture] Stack is already at the best attack position for city at ({target.Tile.X},{target.Tile.Y}).");
                }

                return commands;
            }

            var blocker = FindEnemyBlockerOnRoute(armies, attackPosition, out var routeToAttackPosition);
            if (blocker != null)
            {
                var estimate = this.combatEstimator.EstimateAttack(armies, blocker);
                if (estimate.WinProbability >= AiDifficultyPolicy.ForCurrentPlayer().BlockerAttackWinProbability)
                {
                    if (shouldLog)
                    {
                        logger.LogInformation(
                            $"[Capture] Attacking enemy blocker at ({blocker.X},{blocker.Y}) on route to city with win probability {estimate.WinProbability:0.000}.");
                    }

                    commands.AddRange(AiUtilities.GenerateAttackCommands(
                        armyController,
                        armies,
                        new List<ICommandAction>(),
                        blocker));
                    return commands;
                }

                LastBlockingReason = BlockedReasonCategories.LowOddsBlocker;
                if (shouldLog)
                {
                    logger.LogInformation(
                        $"[Capture] Holding before low-odds blocker at ({blocker.X},{blocker.Y}); win probability {estimate.WinProbability:0.000}.");
                }

                return commands;
            }

            if (shouldLog)
            {
                logger.LogInformation($"[Capture] Army moving toward city at ({attackPosition.X},{attackPosition.Y})");
            }

            AiUtilities.GenerateMoveCommands(armyController, armies, commands, attackPosition, routeToAttackPosition, logger);
            if (commands.Count == 0)
            {
                LastBlockingReason = ClassifyRouteFailure(armies, routeToAttackPosition);
            }
        }
        else
        {
            LastBlockingReason = BlockedReasonCategories.NoRoute;
            if (shouldLog)
            {
                logger.LogWarning($"[Capture] Could not find valid attack position for city at ({target.Tile.X},{target.Tile.Y})");
            }
        }

        return commands;
    }

    private static string ClassifyRouteFailure(List<Army> armies, IList<Tile> path)
    {
        if (path == null)
        {
            return BlockedReasonCategories.NoRoute;
        }

        if (path.Count <= 1)
        {
            return BlockedReasonCategories.EmptyRoute;
        }

        var next = path[1];
        if (next != null && next.GetAllArmies().Any(army => army.Clan != armies[0].Clan))
        {
            return BlockedReasonCategories.EnemyBlocker;
        }

        if (next == null || !next.CanTraverseHere(armies))
        {
            return BlockedReasonCategories.BlockedNextStep;
        }

        var movableArmies = Game.Current.MovementCoordinator.GetArmiesWithApplicableMoves(armies, next);
        if (!Game.Current.MovementCoordinator.HasSufficientMovesAdjacentTile(movableArmies, next))
        {
            return BlockedReasonCategories.InsufficientMoves;
        }

        return BlockedReasonCategories.Unknown;
    }

    private void RememberBidTarget(List<Army> armies, City city)
    {
        if (armies != null && armies.Count > 0 && armies[0] != null && city != null)
        {
            this.bidTargetCityByLeadArmyId[armies[0].Id] = city;
        }

        var key = GetArmyKey(armies);
        if (!string.IsNullOrWhiteSpace(key) && city != null && !string.IsNullOrWhiteSpace(city.ShortName))
        {
            this.bidTargetByArmyKey[key] = city.ShortName;
            this.bidTargetCityByArmyKey[key] = city;
        }
    }

    private City FindRememberedBidTarget(List<Army> armies, World world)
    {
        if (armies == null || armies.Count == 0 || armies[0] == null)
        {
            return null;
        }

        var playerClan = armies[0].Player?.Clan;
        if (this.bidTargetCityByLeadArmyId.TryGetValue(armies[0].Id, out var leadRememberedCity) &&
            IsValidRememberedTarget(leadRememberedCity, playerClan))
        {
            return leadRememberedCity;
        }

        var key = GetArmyKey(armies);
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (this.bidTargetCityByArmyKey.TryGetValue(key, out var rememberedCity) &&
            IsValidRememberedTarget(rememberedCity, playerClan))
        {
            return rememberedCity;
        }

        if (!this.bidTargetByArmyKey.TryGetValue(key, out var cityShortName))
        {
            return null;
        }

        return world.GetCities()
            .FirstOrDefault(city =>
                IsValidRememberedTarget(city, playerClan) &&
                string.Equals(city.ShortName, cityShortName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsValidRememberedTarget(City city, Clan playerClan)
    {
        return city != null &&
               city.Tile != null &&
               city.Clan != playerClan;
    }

    private static string GetArmyKey(IEnumerable<Army> armies)
    {
        if (armies == null)
        {
            return string.Empty;
        }

        return string.Join(",", armies
            .Where(army => army != null)
            .Select(army => army.Id)
            .OrderBy(id => id));
    }

    private List<City> CandidateCitiesForStack(Tile origin, List<City> cities)
    {
        if (origin == null || cities == null || cities.Count <= MaxCandidateCitiesPerStack)
        {
            return cities ?? new List<City>();
        }

        return cities
            .Where(city => city != null && city.Tile != null)
            .OrderBy(city => this.cityTargetEvaluator.GetDistanceToCity(origin, city))
            .ThenByDescending(city => city.Income + city.Defense)
            .ThenBy(city => city.ShortName)
            .Take(MaxCandidateCitiesPerStack)
            .ToList();
    }



    private City FindBestCapturableCity(List<Army> armies, List<City> cities)
    {
        armies = GetUsableSameTileArmies(armies);
        if (armies.Count == 0)
        {
            return null;
        }

        var rankedCities = cities
            .Where(city => CanPursueCity(armies, city))
            .Select(city => new
            {
                City = city,
                Score = this.cityTargetEvaluator.Score(armies, city),
                Distance = this.cityTargetEvaluator.GetDistanceToCity(armies[0].Tile, city)
            })
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.City.ShortName);

        foreach (var candidate in rankedCities)
        {
            if (!CanQueueCityPressureCommand(armies, candidate.City))
            {
                continue;
            }

            if (shouldLog)
            {
                logger.LogInformation($"[Capture] Considering city at ({candidate.City.Tile.X},{candidate.City.Tile.Y}) owned by {candidate.City.Clan?.ShortName ?? "Neutral"} with pressure {candidate.Score:0.000}.");
            }

            return candidate.City;
        }

        return null;
    }

    private bool CanQueueCityPressureCommand(List<Army> armies, City city)
    {
        armies = GetUsableSameTileArmies(armies);
        if (armies == null || armies.Count == 0 || city == null)
        {
            return false;
        }

        if (SelectDirectCaptureArmies(armies, city).Count > 0)
        {
            return true;
        }

        var attackTile = FindAttackableCityTile(armies, city);
        if (attackTile != null)
        {
            var estimate = this.combatEstimator.EstimateAttack(armies, attackTile);
            return estimate.DefenderCount == 0 ||
                   estimate.WinProbability >= GetMinimumCityAttackWinProbability(armies, city);
        }

        var attackPosition = AiUtilities.FindAttackPosition(
            city.Tile,
            armies,
            Game.Current.PathingStrategy,
            logger);
        if (attackPosition == null || attackPosition == armies[0].Tile)
        {
            return false;
        }

        var blocker = FindEnemyBlockerOnRoute(armies, attackPosition, out var routeToAttackPosition);
        if (blocker != null)
        {
            return this.combatEstimator.EstimateAttack(armies, blocker).WinProbability >=
                   AiDifficultyPolicy.ForCurrentPlayer().BlockerAttackWinProbability;
        }

        return routeToAttackPosition != null && routeToAttackPosition.Count > 1;
    }

    private bool CanPursueCity(List<Army> armies, City city)
    {
        armies = GetUsableSameTileArmies(armies);
        if (armies == null || armies.Count == 0 || city == null)
        {
            return false;
        }

        if (SelectDirectCaptureArmies(armies, city).Count > 0)
        {
            return true;
        }

        var defenderTiles = city.GetTiles()
            .Where(tile => tile != null)
            .Where(tile => tile.MusterArmy().Any(army => army.Clan != armies[0].Clan))
            .ToList();
        if (defenderTiles.Count == 0)
        {
            return true;
        }

        var bestWinProbability = defenderTiles
            .Select(tile => this.combatEstimator.EstimateAttack(armies, tile).WinProbability)
            .DefaultIfEmpty(0.0)
            .Max();

        if (bestWinProbability >= GetMinimumCityAttackWinProbability(armies, city))
        {
            return true;
        }

        if (shouldLog)
        {
            logger.LogInformation(
                $"[Capture] Skipping defended city at ({city.Tile.X},{city.Tile.Y}); best win probability {bestWinProbability:0.000}.");
        }

        return false;
    }

    private static double GetMinimumCityAttackWinProbability(List<Army> armies, City city)
    {
        var policy = AiDifficultyPolicy.ForCurrentPlayer();
        return IsEndgameAssault(armies, city)
            ? policy.EndgameCityAttackWinProbability
            : policy.CityAttackWinProbability;
    }

    private static bool IsEndgameAssault(List<Army> armies, City city)
    {
        if (armies == null || armies.Count == 0 || city == null)
        {
            return false;
        }

        var player = armies[0].Player;
        var owner = city.Clan?.Player;
        if (player == null || owner == null || owner == player)
        {
            return false;
        }

        var ownerCityCount = owner.GetCities().Count;
        if (ownerCityCount > 1)
        {
            return false;
        }

        return player.GetCities().Count >= ownerCityCount + 2 ||
               player.GetArmies().Count >= owner.GetArmies().Count * 2;
    }

    private bool CanCaptureDirectly(List<Army> armies, City city)
    {
        return SelectDirectCaptureArmies(armies, city).Count > 0;
    }

    private List<Army> SelectDirectCaptureArmies(List<Army> armies, City city)
    {
        armies = GetUsableSameTileArmies(armies);
        if (this.cityController == null || armies == null || armies.Count == 0 || city?.Tile == null)
        {
            return new List<Army>();
        }

        var player = armies[0].Player;
        var origin = armies[0].Tile;
        if (player == null || origin == null || city.Clan == player.Clan)
        {
            return new List<Army>();
        }

        return armies
            .Where(army => army.Player == player && army.Tile == origin)
            .Where(army => city.GetTiles().Any(tile =>
                tile != null &&
                origin.IsNeighbor(tile) &&
                tile.HasRoom(1) &&
                !tile.MusterArmy().Any(a => a.Clan != player.Clan) &&
                army.MovesRemaining >= tile.Terrain.MovementCost))
            .OrderBy(GetMobilityPriority)
            .ThenBy(army => army.Id)
            .Take(1)
            .ToList();
    }

    private static List<Army> SelectCityPressureArmies(List<Army> armies)
    {
        armies = GetUsableSameTileArmies(armies);
        if (armies.Count == 0)
        {
            return new List<Army>();
        }

        var combatArmies = armies
            .Where(army => !(army is Hero))
            .OrderByDescending(army => army.Strength + army.MovesRemaining)
            .ThenBy(army => army.Id)
            .ToList();

        return combatArmies.Count > 0 ? combatArmies : armies;
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

    private Tile FindAttackableCityTile(List<Army> armies, City city)
    {
        armies = GetUsableSameTileArmies(armies);
        if (armies == null || armies.Count == 0 || city == null)
        {
            return null;
        }

        return city.GetTiles()
            .Where(tile => tile != null)
            .Where(tile => tile.CanAttackHere(armies) && AiUtilities.IsInAttackRange(armies, tile))
            .OrderBy(tile => AiUtilities.GetManhattanDistance(armies[0].Tile, tile))
            .ThenBy(tile => tile.X)
            .ThenBy(tile => tile.Y)
            .FirstOrDefault();
    }

    private Tile FindEnemyBlockerOnRoute(List<Army> armies, Tile destination, out IList<Tile> path)
    {
        path = null;
        armies = GetUsableSameTileArmies(armies);
        if (armies == null || armies.Count == 0 || destination == null)
        {
            return null;
        }

        Game.Current.PathingStrategy.FindShortestRoute(
            World.Current.Map,
            armies,
            destination,
            out path,
            out _,
            ignoreClan: false);

        if (path == null || path.Count <= 1)
        {
            return null;
        }

        var next = path[1];
        if (!next.HasArmies() || next.Armies[0].Clan == armies[0].Clan)
        {
            return null;
        }

        return AiUtilities.IsInAttackRange(armies, next)
            ? next
            : null;
    }

    private static List<Army> GetUsableSameTileArmies(IEnumerable<Army> armies)
    {
        if (armies == null)
        {
            return new List<Army>();
        }

        var usable = armies
            .Where(IsUsableArmy)
            .ToList();
        if (usable.Count == 0)
        {
            return usable;
        }

        var player = usable[0].Player;
        var tile = usable[0].Tile;
        return usable
            .Where(army => army.Player == player && army.Tile == tile)
            .ToList();
    }

    private static bool IsUsableArmy(Army army)
    {
        return army != null &&
               !army.IsDead &&
               army.Tile != null &&
               army.Player != null &&
               army.MovesRemaining > 0;
    }

    private int ManhattanDistance(Tile a, Tile b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    private void LogInformation(string message)
    {
        if (shouldLog)
        {
            logger.LogInformation(message);
        }
    }

    private void LogWarning(string message)
    {
        if (shouldLog)
        {
            logger.LogWarning(message);
        }
    }
}


