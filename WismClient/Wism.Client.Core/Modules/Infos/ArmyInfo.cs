using System;
using System.Runtime.Serialization;

namespace Wism.Client.Modules.Infos
{
    [DataContract]
    public class ArmyInfo
    {
        private static readonly string HeroId = "Hero";
        public static readonly string FileName = "Army.json";

        [DataMember] public bool CanFloat;

        [DataMember] public bool CanFly;

        [DataMember] public bool CanWalk;

        [DataMember] public int Moves;

        [DataMember] public int Strength;

        [DataMember] public string DisplayName { get; set; } = "Army Name";

        [DataMember] public string ShortName { get; set; } = "x";

        [DataMember] public bool IsSpecial { get; internal set; }

        public static ArmyInfo GetHeroInfo()
        {
            return GetArmyInfo(HeroId);
        }

        public static ArmyInfo GetArmyInfo(string shortName)
        {
            var info = ModFactory.FindArmyInfo(shortName);
            if (info == null)
            {
                throw new InvalidOperationException("No such type found.");
            }

            return info;
        }
    }
}