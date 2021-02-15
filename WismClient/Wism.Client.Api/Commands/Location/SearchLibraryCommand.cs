using System;
using System.Collections.Generic;
using System.Text;
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
            bool success = LocationController.SearchLibrary(Armies, Location, out string item);

            Knowledge = item;
            return (success) ? ActionState.Succeeded : ActionState.Failed;
        }

    }
}
