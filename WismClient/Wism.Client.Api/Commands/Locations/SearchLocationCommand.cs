using System;
using System.Collections.Generic;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Commands.Locations
{
    public abstract class SearchLocationCommand : Command
    {
        protected SearchLocationCommand(LocationController locationController, List<Army> armies, Location location)
        {
            LocationController = locationController ?? throw new ArgumentNullException(nameof(locationController));
            Armies = armies ?? throw new ArgumentNullException(nameof(armies));
            Location = location ?? throw new ArgumentNullException(nameof(location));
        }

        public LocationController LocationController { get; }
        public List<Army> Armies { get; }
        public Location Location { get; }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(Armies)} search {Location}";
        }
    }
}