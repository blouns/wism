using System;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Agent.Commands
{
    public class StartProductionCommand : Command
    {
        public StartProductionCommand(CityController cityController, City productionCity, ArmyInfo armyInfo, City destinationCity = null)
            : base()
        {
            CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            ProductionCity = productionCity ?? throw new ArgumentNullException(nameof(productionCity));
            ArmyInfo = armyInfo ?? throw new ArgumentNullException(nameof(armyInfo));
            DestinationCity = destinationCity;
        }

        public CityController CityController { get; }
        public City ProductionCity { get; }
        public ArmyInfo ArmyInfo { get; }
        public City DestinationCity { get; }

        public override ActionState Execute()
        {
            ActionState state = ActionState.Failed;

            if (ProductionCity.Barracks.StartProduction(ArmyInfo, DestinationCity))
            {
                state = ActionState.Succeeded;
            }

            return state;
        }
    }
}
