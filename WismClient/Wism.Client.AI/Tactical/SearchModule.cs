using System.Collections.Generic;
using System.Linq;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Locations;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Pathing;

namespace Wism.Client.AI.Tactical
{
    public class SearchModule : ITacticalModule
    {
        private const double CurrentLocationSearchUtility = 6.0;
        private const double HeroExplorationTravelUtility = 4.0;
        private const double TempleBlessingTravelUtility = 1.5;
        private const double OpportunisticSearchTravelUtility = 0.12;

        private readonly ArmyController armyController;
        private readonly LocationController locationController;
        private readonly IPathingStrategy pathingStrategy;
        private readonly GarrisonPolicy garrisonPolicy;
        private readonly IWismLogger logger;

        public SearchModule(
            ArmyController armyController,
            LocationController locationController,
            IPathingStrategy pathingStrategy,
            IWismLogger logger)
            : this(armyController, locationController, pathingStrategy, GarrisonPolicy.None, logger)
        {
        }

        public SearchModule(
            ArmyController armyController,
            LocationController locationController,
            IPathingStrategy pathingStrategy,
            GarrisonPolicy garrisonPolicy,
            IWismLogger logger)
        {
            this.armyController = armyController;
            this.locationController = locationController;
            this.pathingStrategy = pathingStrategy;
            this.garrisonPolicy = garrisonPolicy;
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

            var locations = world.GetLocations()
                .Where(location => location != null && location.Tile != null && !location.Searched)
                .OrderBy(location => location.ShortName)
                .ToList();

            if (locations.Count == 0)
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

                var target = FindBestSearchTarget(stack, locations);
                if (target == null)
                {
                    continue;
                }

                var distance = AiUtilities.GetManhattanDistance(stack[0].Tile, target.Tile);
                var utility = distance == 0
                    ? CurrentLocationSearchUtility
                    : GetTravelUtility(stack, target) / (distance + 1);

                logger.LogInformation($"[Search] Bidding stack at ({stack[0].Tile.X},{stack[0].Tile.Y}) to search {target.ShortName} with utility {utility:0.000}.");
                bids.Add(new SimpleBid(stack, this, utility));
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

            armies = this.garrisonPolicy.GetMobileArmies(armies);
            if (armies.Count == 0)
            {
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

            var target = FindBestSearchTarget(armies, locations);
            if (target == null)
            {
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
                AiUtilities.GenerateMoveCommands(armyController, armies, commands, target.Tile, path);
            }

            return commands;
        }

        private Location FindBestSearchTarget(List<Army> armies, List<Location> locations)
        {
            return locations
                .Where(location => CanSearchKindEventually(armies, location))
                .OrderBy(location => AiUtilities.GetManhattanDistance(armies[0].Tile, location.Tile))
                .ThenBy(location => location.ShortName)
                .FirstOrDefault();
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
                    return armies.Any();
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
