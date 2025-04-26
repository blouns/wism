using System;
using Wism.Client.Controllers;

namespace Wism.Client.Commands.Cities
{
    public class StopProductionCommand : Command
    {
        public StopProductionCommand(CityController cityController, MapObjects.City productionCity)
        {
            this.CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.ProductionCity = productionCity ?? throw new ArgumentNullException(nameof(productionCity));
        }

        public CityController CityController { get; }

        public MapObjects.City ProductionCity { get; }

        protected override ActionState ExecuteInternal()
        {
            this.CityController.StopProduction(this.ProductionCity);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"{this.ProductionCity} stop production";
        }
    }
}