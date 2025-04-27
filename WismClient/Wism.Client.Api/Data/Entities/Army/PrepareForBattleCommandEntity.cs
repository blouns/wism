namespace Wism.Client.Data.Entities.Army
{
    public class PrepareForBattleCommandEntity : ArmyCommandEntity
    {
        public int TargetTileX { get; set; }

        public int TargetTileY { get; set; }

        public int[] DefendingArmyIds { get; set; }
    }
}