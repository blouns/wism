using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Pathing;

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
            // Select the armies if not selected
            var current = Game.Current.ArmiesSelected()
                ? Game.Current.GetSelectedArmies()
                : new List<Army>();

            // If the sets differ, clear then re‐select
            if (!AreSameSelection(current, armies))
            {
                if (current.Any())
                {
                    commands.Add(new DeselectArmyCommand(armyController, current));
                }

                commands.Add(new SelectArmyCommand(armyController, armies));
            }

            // Set up attack sequence
            commands.Add(
                new PrepareForBattleCommand(armyController, armies, targetTile.X, targetTile.Y));

            var attack = new AttackOnceCommand(armyController, armies, targetTile.X, targetTile.Y);
            commands.Add(attack);

            commands.Add(
                new CompleteBattleCommand(armyController, attack));
            commands.Add(new DeselectArmyCommand(armyController, armies));

            return commands;
        }

        internal static IEnumerable<ICommandAction> GenerateMoveCommands(
            ArmyController armyController,
            List<Army> armies,
            List<ICommandAction> commands,
            Tile targetTile,
            IList<Tile> path = null,
            IWismLogger logger = null)
        {
            if (!CanMoveOneStepThisTurn(armies, targetTile, ref path, logger))
            {
                return commands;
            }

            var current = Game.Current.ArmiesSelected()
                ? Game.Current.GetSelectedArmies()
                : new List<Army>();

            if (!AreSameSelection(current, armies))
            {
                if (current.Any())
                {
                    commands.Add(new DeselectArmyCommand(armyController, current));
                }

                commands.Add(new SelectArmyCommand(armyController, armies));
            }

            var move = new MoveOnceCommand(armyController, armies, targetTile.X, targetTile.Y)
            {
                Path = path
            };

            commands.Add(move);
            commands.Add(new DeselectArmyCommand(armyController, armies));
            return commands;
        }

        private static bool CanMoveOneStepThisTurn(
            List<Army> armies,
            Tile targetTile,
            ref IList<Tile> path,
            IWismLogger logger)
        {
            if (armies == null || armies.Count == 0 || targetTile == null)
            {
                return false;
            }

            if (path == null)
            {
                Game.Current.PathingStrategy.FindShortestRoute(
                    World.Current.Map,
                    armies,
                    targetTile,
                    out path,
                    out _,
                    ignoreClan: false);
            }

            if (path == null || path.Count <= 1)
            {
                logger?.LogInformation("[AI] No movement command queued because no next path step is available.");
                return false;
            }

            var nextTile = path[1];
            if (!nextTile.CanTraverseHere(armies))
            {
                logger?.LogInformation(
                    $"[AI] No movement command queued because next step ({nextTile.X},{nextTile.Y}) is blocked.");
                return false;
            }

            var armiesWithApplicableMoves =
                Game.Current.MovementCoordinator.GetArmiesWithApplicableMoves(armies, nextTile);
            if (!Game.Current.MovementCoordinator.HasSufficientMovesAdjacentTile(armiesWithApplicableMoves, nextTile))
            {
                logger?.LogInformation(
                    $"[AI] No movement command queued because next step ({nextTile.X},{nextTile.Y}) costs more moves than the stack has remaining.");
                return false;
            }

            return true;
        }

        internal static IEnumerable<ICommandAction> GenerateDefendCommands(
            ArmyController armyController,
            List<Army> armies,
            List<ICommandAction> commands)
        {
            var current = Game.Current.ArmiesSelected()
                ? Game.Current.GetSelectedArmies()
                : new List<Army>();

            if (!AreSameSelection(current, armies))
            {
                if (current.Any())
                {
                    commands.Add(new DeselectArmyCommand(armyController, current));
                }

                commands.Add(new SelectArmyCommand(armyController, armies));
            }

            commands.Add(new DefendCommand(armyController, armies));
            return commands;
        }

        // Compare two lists by Army Id to avoid duplicate selects
        private static bool AreSameSelection(
            IList<Army> a, IList<Army> b)
        {
            if (a.Count != b.Count)
                return false;

            var aIds = a.Select(x => x.Id).OrderBy(id => id);
            var bIds = b.Select(x => x.Id).OrderBy(id => id);
            return aIds.SequenceEqual(bIds);
        }


        internal static void LogAttackPositionInfo(Army enemy, Tile attackPosition, IWismLogger logger)
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

        internal static Tile FindAttackPosition(
                                Tile targetTile,
                                List<Army> armies,
                                IPathingStrategy pathingStrategy,
                                IWismLogger logger)
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
