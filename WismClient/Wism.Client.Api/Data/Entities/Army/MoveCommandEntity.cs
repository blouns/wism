namespace Wism.Client.Api.Data.Entities
{
    public class MoveCommandEntity : ArmyCommandEntity
    {
        public int[] PathX { get; set; }

        public int[] PathY { get; set; }
    }
}
