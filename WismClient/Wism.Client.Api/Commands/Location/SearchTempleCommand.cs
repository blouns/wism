using System;
using System.Collections.Generic;
using System.Text;
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
            bool success = LocationController.SearchTemple(Armies, Location, out int result);

            BlessedArmyCount = result;
            return (success) ? ActionState.Succeeded : ActionState.Failed;
        }
    }
}
