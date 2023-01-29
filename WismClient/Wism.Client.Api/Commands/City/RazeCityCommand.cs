using System;
using Wism.Client.Controllers;

namespace Wism.Client.Commands.City
{
    public class RazeCityCommand : Command
    {
        public RazeCityCommand(CityController cityController, MapObjects.City city)
            : base(city.Player)
        {
            this.CityController = cityController ?? throw new ArgumentNullException(nameof(cityController));
            this.City = city ?? throw new ArgumentNullException(nameof(city));
        }

        public CityController CityController { get; }

        public MapObjects.City City { get; }

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