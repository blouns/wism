using Wism.Client.Controllers;

namespace Wism.Client.Controllers
{
    public class ControllerProvider
    {
        public GameController GameController { get; set; }

        public CommandController CommandController { get; set; }

        public ArmyController ArmyController { get; set; }

        public CityController CityController { get; set; }

        public LocationController LocationController { get; set; }

        public HeroController HeroController { get; set; }

        public PlayerController PlayerController { get; set; }
    }
}