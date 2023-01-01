using System;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class BuildCityCommand : Command
    {
        public CityController CityController { get; }

        public City City { get; }

        public BuildCityCommand(CityController cityController, City city)
            : base(city.Player)
        {
            this.CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.City = city ?? throw new ArgumentNullException(nameof(city));
        }

        protected override ActionState ExecuteInternal()
        {
            if (this.CityController.TryBuildDefense(this.City))
            {
                return ActionState.Succeeded;
            }
            else
            {
                return ActionState.Failed;
            }
        }

        public override string ToString()
        {
            return $"{this.City} build defense";
        }
    }
}
