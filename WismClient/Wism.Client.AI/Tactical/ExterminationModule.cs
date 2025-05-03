// File: Wism.Client.AI/Tactical/ExterminationModule.cs

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Wism.Client.Core;
using Wism.Client.AI.Services;
using Wism.Client.Commands.Armies;
using Wism.Client.Controllers;
using Wism.Client.Commands;
using Wism.Client.MapObjects;
using Wism.Client.Pathing;
using System;
using Wism.Client.AI.Framework;
using System.Linq;

namespace Wism.Client.AI.Tactical
{
    public class ExterminationModule : ITacticalModule
    {
        private readonly PathfindingService pathfindingService;
        private readonly IPathingStrategy pathingStrategy;
        private readonly ArmyController armyController;
        private readonly ILogger<ExterminationModule> logger;

        public ExterminationModule(PathfindingService pathfindingService, IPathingStrategy pathingStrategy, ArmyController armyController, ILogger<ExterminationModule> logger)
        {
            this.pathfindingService = pathfindingService;
            this.pathingStrategy = pathingStrategy;
            this.armyController = armyController;
            this.logger = logger;
        }

        public IEnumerable<IBid> GenerateBids(World world)
        {
            var bids = new List<IBid>();
            var currentPlayer = Game.Current.GetCurrentPlayer();
            var enemies = AiUtilities.GetAllEnemyArmies();

            // Group armies into stacks by tile
            var stacks = currentPlayer.GetArmies()
                .Where(a => a.MovesRemaining > 0)
                .GroupBy(a => (a.Tile.X, a.Tile.Y));

            foreach (var stack in stacks)
            {
                var stackList = stack.ToList();
                if (stackList.Count == 0)
                    continue;

                var leader = stackList[0];
                var closestEnemyTile = pathfindingService.FindClosestEnemyTile(leader, enemies, true);
                if (closestEnemyTile != null)
                {
                    double influence = 1.0 / (AiUtilities.GetManhattanDistance(leader.Tile, closestEnemyTile) + 1);
                    bids.Add(new SimpleBid(stackList, this, influence));
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

            var army = armies[0];
            var enemies = AiUtilities.GetAllEnemyArmies();

            foreach (var enemy in enemies)
            {
                if (enemy.Tile.CanAttackHere(armies) && AiUtilities.IsInAttackRange(armies, enemy.Tile))
                {
                    logger.LogInformation($"Army attacking tile at ({enemy.Tile.X},{enemy.Tile.Y}).");
                    return AiUtilities.GenerateAttackCommands(armyController, armies, commands, enemy.Tile);
                }

                var attackPosition = AiUtilities.FindAttackPosition(enemy.Tile, armies, this.pathingStrategy, this.logger);
                if (attackPosition != null)
                {
                    AiUtilities.LogAttackPositionInfo(enemy, attackPosition, this.logger);

                    pathingStrategy.FindShortestRoute(World.Current.Map, armies, attackPosition, out var path, out _, ignoreClan: false);

                    if (path != null && path.Count > 1)
                    {
                        logger.LogInformation($"Army moving toward ({path[1].X},{path[1].Y}) to approach target.");
                        commands.Add(new MoveOnceCommand(armyController, armies, path[1].X, path[1].Y));
                        return commands;
                    }
                }
                else
                {
                    logger.LogInformation("[Extermination] No attack position found for enemy.");
                }
            }

            logger.LogInformation($"Army at ({army.Tile.X},{army.Tile.Y}) found no valid moves this turn.");
            return commands;
        }

        
    }
}
