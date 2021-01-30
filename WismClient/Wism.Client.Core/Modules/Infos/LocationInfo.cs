using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Wism.Client.Modules
{
    [DataContract]
    public class LocationInfo
    {
        public static readonly string FileName = "Location.json";

        [DataMember]
        public string DisplayName { get; set; } = "Old Ruins";      

        [DataMember]
        public string ShortName { get; set; } = "OldRuins";

        [DataMember]
        public string Kind { get; set; } = "Ruins";

        [DataMember]
        public string Terrain { get; set; } = "Ruins";
    }
}
