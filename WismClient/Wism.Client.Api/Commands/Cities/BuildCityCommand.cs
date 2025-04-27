using System;
using Wism.Client.Controllers;

namespace Wism.Client.Commands.Cities
{
    public class BuildCityCommand : Command
    {
        public BuildCityCommand(CityController cityController, MapObjects.City city)
            : base(city.Player)
        {
            this.CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.City = city ?? throw new ArgumentNullException(nameof(city));
        }

        public CityController CityController { get; }

        public MapObjects.City City { get; }

        public bool InsufficientGold { get; set; }

        public bool AtMaxDefense { get; set; }

        protected override ActionState ExecuteInternal()
        {
            if (this.CityController.TryBuildDefense(this.City))
            {
                return ActionState.Succeeded;
            }

            // Why failed?
            InsufficientGold = (Player.Gold < City.GetCostToBuild());
            AtMaxDefense = City.Defense == 9;

            return ActionState.Failed;
        }

        public override string ToString()
        {
            return $"{this.City} build defense";
        }
    }
}