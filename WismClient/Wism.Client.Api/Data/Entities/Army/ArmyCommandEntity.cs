namespace Wism.Client.Api.Data.Entities
{
    public abstract class ArmyCommandEntity : CommandEntity
    {
        public int[] ArmyIds { get; set; }
    }
}
