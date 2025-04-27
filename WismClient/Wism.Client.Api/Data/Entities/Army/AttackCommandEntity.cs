namespace Wism.Client.Data.Entities.Army
{
    public class AttackCommandEntity : ArmyCommandEntity
    {
        public int[] DefendingArmyIds { get; set; }

        public int[] OriginalAttackingArmyIds { get; set; }

        public int[] OriginalDefendingArmyIds { get; set; }
    }
}