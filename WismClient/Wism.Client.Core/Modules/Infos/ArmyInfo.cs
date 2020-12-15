using System;
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

        [DataMember]
        public bool IsSpecial { get; internal set; }

        [DataMember]
        public bool CanWalk;

        [DataMember]
        public bool CanFloat;

        [DataMember]
        public bool CanFly;

        [DataMember]
        public int Strength;

        [DataMember]
        public int Moves;

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
