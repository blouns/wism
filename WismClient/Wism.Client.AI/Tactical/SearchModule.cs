using System.Collections.Generic;
using System.Linq;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Heros;
using Wism.Client.Commands.Locations;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Pathing;

namespace Wism.Client.AI.Tactical
{
    public class SearchModule : ITacticalModule, IBlockedReasonProvider
    {
        private const double CurrentLocationSearchUtility = 6.0;
        private const double ImmediateLocationSearchUtility = 80.0;
        private const double HeroExplorationTravelUtility = 14.0;
        private const double TempleBlessingTravelUtility = 4.0;
        private const double OpportunisticSearchTravelUtility = 0.12;
        private const int MaxCandidateLocationsPerStack = 16;

        private readonly ArmyController armyController;
        private readonly HeroController heroController;
        private readonly LocationController locationController;
        private readonly IPathingStrategy pathingStrategy;
        private readonly GarrisonPolicy garrisonPolicy;
        private readonly bool allowTempleSearch;
        private readonly IWismLogger logger;

        public string LastBlockingReason { get; private set; }

        public SearchModule(
            ArmyController armyController,
            LocationController locationController,
            IPathingStrategy pathingStrategy,
            IWismLogger logger)
            : this(armyController, null, locationController, pathingStrategy, GarrisonPolicy.None, logger)
        {
        }

        public SearchModule(
            ArmyController armyController,
            LocationController locationController,
            IPathingStrategy pathingStrategy,
            GarrisonPolicy garrisonPolicy,
            IWismLogger logger,
            bool allowTempleSearch = true)
            : this(armyController, null, locationController, pathingStrategy, garrisonPolicy, logger, allowTempleSearch)
        {
        }

        public SearchModule(
            ArmyController armyController,
            HeroController heroController,
            LocationController locationController,
            IPathingStrategy pathingStrategy,
            GarrisonPolicy garrisonPolicy,
            IWismLogger logger,
            bool allowTempleSearch = true)
        {
            this.armyController = armyController;
            this.heroController = heroController;
            this.locationController = locationController;
            this.pathingStrategy = pathingStrategy;
            this.garrisonPolicy = garrisonPolicy;
            this.allowTempleSearch = allowTempleSearch;
            this.logger = logger;
        }

        public IEnumerable<IBid> GenerateBids(World world)
        {
            var bids = new List<IBid>();
            var player = Game.Current.GetCurrentPlayer();
            if (player == null)
            {
                return bids;
            }

            var stacks = player.GetArmies()
                .Where(army => army.MovesRemaining > 0)
                .GroupBy(army => (army.Tile.X, army.Tile.Y));

            foreach (var group in stacks)
            {
                var stack = this.garrisonPolicy.GetMobileArmies(group.ToList());
                if (stack.Count == 0)
                {
                    continue;
                }

                var pickupHero = FindItemPickupHero(stack);
                if (pickupHero != null)
                {
                    bids.Add(new StrategicBid(
                        stack,
                        this,
                        CurrentLocationSearchUtility + 3.0,
                        "Search",
                        targetX: pickupHero.Tile.X,
                        targetY: pickupHero.Tile.Y,
                        reason: $"Hero {pickupHero.ShortName} can pick up {pickupHero.Tile.Items.Count} item(s)."));
                    continue;
                }

                var currentTile = stack[0].Tile;
                if (currentTile != null && currentTile.HasLocation() && CanSearch(stack, currentTile.Location))
                {
                    var searchArmies = SelectSearchArmies(stack, currentTile.Location);
                    if (searchArmies.Count > 0)
                    {
                        var utility = IsHeroExplorationLocation(currentTile.Location)
                            ? ImmediateLocationSearchUtility
                            : CurrentLocationSearchUtility;
                        bids.Add(new StrategicBid(
                            searchArmies,
                            this,
                            utility,
                            "Search",
                            targetLocationShortName: currentTile.Location.ShortName,
                            targetX: currentTile.Location.X,
                            targetY: currentTile.Location.Y,
                            reason: $"Search current location {currentTile.Location.ShortName}."));
                        continue;
                    }
                }
            }

            var locations = world.GetLocations()
                .Where(location => location != null && location.Tile != null && !location.Searched)
                .OrderBy(location => location.ShortName)
                .ToList();

            if (locations.Count == 0)
            {
                return bids;
            }

            foreach (var group in stacks)
            {
                var stack = this.garrisonPolicy.GetMobileArmies(group.ToList());
                if (stack.Count == 0)
                {
                    continue;
                }

                if (HasAdjacentAttackableEnemy(stack))
                {
                    logger.LogInformation($"[Search] Skipping travel search for stack at ({stack[0].Tile.X},{stack[0].Tile.Y}) because an adjacent enemy is attackable.");
                    continue;
                }

                var target = FindBestSearchTarget(stack, CandidateLocationsForStack(stack, locations));
                if (target == null)
                {
                    continue;
                }

                var searchArmies = SelectSearchArmies(stack, target);
                if (searchArmies.Count == 0)
                {
                    continue;
                }

                var distance = AiUtilities.GetManhattanDistance(searchArmies[0].Tile, target.Tile);
                var utility = distance == 0
                    ? CurrentLocationSearchUtility
                    : GetTravelUtility(searchArmies, target) / (distance + 1);

                logger.LogInformation($"[Search] Bidding {searchArmies.Count} army/armies at ({searchArmies[0].Tile.X},{searchArmies[0].Tile.Y}) to search {target.ShortName} with utility {utility:0.000}.");
                bids.Add(new StrategicBid(
                    searchArmies,
                    this,
                    utility,
                    "Search",
                    targetLocationShortName: target.ShortName,
                    targetX: target.X,
                    targetY: target.Y,
                    reason: $"Best reachable search target {target.ShortName}."));
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

            armies = this.garrisonPolicy.GetMobileArmies(armies);
            if (armies.Count == 0)
            {
                LastBlockingReason = BlockedReasonCategories.NoSelectedAssets;
                return commands;
            }

            var pickupHero = FindItemPickupHero(armies);
            if (pickupHero != null)
            {
                logger.LogInformation($"[Search] Hero {pickupHero.ShortName} picking up {pickupHero.Tile.Items.Count} item(s).");
                commands.Add(new TakeItemsCommand(this.heroController, pickupHero));
                return commands;
            }

            var currentTile = armies[0].Tile;
            if (currentTile != null && currentTile.HasLocation() && CanSearch(armies, currentTile.Location))
            {
                var command = CreateSearchCommand(armies, currentTile.Location);
                if (command != null)
                {
                    logger.LogInformation($"[Search] Searching {currentTile.Location.ShortName}.");
                    commands.Add(command);
                    return commands;
                }
            }

            var locations = world.GetLocations()
                .Where(location => location != null && location.Tile != null && !location.Searched)
                .OrderBy(location => location.ShortName)
                .ToList();

            var target = FindBestSearchTarget(armies, CandidateLocationsForStack(armies, locations));
            if (target == null)
            {
                LastBlockingReason = BlockedReasonCategories.TargetInvalidated;
                return commands;
            }

            if (IsEnemyOccupied(target.Tile, armies))
            {
                if (target.Tile.CanAttackHere(armies) && AiUtilities.IsInAttackRange(armies, target.Tile))
                {
                    logger.LogInformation($"[Search] Target {target.ShortName} is occupied by an enemy; attacking blocker at ({target.X},{target.Y}).");
                    return AiUtilities.GenerateAttackCommands(armyController, armies, commands, target.Tile);
                }

                logger.LogInformation($"[Search] Target {target.ShortName} is occupied by an enemy; no search move queued.");
                LastBlockingReason = BlockedReasonCategories.EnemyBlocker;
                return commands;
            }

            if (HasAdjacentAttackableEnemy(armies))
            {
                logger.LogInformation("[Search] Adjacent enemy is attackable; no search move queued.");
                LastBlockingReason = BlockedReasonCategories.EnemyBlocker;
                return commands;
            }

            pathingStrategy.FindShortestRoute(
                World.Current.Map,
                armies,
                target.Tile,
                out var path,
                out _,
                ignoreClan: false);

            if (path != null && path.Count > 1)
            {
                logger.LogInformation($"[Search] Moving toward {target.ShortName} via ({path[1].X},{path[1].Y}).");
                AiUtilities.GenerateMoveCommands(armyController, armies, commands, target.Tile, path, logger);
            }

            if (commands.Count == 0)
            {
                LastBlockingReason = ClassifyRouteFailure(armies, path);
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

        private static List<Location> CandidateLocationsForStack(List<Army> armies, List<Location> locations)
        {
            if (armies == null || armies.Count == 0 || locations == null || locations.Count <= MaxCandidateLocationsPerStack)
            {
                return locations ?? new List<Location>();
            }

            return locations
                .Where(location => location != null && location.Tile != null)
                .OrderBy(location => AiUtilities.GetManhattanDistance(armies[0].Tile, location.Tile))
                .ThenBy(location => location.ShortName)
                .Take(MaxCandidateLocationsPerStack)
                .ToList();
        }

        private Location FindBestSearchTarget(List<Army> armies, List<Location> locations)
        {
            return locations
                .Where(location => CanSearchKindEventually(armies, location))
                .Where(location => !IsEnemyOccupied(location.Tile, armies))
                .OrderBy(location => AiUtilities.GetManhattanDistance(armies[0].Tile, location.Tile))
                .ThenBy(location => location.ShortName)
                .FirstOrDefault();
        }

        private static bool IsEnemyOccupied(Tile tile, List<Army> armies)
        {
            if (tile == null || armies == null || armies.Count == 0)
            {
                return false;
            }

            var clan = armies[0].Clan;
            return tile.GetAllArmies().Any(army => army.Clan != clan);
        }

        private static bool HasAdjacentAttackableEnemy(List<Army> armies)
        {
            if (armies == null || armies.Count == 0 || armies[0].Tile == null)
            {
                return false;
            }

            var origin = armies[0].Tile;
            var neighbors = origin.GetNineGrid();
            for (var x = 0; x <= neighbors.GetUpperBound(0); x++)
            {
                for (var y = 0; y <= neighbors.GetUpperBound(1); y++)
                {
                    var tile = neighbors[x, y];
                    if (tile == null || tile == origin || !origin.IsNeighbor(tile) || !IsEnemyOccupied(tile, armies))
                    {
                        continue;
                    }

                    if (tile.CanAttackHere(armies) && AiUtilities.IsInAttackRange(armies, tile))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CanSearch(List<Army> armies, Location location)
        {
            if (location == null || location.Searched)
            {
                return false;
            }

            switch (location.Kind)
            {
                case "Temple":
                    if (!allowTempleSearch)
                    {
                        return false;
                    }

                    return armies.Any(army => army.Tile == location.Tile && army.MovesRemaining > 0);
                case "Ruins":
                case "Tomb":
                    return armies.Any(army => army is Hero && army.Tile == location.Tile && army.MovesRemaining > 0);
                case "Sage":
                case "Library":
                    return armies.Any(army => army is Hero && army.Tile == location.Tile && army.MovesRemaining >= 4);
                default:
                    return false;
            }
        }

        private bool CanSearchKindEventually(List<Army> armies, Location location)
        {
            switch (location.Kind)
            {
                case "Temple":
                    return allowTempleSearch &&
                           (armies.Any(army => army.Tile == location.Tile) ||
                            armies.Any(army => army is Hero));
                case "Ruins":
                case "Tomb":
                case "Sage":
                case "Library":
                    return armies.Any(army => army is Hero);
                default:
                    return false;
            }
        }

        private static double GetTravelUtility(List<Army> armies, Location location)
        {
            if (armies.Any(army => army is Hero) && IsHeroExplorationLocation(location))
            {
                return HeroExplorationTravelUtility;
            }

            if (location.Kind == "Temple")
            {
                return TempleBlessingTravelUtility;
            }

            return OpportunisticSearchTravelUtility;
        }

        private Hero FindItemPickupHero(List<Army> armies)
        {
            if (this.heroController == null)
            {
                return null;
            }

            return armies
                .OfType<Hero>()
                .Where(hero => hero.MovesRemaining > 0)
                .Where(hero => hero.Tile != null && hero.Tile.HasItems())
                .OrderBy(hero => hero.ShortName)
                .FirstOrDefault();
        }

        private static bool IsHeroExplorationLocation(Location location)
        {
            switch (location.Kind)
            {
                case "Ruins":
                case "Tomb":
                case "Sage":
                case "Library":
                    return true;
                default:
                    return false;
            }
        }

        private static List<Army> SelectSearchArmies(List<Army> armies, Location location)
        {
            if (armies == null || armies.Count == 0 || location == null)
            {
                return new List<Army>();
            }

            switch (location.Kind)
            {
                case "Ruins":
                case "Tomb":
                case "Sage":
                case "Library":
                    return armies
                        .OfType<Hero>()
                        .OrderBy(hero => AiUtilities.GetManhattanDistance(hero.Tile, location.Tile))
                        .ThenBy(hero => hero.Id)
                        .Cast<Army>()
                        .Take(1)
                        .ToList();
                case "Temple":
                    return armies
                        .OrderByDescending(army => army is Hero)
                        .ThenBy(army => army.Id)
                        .Take(1)
                        .ToList();
                default:
                    return new List<Army>();
            }
        }

        private ICommandAction CreateSearchCommand(List<Army> armies, Location location)
        {
            switch (location.Kind)
            {
                case "Temple":
                    return new SearchTempleCommand(locationController, armies, location);
                case "Ruins":
                case "Tomb":
                    return new SearchRuinsCommand(locationController, armies, location);
                case "Sage":
                    return new SearchSageCommand(locationController, armies, location);
                case "Library":
                    return new SearchLibraryCommand(locationController, armies, location);
                default:
                    logger.LogWarning($"[Search] Unsupported location kind {location.Kind} at {location.ShortName}.");
                    return null;
            }
        }
    }
}
