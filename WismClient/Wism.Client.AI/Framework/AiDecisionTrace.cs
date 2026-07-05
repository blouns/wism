using System.Collections.Generic;
using Wism.Client.Commands;

namespace Wism.Client.AI.Framework
{
    public sealed class AiDecisionTrace
    {
        public AiDecisionTrace(
            string objectiveKind,
            string moduleName,
            double score,
            string target,
            string reason,
            IReadOnlyList<int> armyIds,
            IReadOnlyList<string> commandNames,
            string outcome = null,
            string blockingReason = null)
        {
            ObjectiveKind = objectiveKind;
            ModuleName = moduleName;
            Score = score;
            Target = target;
            Reason = reason;
            ArmyIds = armyIds ?? new int[0];
            CommandNames = commandNames ?? new string[0];
            Outcome = outcome ?? (CommandNames.Count > 0 ? "executed" : "blocked");
            BlockingReason = blockingReason;
        }

        public string ObjectiveKind { get; }

        public string ModuleName { get; }

        public double Score { get; }

        public string Target { get; }

        public string Reason { get; }

        public IReadOnlyList<int> ArmyIds { get; }

        public IReadOnlyList<string> CommandNames { get; }

        public string Outcome { get; }

        public string BlockingReason { get; }
    }
}
