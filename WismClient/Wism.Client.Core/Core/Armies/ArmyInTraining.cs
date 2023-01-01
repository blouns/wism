using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Core
{
    public class ArmyInTraining
    {
        public ArmyInfo ArmyInfo { get; set; }

        public int TurnsToProduce { get; set; }

        public int TurnsToDeliver { get; set; }

        public City ProductionCity { get; set; }

        public City DestinationCity { get; set; }

        public int Upkeep { get; set; }
        public int Moves { get; internal set; }
        public int Strength { get; internal set; }
        public string DisplayName { get; set; }
    }
}