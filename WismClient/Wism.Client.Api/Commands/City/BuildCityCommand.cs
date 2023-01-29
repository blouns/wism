using System;
using Wism.Client.Controllers;

namespace Wism.Client.Commands.City
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

        protected override ActionState ExecuteInternal()
        {
            if (this.CityController.TryBuildDefense(this.City))
            {
                return ActionState.Succeeded;
            }

            return ActionState.Failed;
        }

        public override string ToString()
        {
            return $"{this.City} build defense";
        }
    }
}