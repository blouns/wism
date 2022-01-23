using System;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class RazeCityCommand : Command
    {
        public CityController CityController { get; }

        public City City { get; }

        public RazeCityCommand(CityController cityController, City city)
            : base(city.Player)
        {
            this.CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.City = city ?? throw new ArgumentNullException(nameof(city));
        }

        protected override ActionState ExecuteInternal()
        {
            this.CityController.RazeCity(this.City, this.Player);

            return ActionState.Succeeded;
        }

        public override string ToString()
        {
            return $"{this.City} raze";
        }
    }
}
