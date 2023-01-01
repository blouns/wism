using System.Runtime.Serialization;

namespace Wism.Client.Entities
{
    [DataContract]
    public class PlayerEntity
    {
        [DataMember] public string ClanShortName { get; set; }

        [DataMember] public int Gold { get; set; }

        [DataMember] public int Turn { get; set; }

        [DataMember] public bool IsDead { get; set; }

        [DataMember] public bool IsHuman { get; set; }

        [DataMember] public string CapitolShortName { get; set; }

        [DataMember] public ArmyEntity[] Armies { get; set; }

        [DataMember] public string[] MyCitiesShortNames { get; set; }

        [DataMember] public int[] MyHeroIds { get; set; }

        [DataMember] public int LastHeroTurn { get; set; }

        [DataMember] public int NewHeroPrice { get; set; }
    }
}