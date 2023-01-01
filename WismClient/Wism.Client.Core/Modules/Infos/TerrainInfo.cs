using System.Runtime.Serialization;

namespace Wism.Client.Modules
{
    [DataContract]
    public class TerrainInfo
    {
        public static readonly string FileName = "Terrain.json";

        [DataMember] public int Movement = 99;

        [DataMember] public string DisplayName { get; set; } = "Void";

        [DataMember] public string ShortName { get; set; } = "V";

        [DataMember] public bool AllowFlight { get; set; }

        [DataMember] public bool AllowFloat { get; set; }

        [DataMember] public bool AllowWalk { get; set; }
    }
}