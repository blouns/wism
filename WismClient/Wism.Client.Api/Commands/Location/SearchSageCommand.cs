using System;
using System.Collections.Generic;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class SearchSageCommand : SearchLocationCommand
    {
        public int Gold { get; private set; }

        public SearchSageCommand(LocationController locationController, List<Army> armies, Location location)
            : base(locationController, armies, location)
        {
        }

        protected override ActionState ExecuteInternal()
        {
            bool success = LocationController.SearchSage(Armies, Location, out int gold);

            Gold = gold;
            return (success) ? ActionState.Succeeded : ActionState.Failed;
        }
    }
}
