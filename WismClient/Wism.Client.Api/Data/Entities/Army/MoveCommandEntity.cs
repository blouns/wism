namespace Wism.Client.Data.Entities.Army
{
    public class MoveCommandEntity : ArmyCommandEntity
    {
        public int? TargetX { get; set; }
        public int? TargetY { get; set; }
        public bool RevalidateFriendlyCapacity { get; set; }
        public bool RouteChecked { get; set; }
        public bool RouteRevalidationFailed { get; set; }
        public int[] PathX { get; set; }

        public int[] PathY { get; set; }
    }
}
