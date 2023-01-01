using System.Runtime.Serialization;

namespace Wism.Client.Entities
{
    [DataContract]
    public class ArmyInTrainingEntity
    {
        [DataMember] public string ArmyShortName { get; set; }

        [DataMember] public int TurnsToProduce { get; set; }

        [DataMember] public int TurnsToDeliver { get; set; }

        [DataMember] public string ProductionCityShortName { get; set; }

        [DataMember] public string DestinationCityShortName { get; set; }

        [DataMember] public int Upkeep { get; set; }

        [DataMember] public int Moves { get; set; }

        [DataMember] public int Strength { get; set; }

        [DataMember] public string DisplayName { get; set; }
    }
}