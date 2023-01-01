using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Intelligence
{
    public class TargetPortfolio
    {
        public List<Army> OpposingArmies { get; set; }

        public List<City> NeutralCities { get; set; }

        public List<City> OpposingCities { get; set; }

        public List<Location> UnsearchedLocations { get; set; }

        public List<Artifact> LooseItems { get; set; }
    }
}