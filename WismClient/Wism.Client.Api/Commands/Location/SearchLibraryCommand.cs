using System.Collections.Generic;
using Wism.Client.Api.Commands;
using Wism.Client.Controllers;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Commands.Location
{
    public class SearchLibraryCommand : SearchLocationCommand
    {
        public SearchLibraryCommand(LocationController locationController, List<MapObjects.Army> armies, MapObjects.Location location)
            : base(locationController, armies, location)
        {
        }

        public string Knowledge { get; private set; }

        protected override ActionState ExecuteInternal()
        {
            var success = this.LocationController.SearchLibrary(this.Armies, this.Location, out var item);

            this.Knowledge = item;
            return success ? ActionState.Succeeded : ActionState.Failed;
        }
    }
}