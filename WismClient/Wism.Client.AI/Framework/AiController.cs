using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Wism.Client.AI.InfluenceMaps;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Framework
{
    public class AiController
    {
        private const int RepeatedCommandSignatureLimit = 2;

        private readonly IStrategicModule strategicModule;
        private readonly List<ITacticalModule> tacticalModules;
        private readonly List<ITurnModule> turnModules;
        private readonly IWismLogger logger;
        private readonly ISpatialAdvisor spatialAdvisor;
        private readonly Action<string, TimeSpan> timingSink;
        private readonly Dictionary<string, int> commandSignatureCounts = new Dictionary<string, int>();
        private List<AiDecisionTrace> lastDecisionTraces = new List<AiDecisionTrace>();
        private string commandSignatureTurnContext;

        public AiController(IStrategicModule strategicModule, List<ITacticalModule> tacticalModules)
            : this(strategicModule, tacticalModules, new List<ITurnModule>(), null)
        {
        }

        public AiController(IStrategicModule strategicModule, List<ITacticalModule> tacticalModules, List<ITurnModule> turnModules)
            : this(strategicModule, tacticalModules, turnModules, null)
        {
        }

        public AiController(
            IStrategicModule strategicModule,
            List<ITacticalModule> tacticalModules,
            List<ITurnModule> turnModules,
            IWismLogger logger,
            ISpatialAdvisor spatialAdvisor = null,
            Action<string, TimeSpan> timingSink = null)
        {
            this.strategicModule = strategicModule;
            this.tacticalModules = tacticalModules ?? new List<ITacticalModule>();
            this.turnModules = turnModules ?? new List<ITurnModule>();
            this.logger = logger;
            this.spatialAdvisor = spatialAdvisor ?? new ForwardFeedInfluenceMap();
            this.timingSink = timingSink;
        }

        /// <summary>
        ///     The shared, terrain-aware spatial advisor (forward-feed influence map). It is
        ///     refreshed exactly once per AI turn in <see cref="ExecuteTurnAndReturnCommands"/>;
        ///     strategic and tactical modules read this single cached instance.
        /// </summary>
        public ISpatialAdvisor SpatialAdvisor => this.spatialAdvisor;

        public IEnumerable<IBid> GetBids(World world)
        {
            return tacticalModules.SelectMany(module => module.GenerateBids(world));
        }

        public IReadOnlyList<AiDecisionTrace> LastDecisionTraces => lastDecisionTraces;

        public List<ICommandAction> ExecuteTurnAndReturnCommands(World world)
        {
            var commands = new List<ICommandAction>();
            var traces = new List<AiDecisionTrace>();
            LogDecisionStart(world);

            // Refresh the shared spatial picture once, before any module reads it (A2). All
            // downstream strategic/tactical consumers query this single cached flood per turn.
            Measure("spatial-advisor-update", () => this.spatialAdvisor.Update());

            Measure("strategic-goal-update", () => strategicModule.UpdateGoals(world));

            foreach (var module in turnModules)
            {
                List<ICommandAction> generated = null;
                Measure("turn-module-generation", () =>
                {
                    generated = module.GenerateCommands(world)?.ToList() ?? new List<ICommandAction>();
                });
                LogTurnModuleCommands(module, generated);
                if (generated.Count > 0)
                {
                    commands.AddRange(generated);
                }
            }

            List<IBid> bids = null;
            Measure("tactical-bid-generation", () =>
            {
                bids = GetBids(world).ToList();
            });
            LogBids("Candidate", bids);
            if (bids.Count == 0)
            {
                LogDecisionComplete(commands);
                return commands;
            }

            Measure("strategic-asset-allocation", () => strategicModule.AllocateAssets(bids));

            var winningBids = ((strategicModule as IAcceptedBidProvider)?.GetAcceptedBids() ?? bids).ToList();
            LogAcceptedBids(winningBids, bids);

            var winningBidSet = new HashSet<IBid>(winningBids);
            var commandBids = winningBids
                .Concat(bids
                    .Where(bid => !winningBidSet.Contains(bid))
                    .OrderByDescending(bid => bid.Utility)
                    .ThenBy(bid => bid.Armies?.Where(army => army != null).Select(army => army.Id).DefaultIfEmpty(int.MaxValue).Min()))
                .ToList();
            var reservedArmyIds = new HashSet<int>();

            foreach (var bid in commandBids)
            {
                if (HasReservedArmy(bid, reservedArmyIds))
                {
                    continue;
                }

                List<ICommandAction> generated = null;
                var bidModuleName = bid.Module?.GetType().Name ?? "Unknown";
                if (!winningBidSet.Contains(bid))
                {
                    logger?.LogInformation(
                        $"[Adapta] Trying fallback bid module={bidModuleName} utility={bid.Utility:0.000} armies={DescribeArmies(bid.Armies)}.");
                }

                Measure(
                    "accepted-bid-command-generation",
                    () =>
                    {
                        generated = bid.Module.GenerateCommands(bid.Armies, world)?.ToList() ?? new List<ICommandAction>();
                    },
                    "accepted-bid-command-generation:" + bidModuleName);
                generated = SuppressRepeatedCommandBatch(bid, generated);
                LogBidCommands(bid, generated);
                traces.Add(CreateTrace(bid, generated));
                if (generated.Count > 0)
                {
                    commands.AddRange(generated);
                    ReserveArmies(bid, reservedArmyIds);
                }
            }

            lastDecisionTraces = traces;
            LogDecisionComplete(commands);
            return commands;
        }

        private void Measure(string name, Action action, string secondaryName = null)
        {
            if (this.timingSink == null)
            {
                action();
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                action();
            }
            finally
            {
                stopwatch.Stop();
                this.timingSink(name, stopwatch.Elapsed);
                if (!string.IsNullOrWhiteSpace(secondaryName))
                {
                    this.timingSink(secondaryName, stopwatch.Elapsed);
                }
            }
        }

        private static AiDecisionTrace CreateTrace(IBid bid, List<ICommandAction> commands)
        {
            var metadata = bid as IStrategicBidMetadata;
            return new AiDecisionTrace(
                objectiveKind: metadata?.ObjectiveKind ?? "Unknown",
                moduleName: bid?.Module?.GetType().Name ?? "Unknown",
                score: bid?.Utility ?? 0,
                target: DescribeTarget(metadata),
                reason: metadata?.Reason ?? "No strategic reason recorded.",
                armyIds: bid?.Armies?.Where(army => army != null).Select(army => army.Id).ToArray(),
                commandNames: commands?.Select(command => command.GetType().Name).ToArray());
        }

        private static string DescribeTarget(IStrategicBidMetadata metadata)
        {
            if (metadata == null)
            {
                return "none";
            }

            if (!string.IsNullOrWhiteSpace(metadata.TargetCityShortName))
            {
                return "city:" + metadata.TargetCityShortName;
            }

            if (!string.IsNullOrWhiteSpace(metadata.TargetLocationShortName))
            {
                return "location:" + metadata.TargetLocationShortName;
            }

            return metadata.TargetX.HasValue && metadata.TargetY.HasValue
                ? "tile:" + metadata.TargetX.Value + "," + metadata.TargetY.Value
                : "none";
        }

        private void LogDecisionStart(World world)
        {
            if (logger == null)
            {
                return;
            }

            var player = Game.Current?.GetCurrentPlayer();
            var selected = Game.Current != null && Game.Current.ArmiesSelected()
                ? DescribeArmies(Game.Current.GetSelectedArmies())
                : "none";
            logger.LogInformation(
                $"[Adapta] Decision start player={DescribePlayer(player)} state={Game.Current?.GameState} world={world?.Name ?? "unknown"} selected={selected}.");
        }

        private void LogTurnModuleCommands(ITurnModule module, List<ICommandAction> commands)
        {
            if (logger == null)
            {
                return;
            }

            logger.LogInformation(
                $"[Adapta] Turn module {module.GetType().Name} generated {commands.Count} command(s): {DescribeCommands(commands)}.");
        }

        private void LogBids(string label, List<IBid> bids)
        {
            if (logger == null)
            {
                return;
            }

            logger.LogInformation($"[Adapta] {label} bids: count={bids.Count}.");
            foreach (var bid in bids
                .Where(bid => bid != null)
                .OrderByDescending(bid => bid.Utility)
                .ThenBy(bid => bid.Armies?.Where(army => army != null).Select(army => army.Id).DefaultIfEmpty(int.MaxValue).Min())
                .Take(12))
            {
                logger.LogInformation(
                    $"[Adapta] {label} bid module={bid.Module?.GetType().Name ?? "Unknown"} utility={bid.Utility:0.000} armies={DescribeArmies(bid.Armies)}.");
            }

            if (bids.Count > 12)
            {
                logger.LogInformation($"[Adapta] {label} bid log truncated after 12 of {bids.Count}.");
            }
        }

        private void LogAcceptedBids(List<IBid> acceptedBids, List<IBid> candidateBids)
        {
            if (logger == null)
            {
                return;
            }

            LogBids("Accepted", acceptedBids);
            var rejected = candidateBids.Count - acceptedBids.Count;
            if (rejected > 0)
            {
                logger.LogInformation($"[Adapta] Rejected bids due to lower utility or reserved armies: count={rejected}.");
            }
        }

        private void LogBidCommands(IBid bid, List<ICommandAction> commands)
        {
            if (logger == null)
            {
                return;
            }

            var moduleName = bid?.Module?.GetType().Name ?? "Unknown";
            if (commands.Count == 0)
            {
                logger.LogInformation(
                    $"[Adapta] Accepted bid produced no executable commands module={moduleName} utility={bid?.Utility ?? 0:0.000} armies={DescribeArmies(bid?.Armies)}.");
                return;
            }

            logger.LogInformation(
                $"[Adapta] Accepted bid generated {commands.Count} command(s) module={moduleName}: {DescribeCommands(commands)}.");
        }

        private void LogDecisionComplete(List<ICommandAction> commands)
        {
            if (logger == null)
            {
                return;
            }

            logger.LogInformation($"[Adapta] Decision complete generated={commands.Count} command(s): {DescribeCommands(commands)}.");
        }

        private List<ICommandAction> SuppressRepeatedCommandBatch(IBid bid, List<ICommandAction> commands)
        {
            if (commands == null || commands.Count == 0)
            {
                return commands ?? new List<ICommandAction>();
            }

            var turnContext = CreateTurnContextKey();
            if (!string.Equals(commandSignatureTurnContext, turnContext, StringComparison.Ordinal))
            {
                commandSignatureCounts.Clear();
                commandSignatureTurnContext = turnContext;
            }

            var signature = CreateCommandBatchSignature(bid, commands);
            commandSignatureCounts.TryGetValue(signature, out var count);
            count++;
            commandSignatureCounts[signature] = count;

            if (count < RepeatedCommandSignatureLimit)
            {
                return commands;
            }

            logger?.LogWarning(
                $"[Adapta] Suppressing repeated command batch after {count} identical attempt(s): {signature}.");
            return new List<ICommandAction>();
        }

        private static string CreateTurnContextKey()
        {
            var player = Game.Current?.GetCurrentPlayer();
            return player == null
                ? "none"
                : $"{player.Clan?.ShortName ?? player.GetDisplayName()}:{player.Turn}";
        }

        private static string CreateCommandBatchSignature(IBid bid, IReadOnlyList<ICommandAction> commands)
        {
            var metadata = bid as IStrategicBidMetadata;
            var armies = DescribeArmyStateForSignature(bid?.Armies);
            var commandBatch = string.Join("|", commands.Select(DescribeCommandForSignature));
            return string.Join(
                ";",
                bid?.Module?.GetType().Name ?? "Unknown",
                metadata?.ObjectiveKind ?? "Unknown",
                metadata?.TargetCityShortName ?? string.Empty,
                metadata?.TargetLocationShortName ?? string.Empty,
                metadata?.TargetX?.ToString() ?? string.Empty,
                metadata?.TargetY?.ToString() ?? string.Empty,
                armies,
                commandBatch);
        }

        private static string DescribeCommandForSignature(ICommandAction command)
        {
            switch (command)
            {
                case MoveOnceCommand move:
                    return $"{nameof(MoveOnceCommand)}:{DescribeArmyIds(move.Armies)}->{move.X},{move.Y}";
                case AttackOnceCommand attack:
                    return $"{nameof(AttackOnceCommand)}:{DescribeArmyIds(attack.Armies)}->{attack.X},{attack.Y}";
                case ArmyCommand armyCommand:
                    return $"{command.GetType().Name}:{DescribeArmyIds(armyCommand.Armies)}";
                default:
                    return command?.GetType().Name ?? "null";
            }
        }

        private static string DescribeArmyStateForSignature(IReadOnlyCollection<Army> armies)
        {
            if (armies == null || armies.Count == 0)
            {
                return "none";
            }

            return string.Join(
                ",",
                armies
                    .Where(army => army != null)
                    .OrderBy(army => army.Id)
                    .Select(army => $"{army.Id}@{DescribeTile(army.Tile)}:{army.MovesRemaining}"));
        }

        private static string DescribeArmyIds(IReadOnlyCollection<Army> armies)
        {
            if (armies == null || armies.Count == 0)
            {
                return "none";
            }

            return string.Join(",", armies.Where(army => army != null).OrderBy(army => army.Id).Select(army => army.Id));
        }

        private static bool HasReservedArmy(IBid bid, HashSet<int> reservedArmyIds)
        {
            return bid?.Armies != null &&
                   bid.Armies.Any(army => army != null && reservedArmyIds.Contains(army.Id));
        }

        private static void ReserveArmies(IBid bid, HashSet<int> reservedArmyIds)
        {
            if (bid?.Armies == null)
            {
                return;
            }

            foreach (var army in bid.Armies.Where(army => army != null))
            {
                reservedArmyIds.Add(army.Id);
            }
        }

        private static string DescribePlayer(Player player)
        {
            return player == null
                ? "none"
                : $"{player.GetDisplayName()} turn={player.Turn} human={player.IsHuman} dead={player.IsDead}";
        }

        private static string DescribeArmies(IReadOnlyCollection<Army> armies)
        {
            if (armies == null || armies.Count == 0)
            {
                return "none";
            }

            var descriptions = armies
                .Where(army => army != null)
                .Take(4)
                .Select(army =>
                    $"{army}(id={army.Id},tile={DescribeTile(army.Tile)},moves={army.MovesRemaining},defending={army.IsDefending})")
                .ToList();

            if (armies.Count > descriptions.Count)
            {
                descriptions.Add($"+{armies.Count - descriptions.Count} more");
            }

            return string.Join("; ", descriptions);
        }

        private static string DescribeCommands(IReadOnlyCollection<ICommandAction> commands)
        {
            if (commands == null || commands.Count == 0)
            {
                return "none";
            }

            return string.Join(", ", commands.Select(command => command.GetType().Name));
        }

        private static string DescribeTile(Tile tile)
        {
            return tile == null ? "none" : $"({tile.X},{tile.Y})";
        }
    }
}
