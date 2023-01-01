using System;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class StopProductionCommand : Command
    {
        public StopProductionCommand(CityController cityController, City productionCity)
        {
            this.CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.ProductionCity = productionCity ?? throw new ArgumentNullException(nameof(productionCity));
        }

        public CityController CityController { get; }

        public City ProductionCity { get; }

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