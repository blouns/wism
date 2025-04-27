namespace Wism.Client.Data.Entities.Army
{
    public class MoveCommandEntity : ArmyCommandEntity
    {
        public int[] PathX { get; set; }

        public int[] PathY { get; set; }
    }
}