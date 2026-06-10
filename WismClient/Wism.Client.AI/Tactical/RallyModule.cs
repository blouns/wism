using System.Collections.Generic;
using System.Linq;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Pathing;

namespace Wism.Client.AI.Tactical
{
    public class RallyModule : ITacticalModule
    {
        private const double AdjacentRallyUtility = 5.5;
        private const double TravelRallyUtility = 1.0;

        private readonly ArmyController armyController;
        private readonly IPathingStrategy pathingStrategy;
        private readonly GarrisonPolicy garrisonPolicy;
        private readonly IWismLogger logger;

        public RallyModule(
            ArmyController armyController,
            IPathingStrategy pathingStrategy,
            GarrisonPolicy garrisonPolicy,
            IWismLogger logger)
        {
            this.armyController = armyController;
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

            var stacks = GetMobileStacks(player);
            foreach (var stack in stacks)
            {
                var target = FindRallyTarget(stack, stacks);
                if (target == null)
                {
                    continue;
                }

                var distance = AiUtilities.GetManhattanDistance(stack[0].Tile, target.Tile);
                var utility = distance <= 1
                    ? AdjacentRallyUtility
                    : TravelRallyUtility / (distance + 1);

                logger.LogInformation(
                    $"[Rally] Bidding stack at ({stack[0].Tile.X},{stack[0].Tile.Y}) to rally at ({target.Tile.X},{target.Tile.Y}) with utility {utility:0.000}.");
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

            var stacks = GetMobileStacks(armies[0].Player);
            var target = FindRallyTarget(armies, stacks);
            if (target == null)
            {
                return commands;
            }

            this.pathingStrategy.FindShortestRoute(
                World.Current.Map,
                armies,
                target.Tile,
                out var path,
                out _,
                ignoreClan: false);

            if (path == null || path.Count <= 1)
            {
                return commands;
            }

            logger.LogInformation(
                $"[Rally] Moving stack at ({armies[0].Tile.X},{armies[0].Tile.Y}) toward friendly stack at ({target.Tile.X},{target.Tile.Y}) via ({path[1].X},{path[1].Y}).");
            AiUtilities.GenerateMoveCommands(this.armyController, armies, commands, target.Tile, path, this.logger);
            return commands;
        }

        private List<List<Army>> GetMobileStacks(Player player)
        {
            return player.GetArmies()
                .Where(army => army != null && army.Tile != null && army.MovesRemaining > 0)
                .GroupBy(army => army.Tile)
                .Select(group => this.garrisonPolicy.GetMobileArmies(group.ToList()))
                .Where(stack => stack.Count > 0)
                .ToList();
        }

        private RallyTarget FindRallyTarget(List<Army> armies, List<List<Army>> stacks)
        {
            if (armies == null || armies.Count == 0 || stacks == null || stacks.Count == 0)
            {
                return null;
            }

            var origin = armies[0].Tile;
            return stacks
                .Where(stack => stack.Count > armies.Count)
                .Where(stack => stack[0].Tile != origin)
                .Where(stack => stack[0].Tile.HasRoom(armies.Count))
                .Where(stack => stack[0].Tile.CanTraverseHere(armies))
                .Select(stack => new RallyTarget(
                    stack[0].Tile,
                    stack.Count,
                    AiUtilities.GetManhattanDistance(origin, stack[0].Tile)))
                .OrderByDescending(target => target.StackSize)
                .ThenBy(target => target.Distance)
                .ThenBy(target => target.Tile.X)
                .ThenBy(target => target.Tile.Y)
                .FirstOrDefault();
        }

        private class RallyTarget
        {
            public RallyTarget(Tile tile, int stackSize, int distance)
            {
                this.Tile = tile;
                this.StackSize = stackSize;
                this.Distance = distance;
            }

            public Tile Tile { get; }

            public int StackSize { get; }

            public int Distance { get; }
        }
    }
}
