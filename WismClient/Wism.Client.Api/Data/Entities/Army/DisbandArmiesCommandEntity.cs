namespace Wism.Client.Data.Entities.Army
{
    public sealed class DisbandArmiesCommandEntity : CommandEntity
    {
        public int?[] RequestedArmyIds { get; set; }
    }
}
