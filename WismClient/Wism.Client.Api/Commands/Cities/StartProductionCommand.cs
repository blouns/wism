using System;
using Wism.Client.Controllers;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Commands.Cities
{
    public class StartProductionCommand : Command
    {
        public StartProductionCommand(CityController cityController,
            MapObjects.City productionCity, ArmyInfo armyInfo, MapObjects.City destinationCity = null)
        {
            this.CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.ProductionCity = productionCity ?? throw new ArgumentNullException(nameof(productionCity));
            this.ArmyInfo = armyInfo ?? throw new ArgumentNullException(nameof(armyInfo));
            this.DestinationCity = destinationCity;
        }

        public CityController CityController { get; }
        public MapObjects.City ProductionCity { get; }
        public ArmyInfo ArmyInfo { get; }
        public MapObjects.City DestinationCity { get; }

        protected override ActionState ExecuteInternal()
        {
            bool success;
            if (this.DestinationCity == null)
            {
                success = this.CityController.TryStartingProduction(this.ProductionCity, this.ArmyInfo);
            }
            else
            {
                success = this.CityController.TryStartingProductionToDestination(this.ProductionCity, this.ArmyInfo,
                    this.DestinationCity);
            }

            return success ? ActionState.Succeeded : ActionState.Failed;
        }

        public override string ToString()
        {
            var dest = this.DestinationCity == null
                ? this.ProductionCity.DisplayName
                : this.DestinationCity.DisplayName;
            return $"{this.ProductionCity.DisplayName} start production of {this.ArmyInfo.DisplayName} at {dest}";
        }
    }
}