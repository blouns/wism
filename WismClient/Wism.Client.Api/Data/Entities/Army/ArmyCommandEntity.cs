namespace Wism.Client.Data.Entities.Army
{
    public abstract class ArmyCommandEntity : CommandEntity
    {
        public int[] ArmyIds { get; set; }
    }
}