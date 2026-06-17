using System.Collections.Generic;
using System.Linq;
using Wism.Client.AI.InfluenceMaps;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Framework
{
    public class AiController
    {
        private readonly IStrategicModule strategicModule;
        private readonly List<ITacticalModule> tacticalModules;
        private readonly List<ITurnModule> turnModules;
        private readonly IWismLogger logger;
        private readonly ISpatialAdvisor spatialAdvisor;
        private List<AiDecisionTrace> lastDecisionTraces = new List<AiDecisionTrace>();

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
            ISpatialAdvisor spatialAdvisor = null)
        {
            this.strategicModule = strategicModule;
            this.tacticalModules = tacticalModules ?? new List<ITacticalModule>();
            this.turnModules = turnModules ?? new List<ITurnModule>();
            this.logger = logger;
            this.spatialAdvisor = spatialAdvisor ?? new ForwardFeedInfluenceMap();
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
            this.spatialAdvisor.Update();

            strategicModule.UpdateGoals(world);

            foreach (var module in turnModules)
            {
                var generated = module.GenerateCommands(world)?.ToList() ?? new List<ICommandAction>();
                LogTurnModuleCommands(module, generated);
                if (generated.Count > 0)
                {
                    commands.AddRange(generated);
                }
            }

            var bids = GetBids(world).ToList();
            LogBids("Candidate", bids);
            if (bids.Count == 0)
            {
                LogDecisionComplete(commands);
                return commands;
            }

            strategicModule.AllocateAssets(bids);

            var winningBids = ((strategicModule as IAcceptedBidProvider)?.GetAcceptedBids() ?? bids).ToList();
            LogAcceptedBids(winningBids, bids);

            foreach (var bid in winningBids)
            {
                var generated = bid.Module.GenerateCommands(bid.Armies, world)?.ToList() ?? new List<ICommandAction>();
                LogBidCommands(bid, generated);
                traces.Add(CreateTrace(bid, generated));
                if (generated.Count > 0)
                {
                    commands.AddRange(generated);
                }
            }

            lastDecisionTraces = traces;
            LogDecisionComplete(commands);
            return commands;
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
