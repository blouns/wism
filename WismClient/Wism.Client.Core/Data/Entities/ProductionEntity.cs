using System.Runtime.Serialization;

namespace Wism.Client.Data.Entities
{
    [DataContract]
    public class ProductionEntity
    {
        [DataMember] public string[] ArmyNames { get; set; }

        [DataMember] public int[] ProductionNumbers { get; set; }
    }
}