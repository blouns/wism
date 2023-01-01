namespace Wism.Client.Data.Entities.Army
{
    public class CompleteBattleCommandEntity : ArmyCommandEntity
    {
        public int[] DefendingArmyIds { get; set; }

        public AttackCommandEntity AttackCommand { get; set; }

        public int TargetTileX { get; set; }

        public int TargetTileY { get; set; }
    }
}