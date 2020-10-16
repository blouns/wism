using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace BranallyGames.Wism
{
    /// Terrain Modifier. Troops from the different Empires have their likes
    /// and dislikes in regard to where they prefer to fight.For example, the
    /// Elvallie like forests but don't much care for hills or marsh. The Sirians
    /// don't mind where they fight. Consult the following table for the correct
    /// modifier.
    /// These modifiers, if any, are added together. Once calculated, the AFCM is
    /// set aside for use later in the combat routine.
    ///
    ///                                         TERRAIN TYPE
    ///
    ///                                             F
    ///                                       W  S  O     P  M
    ///                                    R  A  H  R  H  L  A
    ///                                    O  T  O  E  I  A  R
    ///                                    A  E  R  S  L  I  S
    ///                                    D  R  E  T  L  N  H
    ///                                  +---------------------+
    ///                SIRIANS           : .  .  .  .  .  .  . :
    ///                STORM GIANTS      : .  .  .  . +1  . -1 :     
    ///                GREY DWARVES      : .  .  . -1 +2  . -1 :
    ///                ORCS OF KOR       : .  .  . -1  .  . +1 :
    ///                ELVALLIE          : .  .  . +1 -1  . -1 :
    ///                SELENTINES        : . +1 +1  .  .  .  . :
    ///                HORSE LORDS       :+1  .  . -1 -1 +1  . :
    ///                LORD BANE         : .  .  . -1  .  . +1 :
    ///                                  +---------------------+
    ///                                             .=NO EFFECT
    [DataContract]
    public class AffiliationTerrainModifierInfo
    {
        public static readonly string FileName = "AffiliationTerrainModifier.json";

        [DataMember]
        public string AffiliationID { get; set; } = "Unknown";

        [DataMember]
        public string TerrainID { get; set; } = "Unknown";

        [DataMember]
        public int Modifier { get; set; } = 0;
    }
}
