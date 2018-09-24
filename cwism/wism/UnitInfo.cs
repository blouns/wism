using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace wism
{
    [DataContract]
    public class UnitInfo : ICustomizable
    {
        public string FileName { get => "Unit_Template.json"; }

        public static readonly string FilePattern = "Unit_*.json";

        [DataMember]
        public string Type = "Unit";

        [DataMember]
        public string DisplayName { get; set; } = "Unit Name";

        [DataMember]
        public char Symbol { get; set; } = 'x';

        [DataMember]
        private string ImageFileName = "image.jpg";

        [DataMember]
        internal bool CanWalk;

        [DataMember]
        internal bool CanFloat;

        [DataMember]
        internal bool CanFly;

    }
}
