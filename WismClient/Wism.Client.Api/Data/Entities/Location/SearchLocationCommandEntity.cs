namespace Wism.Client.Api.Data.Entities
{
    public abstract class SearchLocationCommandEntity : CommandEntity
    {
        public string LocationShortName { get; set; }

        public int[] ArmyIds { get; set; }
    }
}
