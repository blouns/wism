using System.Runtime.Serialization;

namespace Wism.Client.Data.Entities
{
    [DataContract]
    public sealed class StrategicPlanEntity
    {
        [DataMember] public int SchemaVersion { get; set; } = 1;

        [DataMember] public string ClanShortName { get; set; }

        [DataMember] public int Turn { get; set; }

        [DataMember] public int Revision { get; set; }

        [DataMember] public string Posture { get; set; }

        [DataMember] public string PersonalityProfile { get; set; }

        [DataMember] public StrategicObjectiveEntity[] Objectives { get; set; }
    }
}
