using System.Runtime.Serialization;

namespace Wism.Client.Modules.Infos
{
    [DataContract]
    public class ProductionInfo
    {
        [DataMember] public string ArmyInfoName { get; set; }

        [DataMember] public int TurnsToProduce { get; set; }

        [DataMember] public int Upkeep { get; set; }

        [DataMember] public int Strength { get; set; }

        [DataMember] public int Moves { get; set; }
    }
}