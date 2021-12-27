using System;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Api.Commands
{
    public class StartProductionCommand : Command
    {
        public CityController CityController { get; }
        public City ProductionCity { get; }
        public ArmyInfo ArmyInfo { get; }
        public City DestinationCity { get; }

        public StartProductionCommand(CityController cityController, 
            City productionCity, ArmyInfo armyInfo, City destinationCity = null)
            : base()
        {
            CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            ProductionCity = productionCity ?? throw new ArgumentNullException(nameof(productionCity));
            ArmyInfo = armyInfo ?? throw new ArgumentNullException(nameof(armyInfo));
            DestinationCity = destinationCity;
        }

        protected override ActionState ExecuteInternal()
        {
            bool success;
            if (DestinationCity == null)
            {
                success = this.CityController.TryStartingProduction(ProductionCity, ArmyInfo);
            }
            else
            {
                success = this.CityController.TryStartingProductionToDestination(ProductionCity, ArmyInfo, DestinationCity);
            }

            return success ? ActionState.Succeeded : ActionState.Failed;
        }

        public override string ToString()
        {
            var dest = (DestinationCity == null) ? ProductionCity.DisplayName : DestinationCity.DisplayName;
            return $"{ProductionCity.DisplayName} start production of {ArmyInfo.DisplayName} at {dest}";                
        }
    }
}
