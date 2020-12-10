using System.Runtime.Serialization;

namespace Wism.Client.Modules
{
    [DataContract]
    public class ProductionInfo
    {
        [DataMember]
        public string ArmyInfoName { get; set; }

        [DataMember]
        public int TurnsToProduce { get; set; }

        [DataMember]
        public int Upkeep { get; set; }

        [DataMember]
        public int StrengthModifier { get; set; }

        [DataMember]
        public int MovesModifier { get; set; }

    }
}
