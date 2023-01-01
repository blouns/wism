namespace Wism.Client.Api.Data.Entities
{
    public class CompleteBattleCommandEntity : ArmyCommandEntity
    {
        public int[] DefendingArmyIds { get; set; }

        public AttackCommandEntity AttackCommand { get; set; }

        public int TargetTileX { get; set; }

        public int TargetTileY { get; set; }
    }
}