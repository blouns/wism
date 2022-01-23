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
            this.CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.ProductionCity = productionCity ?? throw new ArgumentNullException(nameof(productionCity));
            this.ArmyInfo = armyInfo ?? throw new ArgumentNullException(nameof(armyInfo));
            this.DestinationCity = destinationCity;
        }

        protected override ActionState ExecuteInternal()
        {
            bool success;
            if (this.DestinationCity == null)
            {
                success = this.CityController.TryStartingProduction(this.ProductionCity, this.ArmyInfo);
            }
            else
            {
                success = this.CityController.TryStartingProductionToDestination(this.ProductionCity, this.ArmyInfo, this.DestinationCity);
            }

            return success ? ActionState.Succeeded : ActionState.Failed;
        }

        public override string ToString()
        {
            var dest = (this.DestinationCity == null) ? this.ProductionCity.DisplayName : this.DestinationCity.DisplayName;
            return $"{this.ProductionCity.DisplayName} start production of {this.ArmyInfo.DisplayName} at {dest}";
        }
    }
}
