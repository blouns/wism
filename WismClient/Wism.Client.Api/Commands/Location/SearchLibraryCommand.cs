using System.Collections.Generic;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class SearchLibraryCommand : SearchLocationCommand
    {
        public string Knowledge { get; private set; }

        public SearchLibraryCommand(LocationController locationController, List<Army> armies, Location location)
            : base(locationController, armies, location)
        {
        }

        protected override ActionState ExecuteInternal()
        {
            bool success = this.LocationController.SearchLibrary(this.Armies, this.Location, out string item);

            this.Knowledge = item;
            return (success) ? ActionState.Succeeded : ActionState.Failed;
        }

    }
}
