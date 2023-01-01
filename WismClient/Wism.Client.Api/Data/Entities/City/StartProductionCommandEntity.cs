namespace Wism.Client.Data.Entities.City
{
    public class StartProductionCommandEntity : CommandEntity
    {
        public string ProductionCityShortName { get; set; }

        public string ArmyShortName { get; set; }

        public string DestinationCityShortName { get; set; }
    }
}