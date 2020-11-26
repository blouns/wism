using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Wism.Client.Modules
{
    [DataContract]
    public class TerrainInfo
    {
        public static readonly string FileName = "Terrain.json";

        [DataMember]
        public string DisplayName { get; set; } = "Void";      

        [DataMember]
        public string ShortName { get; set; } = "V";

        [DataMember]
        public bool AllowFlight { get; set; } = false;

        [DataMember]
        public bool AllowFloat { get; set; } = false;

        [DataMember]
        public bool AllowWalk { get; set; } = false;

        [DataMember]
        public int Movement = 99;
    }
}
