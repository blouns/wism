using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.Commands
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

        public override ActionState Execute()
        {
            ProductionCity.Barracks.StopProduction();

            return ActionState.Succeeded;
        }
    }
}
