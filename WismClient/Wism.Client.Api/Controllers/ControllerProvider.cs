using System;
using System.Collections.Generic;
using System.Text;

namespace Wism.Client.Core.Controllers
{
    public class ControllerProvider
    {
        public GameController GameController { get; set; }

        public CommandController CommandController { get; set; }

        public ArmyController ArmyController { get; set; }

        public CityController CityController { get; set; }

        public LocationController LocationController { get; set; }
    }
}
