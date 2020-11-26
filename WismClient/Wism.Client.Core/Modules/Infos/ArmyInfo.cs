using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Wism.Client.Modules
{
    [DataContract]
    public class ArmyInfo
    {        
        private static readonly string HeroId = "Hero";
        public static readonly string FileName = "Army.json";

        [DataMember]
        public string DisplayName { get; set; } = "Army Name";

        [DataMember]
        public string ShortName { get; set; } = "x";

        private bool isSpecial = false;

        [DataMember]
        public bool IsSpecial { get; internal set; }

        [DataMember]
        internal bool CanWalk;

        [DataMember]
        internal bool CanFloat;

        [DataMember]
        internal bool CanFly;

        [DataMember]
        internal int Strength;

        [DataMember]
        internal int Moves;

        public static ArmyInfo GetHeroInfo()
        {
            return GetArmyInfo(HeroId);
        }

        public static ArmyInfo GetArmyInfo(string shortName)
        {
            ArmyInfo info = ModFactory.FindArmyInfo(shortName);
            if (info == null)
                throw new InvalidOperationException("No such type found.");

            return info;
        }
    }
}
