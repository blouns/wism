using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace wism
{
    [DataContract]
    public class AffiliationInfo : ICustomizable
    {
        public const string FilePattern = "Affiliation_*.json";

        public string FileName { get => "Affiliation_Template.json"; }

        [DataMember]
        public string Type = "Affiliation";

        [DataMember]
        public string DisplayName { get;  set; } = "Display Name"; 

        [DataMember]
        private string imageFileName = "image.jpg";

        [DataMember]
        public string Color = "(255, 0, 0)";
    }
}
