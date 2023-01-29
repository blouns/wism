using System.Collections.Generic;
using Wism.Client.Api.Commands;
using Wism.Client.Controllers;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Commands.Location
{
    public class SearchTempleCommand : SearchLocationCommand
    {
        public SearchTempleCommand(LocationController locationController, List<MapObjects.Army> armies, MapObjects.Location location)
            : base(locationController, armies, location)
        {
        }

        public int BlessedArmyCount { get; private set; }

        protected override ActionState ExecuteInternal()
        {
            var success = this.LocationController.SearchTemple(this.Armies, this.Location, out var result);

            this.BlessedArmyCount = result;
            return success ? ActionState.Succeeded : ActionState.Failed;
        }
    }
}