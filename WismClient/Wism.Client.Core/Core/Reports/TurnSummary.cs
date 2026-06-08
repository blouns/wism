using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.Core.Reports
{
    /// <summary>
    ///     Summary of a player's completed turn: gold, production, and city events.
    /// </summary>
    public class TurnSummary
    {
        /// <summary>Gold earned from city income this turn (before upkeep).</summary>
        public int GoldIncome { get; set; }

        /// <summary>Gold spent on army upkeep this turn.</summary>
        public int ArmyUpkeep { get; set; }

        /// <summary>Net gold change this turn (GoldIncome - ArmyUpkeep).</summary>
        public int NetGold => this.GoldIncome - this.ArmyUpkeep;

        /// <summary>Gold the player holds at the end of this turn.</summary>
        public int GoldBalance { get; set; }

        /// <summary>Armies that completed production and are ready to deploy.</summary>
        public IReadOnlyList<ArmyInTrainingSnapshot> ArmiesProduced { get; set; }
            = new List<ArmyInTrainingSnapshot>();

        /// <summary>Armies delivered to their destination cities this turn.</summary>
        public IReadOnlyList<ArmyInTrainingSnapshot> ArmiesDelivered { get; set; }
            = new List<ArmyInTrainingSnapshot>();

        /// <summary>Cities captured by this player this turn.</summary>
        public IReadOnlyList<string> CitiesCaptured { get; set; }
            = new List<string>();

        /// <summary>Cities lost to other players this turn.</summary>
        public IReadOnlyList<string> CitiesLost { get; set; }
            = new List<string>();

        /// <summary>Turn number this summary covers.</summary>
        public int Turn { get; set; }

        /// <summary>Player clan short name.</summary>
        public string ClanName { get; set; }
    }

    /// <summary>
    ///     Immutable snapshot of an army in training at summary time.
    /// </summary>
    public class ArmyInTrainingSnapshot
    {
        public string ArmyKind { get; set; }
        public string DestinationCity { get; set; }
        public int Strength { get; set; }
        public int Moves { get; set; }
    }
}
