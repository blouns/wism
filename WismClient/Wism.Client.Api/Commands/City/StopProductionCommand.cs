using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class StopProductionCommand : Command
    {
        public StopProductionCommand(CityController cityController, City productionCity)
        {
            CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            ProductionCity = productionCity ?? throw new ArgumentNullException(nameof(productionCity));
        }

        public CityController CityController { get; }
        public City ProductionCity { get; }

        protected override ActionState ExecuteInternal()
        {
            ProductionCity.Barracks.StopProduction();

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"{ProductionCity} stop production";
        }
    }
}
