using System.Runtime.Serialization;

namespace Wism.Client.Modules
{
    [DataContract]
    public class CityInfo
    {
        public static readonly string FileName = "City.json";

        [DataMember] public string DisplayName { get; set; }

        [DataMember] public string ShortName { get; set; }

        [DataMember] public int X { get; set; }

        [DataMember] public int Y { get; set; }

        [DataMember] public int Defense { get; set; }

        [DataMember] public int Income { get; set; }

        [DataMember] public string ClanName { get; set; }

        [DataMember] public ProductionInfo[] ProductionInfos { get; set; }
    }
}