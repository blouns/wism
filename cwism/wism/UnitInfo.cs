using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace BranallyGames.Wism
{
    [DataContract]
    public class UnitInfo
    {        
        private static readonly string HeroId = "Hero";
        public static readonly string FileName = "Unit.json";

        [DataMember]
        public string DisplayName { get; set; } = "Unit Name";

        [DataMember]
        public string ID { get; set; } = "x";

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
            return GetUnitInfo(HeroId);
        }

        public static UnitInfo GetUnitInfo(string id)
        {
            UnitInfo info = ModFactory.FindUnitInfo(id);
            if (info == null)
                throw new InvalidOperationException("No such type found.");

            return info;
        }
    }
}
