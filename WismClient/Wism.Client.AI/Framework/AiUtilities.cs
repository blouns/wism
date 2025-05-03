using System;
using System.Collections.Generic;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Pathing;
using Microsoft.Extensions.Logging;

namespace Wism.Client.AI.Framework
{
    internal static class AiUtilities
    {
        internal static IEnumerable<ICommandAction> GenerateAttackCommands(
            ArmyController armyController,
            List<Army> armies,
            List<ICommandAction> commands,
            Tile targetTile)
        {
            var select = new SelectArmyCommand(armyController, armies);
            var prepare = new PrepareForBattleCommand(armyController, armies, targetTile.X, targetTile.Y);
            var attack = new AttackOnceCommand(armyController, armies, targetTile.X, targetTile.Y);
            var complete = new CompleteBattleCommand(armyController, attack);
            var deselect = new DeselectArmyCommand(armyController, armies);

            commands.Add(select);
            commands.Add(prepare);
            commands.Add(attack);
            commands.Add(complete);
            commands.Add(deselect);

            return commands;
        }


        internal static void LogAttackPositionInfo<T>(Army enemy, Tile attackPosition, ILogger<T> logger)
        {
            logger.LogInformation($"AttackPosition = ({attackPosition.X},{attackPosition.Y})");
            logger.LogInformation($"EnemyPosition  = ({enemy.Tile.X},{enemy.Tile.Y})");

            int dx = Math.Abs(attackPosition.X - enemy.Tile.X);
            int dy = Math.Abs(attackPosition.Y - enemy.Tile.Y);
            logger.LogInformation($"[Extermination] Distance to enemy: dx={dx}, dy={dy}, sum={dx + dy}");

            if (attackPosition.X == enemy.Tile.X && attackPosition.Y == enemy.Tile.Y)
            {
                logger.LogWarning("[Extermination] WARNING: AI is trying to move onto the enemy tile!");
            }
        }

        internal static Tile FindAttackPosition<T>(
                                Tile targetTile,
                                List<Army> armies,
                                IPathingStrategy pathingStrategy,
                                ILogger<T> logger)
        {
            float distance;
            pathingStrategy.FindShortestRoute(World.Current.Map, armies, targetTile, out var path, out distance, ignoreClan: true);

            if (path == null || path.Count == 0)
            {
                logger.LogWarning($"No path found to target at ({targetTile.X},{targetTile.Y})");
                return null;
            }

            for (int i = path.Count - 2; i >= 0; i--)
            {
                var tile = path[i];
                if (tile.CanTraverseHere(armies, ignoreClan: false))
                {
                    return tile;
                }
            }

            return null;
        }



        internal static bool IsInAttackRange(List<Army> armies, Tile enemyTile)
        {
            foreach (var army in armies)
            {
                if (IsAdjacent(army.Tile, enemyTile))
                {
                    return true;
                }

                if (enemyTile.City != null && IsAdjacent(army.Tile, enemyTile))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsAdjacent(Tile a, Tile b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            return dx <= 1 && dy <= 1 && !(dx == 0 && dy == 0);
        }


        internal static List<Army> GetAllEnemyArmies()
        {
            var enemies = new List<Army>();
            var currentPlayer = Game.Current.GetCurrentPlayer();

            foreach (var player in Game.Current.Players)
            {
                if (player != currentPlayer)
                {
                    enemies.AddRange(player.GetArmies());
                }
            }

            return enemies;
        }

        internal static int GetManhattanDistance(Tile tile1, Tile tile2)
        {
            return Math.Abs(tile1.X - tile2.X) + Math.Abs(tile1.Y - tile2.Y);
        }
    }
}