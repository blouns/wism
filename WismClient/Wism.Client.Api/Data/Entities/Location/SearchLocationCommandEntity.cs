namespace Wism.Client.Data.Entities.Location
{
    public abstract class SearchLocationCommandEntity : CommandEntity
    {
        public string LocationShortName { get; set; }

        public int[] ArmyIds { get; set; }
    }
}