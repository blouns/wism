namespace Wism.Client.Api.Data.Entities
{
    public class PrepareForBattleCommandEntity : ArmyCommandEntity
    {
        public int TargetTileX { get; set; }

        public int TargetTileY { get; set; }

        public int[] DefendingArmyIds { get; set; }
    }
}