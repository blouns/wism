using System.Collections.Generic;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class SearchTempleCommand : SearchLocationCommand
    {
        public SearchTempleCommand(LocationController locationController, List<Army> armies, Location location)
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