using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace BranallyGames.Wism
{
    [DataContract]
    public class UnitInfo : ICustomizable
    {        
        public const char HeroSymbol = 'H';

        public string FileName { get => "Unit_Template.json"; }

        public static readonly string FilePattern = "Unit_*.json";

        [DataMember]
        public string Type = "Unit";

        [DataMember]
        public string DisplayName { get; set; } = "Unit Name";

        [DataMember]
        public char Symbol { get; set; } = 'x';

        private bool isSpecial = false;

        [DataMember]
        public bool IsSpecial { get; internal set; }

        [DataMember]
        internal bool CanWalk;

        [DataMember]
        internal bool CanFloat;

        [DataMember]
        internal bool CanFly;

        public static UnitInfo GetHeroInfo()
        {
            UnitInfo heroInfo = ModFactory.FindUnitInfo(HeroSymbol);
            if (heroInfo == null)
                throw new InvalidOperationException("No hero type found.");

            return heroInfo;
        }
    }
}
