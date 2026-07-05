using System.Runtime.Serialization;

namespace Wism.Client.Data.Entities
{
    [DataContract]
    public sealed class StrategicObjectiveEntity
    {
        [DataMember] public string Id { get; set; }

        [DataMember] public string Kind { get; set; }

        [DataMember] public string TargetCityShortName { get; set; }

        [DataMember] public string TargetLocationShortName { get; set; }

        [DataMember] public int? TargetX { get; set; }

        [DataMember] public int? TargetY { get; set; }

        [DataMember] public int[] AssignedArmyIds { get; set; }

        [DataMember] public string[] AssignedCityShortNames { get; set; }

        [DataMember] public double Priority { get; set; }

        [DataMember] public string Status { get; set; }

        [DataMember] public string StaleReason { get; set; }

        [DataMember] public string GoalId { get; set; }

        [DataMember] public string State { get; set; }

        [DataMember] public int CreatedTurn { get; set; }

        [DataMember] public int UpdatedTurn { get; set; }

        [DataMember] public string Reason { get; set; }

        [DataMember] public string BlockingReason { get; set; }

        [DataMember] public string ParentGoalId { get; set; }
    }
}
