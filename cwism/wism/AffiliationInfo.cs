using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace BranallyGames.Wism
{
    [DataContract]
    public class AffiliationInfo : ICustomizable
    {
        public static readonly string FilePattern = "Affiliation_*.json";

        public string FileName { get => "Affiliation_Template.json"; }

        [DataMember]
        public string Type = "Affiliation";

        [DataMember]
        public string DisplayName { get; set; } = "Affiliation Name";

        [DataMember]
        public string Color = "(255, 0, 0)";
    }
}
