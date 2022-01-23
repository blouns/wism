using System.Collections.Generic;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class SearchTempleCommand : SearchLocationCommand
    {
        public int BlessedArmyCount { get; private set; }

        public SearchTempleCommand(LocationController locationController, List<Army> armies, Location location)
            : base(locationController, armies, location)
        {
        }

        protected override ActionState ExecuteInternal()
        {
            bool success = this.LocationController.SearchTemple(this.Armies, this.Location, out int result);

            this.BlessedArmyCount = result;
            return (success) ? ActionState.Succeeded : ActionState.Failed;
        }
    }
}
