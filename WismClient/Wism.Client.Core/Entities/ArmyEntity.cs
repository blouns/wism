using System.Runtime.Serialization;

namespace Wism.Client.Entities
{
    [DataContract]
    public class ArmyEntity
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public int X { get; set; }

        [DataMember]
        public int Y { get; set; }

        [DataMember]
        public string ArmyShortName { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public bool IsHero { get; set; }

        [DataMember]
        public int Upkeep { get; set; }

        [DataMember]
        public int Strength { get; set; }

        [DataMember]
        public int MovesRemaining { get; set; }

        [DataMember]
        public bool IsDead { get; set; }

        [DataMember]
        public int Moves { get; internal set; }

        [DataMember]
        public string[] BlessedAtShortNames { get; set; }

        [DataMember]
        public ArtifactEntity[] Artifacts { get; set; }
    }
}