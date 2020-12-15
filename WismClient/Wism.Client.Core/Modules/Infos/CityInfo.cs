using System.Runtime.Serialization;

namespace Wism.Client.Modules
{
    [DataContract]
    public class CityInfo
    {
        public static readonly string FileName = "City.json";

        [DataMember]
        public string DisplayName { get; set; } = "City";

        [DataMember]
        public string ShortName { get; set; } = "C";

        [DataMember]
        public int Defense { get; set; }

        [DataMember]
        public int Income { get; set; }

        [DataMember]
        public ProductionInfo[] ProductionInfos { get; set; }
    }
}
