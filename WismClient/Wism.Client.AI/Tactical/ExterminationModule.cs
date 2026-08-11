// File: Wism.Client.AI/Tactical/ExterminationModule.cs

using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.AI.Services;
using Wism.Client.Commands.Armies;
using Wism.Client.Controllers;
using Wism.Client.Commands;
using Wism.Client.MapObjects;
using Wism.Client.Pathing;
using Wism.Client.AI.Framework;
using System.Linq;
using Wism.Client.Common;
using Wism.Client.AI.InfluenceMaps;

namespace Wism.Client.AI.Tactical
{
    public class ExterminationModule : ITacticalModule, IBlockedReasonProvider
    {
        private const int MaxCandidateEnemyTilesPerStack = 24;
        private const double WeakPointInfluenceWeight = 0.35;

        private readonly PathfindingService pathfindingService;
        private readonly IPathingStrategy pathingStrategy;
        private readonly ArmyController armyController;
        private readonly CombatEstimator combatEstimator;
        private readonly GarrisonPolicy garrisonPolicy;
        private readonly IWismLogger logger;
        private readonly ISpatialAdvisor spatialAdvisor;

        public string LastBlockingReason { get; private set; }

        public ExterminationModule(PathfindingService pathfindingService, IPathingStrategy pathingStrategy, ArmyController armyController, IWismLogger logger)
            : this(pathfindingService, pathingStrategy, armyController, new CombatEstimator(), GarrisonPolicy.None, logger)
        {
        }

        public ExterminationModule(PathfindingService pathfindingService, IPathingStrategy pathingStrategy, ArmyController armyController, CombatEstimator combatEstimator, IWismLogger logger)
            : this(pathfindingService, pathingStrategy, armyController, combatEstimator, GarrisonPolicy.None, logger)
        {
        }

        public ExterminationModule(
            PathfindingService pathfindingService,
            IPathingStrategy pathingStrategy,
            ArmyController armyController,
            CombatEstimator combatEstimator,
            GarrisonPolicy garrisonPolicy,
            IWismLogger logger,
            ISpatialAdvisor spatialAdvisor = null)
        {
            this.pathfindingService = pathfindingService;
            this.pathingStrategy = pathingStrategy;
            this.armyController = armyController;
            this.combatEstimator = combatEstimator;
            this.garrisonPolicy = garrisonPolicy;
            this.logger = logger;
            this.spatialAdvisor = spatialAdvisor;
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
                var stackList = this.garrisonPolicy.GetMobileArmies(stack.ToList());
                if (stackList.Count == 0)
                    continue;

                var leader = stackList[0];
                var target = FindBestEnemyTarget(stackList, enemies);
                if (target != null)
                {
                    var distance = AiUtilities.GetManhattanDistance(leader.Tile, target);
                    var estimate = this.combatEstimator.EstimateAttack(stackList, target);
                    var canAttackNow = target.CanAttackHere(stackList)
                        && AiUtilities.IsInAttackRange(stackList, target);
                    if (canAttackNow && estimate.WinProbability < AiDifficultyPolicy.ForCurrentPlayer().ExterminationAttackWinProbability)
                    {
                        logger.LogInformation(
                            $"[Extermination] Suppressing low-odds immediate bid at ({target.X},{target.Y}); win probability {estimate.WinProbability:0.000}.");
                        continue;
                    }

                    var combatPressure = 0.10 + estimate.WinProbability;
                    var influence = ApplyWeakPointInfluence(target, combatPressure / (distance + 1));
                    bids.Add(new StrategicBid(
                        stackList,
                        this,
                        influence,
                        "Siege",
                        targetCityShortName: target.City?.ShortName,
                        targetX: target.X,
                        targetY: target.Y));
                }
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

            // 1) Snapshot current selection
            var current = Game.Current.ArmiesSelected()
                ? Game.Current.GetSelectedArmies()
                : new List<Army>();

            var army = armies[0];
            var enemies = AiUtilities.GetAllEnemyArmies();
            var target = FindBestEnemyTarget(armies, enemies);

            if (target == null)
            {
                LastBlockingReason = BlockedReasonCategories.TargetInvalidated;
                logger.LogInformation(
                    $"[Extermination] Army at ({army.Tile.X},{army.Tile.Y}) found no valid enemy targets this turn.");
                return commands;
            }

            // 2) If in range, generate attack, then filter
            if (target.CanAttackHere(armies) && AiUtilities.IsInAttackRange(armies, target))
            {
                var estimate = this.combatEstimator.EstimateAttack(armies, target);
                if (estimate.WinProbability < AiDifficultyPolicy.ForCurrentPlayer().ExterminationAttackWinProbability)
                {
                    LastBlockingReason = target.City == null
                        ? BlockedReasonCategories.LowOddsBlocker
                        : BlockedReasonCategories.LowOddsCityAttack;
                    logger.LogInformation(
                        $"[Extermination] Skipping low-odds attack at ({target.X},{target.Y}); win probability {estimate.WinProbability:0.000}.");
                    return commands;
                }

                logger.LogInformation(
                    $"[Extermination] Army attacking tile at ({target.X},{target.Y}) with win probability {estimate.WinProbability:0.000}.");

                var raw = AiUtilities.GenerateAttackCommands(
                    armyController, armies, new List<ICommandAction>(), target);

                foreach (var cmd in raw)
                {
                    if (cmd is SelectArmyCommand sel
                        && sel.Armies.Count == current.Count
                        && !sel.Armies.Except(current).Any())
                    {
                        logger.LogInformation("[Extermination] Skipping duplicate SelectArmyCommand");
                        continue;
                    }

                    commands.Add(cmd);
                    if (cmd is SelectArmyCommand s)
                        current = s.Armies;
                }

                return commands;
            }

            // 3) Else move toward this enemy
            var attackPosition = AiUtilities.FindAttackPosition(
                target, armies, this.pathingStrategy, this.logger);

            if (attackPosition != null)
            {
                LogAttackPositionInfo(target, attackPosition);

                pathingStrategy.FindShortestRoute(
                    World.Current.Map, armies, attackPosition,
                    out var path, out _, ignoreClan: false);

                if (path != null && path.Count > 1)
                {
                    logger.LogInformation(
                        $"[Extermination] Army moving toward ({path[1].X},{path[1].Y}) to approach target.");
                    AiUtilities.GenerateMoveCommands(
                        armyController, armies, commands, attackPosition, path, logger);
                    if (commands.Count == 0)
                    {
                        LastBlockingReason = ClassifyRouteFailure(armies, path);
                    }
                    return commands;
                }

                LastBlockingReason = path == null
                    ? BlockedReasonCategories.NoRoute
                    : BlockedReasonCategories.EmptyRoute;
            }
            else
            {
                LastBlockingReason = BlockedReasonCategories.NoRoute;
                logger.LogInformation("[Extermination] No attack position found for enemy.");
            }

            logger.LogInformation(
                $"[Extermination] Army at ({army.Tile.X},{army.Tile.Y}) found no valid moves this turn.");
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

        private Tile FindBestEnemyTarget(List<Army> armies, List<Army> enemies)
        {
            if (armies == null || armies.Count == 0 || enemies == null || enemies.Count == 0)
            {
                return null;
            }

            var leader = armies[0];
            return enemies
                .Select(enemy => enemy.Tile)
                .Where(tile => tile != null)
                .Distinct()
                .OrderBy(tile => AiUtilities.GetManhattanDistance(leader.Tile, tile))
                .ThenBy(tile => tile.X)
                .ThenBy(tile => tile.Y)
                .Take(MaxCandidateEnemyTilesPerStack)
                .Select(tile =>
                {
                    var distance = AiUtilities.GetManhattanDistance(leader.Tile, tile);
                    var estimate = this.combatEstimator.EstimateAttack(armies, tile);
                    var score = ApplyWeakPointInfluence(tile, (0.10 + estimate.WinProbability) / (distance + 1));
                    return new { Tile = tile, Distance = distance, Estimate = estimate, Score = score, EnemyInfluence = GetEnemyInfluence(tile) };
                })
                .OrderByDescending(candidate => candidate.Score)
                .ThenByDescending(candidate => candidate.Estimate.WinProbability)
                .ThenBy(candidate => candidate.EnemyInfluence)
                .ThenBy(candidate => candidate.Distance)
                .ThenBy(candidate => candidate.Tile.X)
                .ThenBy(candidate => candidate.Tile.Y)
                .Select(candidate => candidate.Tile)
                .FirstOrDefault();
        }

        private void LogAttackPositionInfo(Tile enemyTile, Tile attackPosition)
        {
            logger.LogInformation($"AttackPosition = ({attackPosition.X},{attackPosition.Y})");
            logger.LogInformation($"EnemyPosition  = ({enemyTile.X},{enemyTile.Y})");

            var dx = System.Math.Abs(attackPosition.X - enemyTile.X);
            var dy = System.Math.Abs(attackPosition.Y - enemyTile.Y);
            logger.LogInformation($"[Extermination] Distance to enemy: dx={dx}, dy={dy}, sum={dx + dy}");

            if (attackPosition.X == enemyTile.X && attackPosition.Y == enemyTile.Y)
            {
                logger.LogWarning("[Extermination] WARNING: AI is trying to move onto the enemy tile!");
            }
        }

        private double ApplyWeakPointInfluence(Tile tile, double baseScore)
        {
            var enemyInfluence = GetEnemyInfluence(tile);
            if (enemyInfluence <= 0.0)
            {
                return baseScore;
            }

            return baseScore * (1.0 + (1.0 - enemyInfluence) * WeakPointInfluenceWeight);
        }

        private double GetEnemyInfluence(Tile tile)
        {
            if (tile == null || spatialAdvisor == null)
            {
                return 0.0;
            }

            var enemyInfluence = spatialAdvisor.GetEnemy(tile);
            if (double.IsNaN(enemyInfluence) || double.IsInfinity(enemyInfluence))
            {
                return 0.0;
            }

            return System.Math.Max(0.0, System.Math.Min(1.0, enemyInfluence));
        }

    }
}
