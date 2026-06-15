using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.AI.Tactical;
using Wism.Client.Core;
using Wism.Client.Data.Entities;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Strategic
{
    public sealed class ClassicStrategicModule : IStrategicModule, IAcceptedBidProvider
    {
        private readonly ClassicStrategicPlanner planner;
        private StrategicPlanEntity currentPlan;
        private List<IBid> acceptedBids = new List<IBid>();

        public ClassicStrategicModule()
            : this(new ClassicStrategicPlanner())
        {
        }

        public ClassicStrategicModule(ClassicStrategicPlanner planner)
        {
            this.planner = planner;
        }

        public void UpdateGoals(World world)
        {
            currentPlan = planner.Reconcile(world);
        }

        public void AllocateAssets(IEnumerable<IBid> bids)
        {
            var reservedArmies = new HashSet<Army>();
            var selectedBids = new List<IBid>();

            foreach (var bid in bids
                         .Where(bid => bid != null && bid.Armies != null && bid.Armies.Count > 0)
                         .Select(ApplyStrategicWeight)
                         .OrderByDescending(bid => bid.Utility)
                         .ThenBy(bid => bid.Armies.Min(army => army.Id)))
            {
                if (bid.Armies.Any(army => army == null || reservedArmies.Contains(army)))
                {
                    continue;
                }

                selectedBids.Add(bid);
                foreach (var army in bid.Armies)
                {
                    reservedArmies.Add(army);
                }
            }

            acceptedBids = selectedBids;
        }

        public IEnumerable<IBid> GetAcceptedBids()
        {
            return acceptedBids;
        }

        private IBid ApplyStrategicWeight(IBid bid)
        {
            if (currentPlan?.Objectives == null)
            {
                return bid;
            }

            var metadata = bid as IStrategicBidMetadata;
            var activeObjectives = currentPlan.Objectives
                .Where(objective => string.Equals(objective.Status, "Active", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var matchingObjective = activeObjectives
                .Where(objective => Matches(metadata, objective))
                .OrderByDescending(objective => objective.Priority)
                .ThenBy(objective => objective.Id)
                .FirstOrDefault();

            var assignedObjective = activeObjectives
                .Where(objective => objective.AssignedArmyIds != null &&
                                    objective.AssignedArmyIds.Any(id => bid.Armies.Any(army => army.Id == id)))
                .OrderByDescending(objective => objective.Priority)
                .ThenBy(objective => objective.Id)
                .FirstOrDefault();

            var utility = bid.Utility;
            var isConquestPosture = string.Equals(currentPlan.Posture, "Conquest", StringComparison.OrdinalIgnoreCase);
            if (matchingObjective != null)
            {
                utility += matchingObjective.Priority / 10.0;
            }

            if (assignedObjective != null)
            {
                utility += assignedObjective.Priority / 12.0;
            }

            if (metadata != null && metadata.ObjectiveKind == "Defend")
            {
                utility += isConquestPosture ? 2.0 : 8.0;
            }

            if (metadata != null && isConquestPosture)
            {
                if (metadata.ObjectiveKind == "Siege")
                {
                    utility += 12.0;
                }
                else if (metadata.ObjectiveKind == "Recover")
                {
                    utility -= 2.0;
                }
            }

            if (metadata != null &&
                metadata.ObjectiveKind == "Search" &&
                currentPlan.Posture == "OpeningExpansion" &&
                activeObjectives.Any(objective => objective.Kind == "Expand"))
            {
                utility -= 1.5;
            }

            return new WeightedBid(bid, utility);
        }

        private static bool Matches(IStrategicBidMetadata metadata, StrategicObjectiveEntity objective)
        {
            if (metadata == null || objective == null)
            {
                return false;
            }

            if (!string.Equals(metadata.ObjectiveKind, objective.Kind, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(objective.TargetCityShortName))
            {
                return string.Equals(metadata.TargetCityShortName, objective.TargetCityShortName, StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(objective.TargetLocationShortName))
            {
                return string.Equals(metadata.TargetLocationShortName, objective.TargetLocationShortName, StringComparison.OrdinalIgnoreCase);
            }

            return metadata.TargetX == objective.TargetX && metadata.TargetY == objective.TargetY;
        }

        private sealed class WeightedBid : IBid, IStrategicBidMetadata
        {
            private readonly IBid inner;
            private readonly IStrategicBidMetadata metadata;

            public WeightedBid(IBid inner, double utility)
            {
                this.inner = inner;
                metadata = inner as IStrategicBidMetadata;
                Utility = utility;
            }

            public List<Army> Armies => inner.Armies;

            public double Utility { get; }

            public ITacticalModule Module => inner.Module;

            public string ObjectiveKind => metadata?.ObjectiveKind;

            public string TargetCityShortName => metadata?.TargetCityShortName;

            public string TargetLocationShortName => metadata?.TargetLocationShortName;

            public int? TargetX => metadata?.TargetX;

            public int? TargetY => metadata?.TargetY;

            public string Reason => metadata?.Reason ?? "Strategic weight applied without bid reason.";
        }
    }
}
