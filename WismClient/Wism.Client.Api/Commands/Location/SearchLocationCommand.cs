using System;
using System.Collections.Generic;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public abstract class SearchLocationCommand : Command
    {
        public LocationController LocationController { get; }
        public List<Army> Armies { get; }
        public Location Location { get; }

        public SearchLocationCommand(LocationController locationController, List<Army> armies, Location location)
        {
            this.LocationController = locationController ?? throw new ArgumentNullException(nameof(locationController));
            this.Armies = armies ?? throw new ArgumentNullException(nameof(armies));
            this.Location = location ?? throw new ArgumentNullException(nameof(location));
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(this.Armies)} search {this.Location}";
        }
    }
}
