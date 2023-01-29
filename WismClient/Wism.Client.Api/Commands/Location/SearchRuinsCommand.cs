using System.Collections.Generic;
using Wism.Client.Api.Commands;
using Wism.Client.Controllers;
using Wism.Client.Core.Boons;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Commands.Location
{
    public class SearchRuinsCommand : SearchLocationCommand
    {
        public SearchRuinsCommand(LocationController locationController, List<MapObjects.Army> armies, MapObjects.Location location)
            : base(locationController, armies, location)
        {
        }

        public IBoon Boon { get; private set; }
        public object BoonResult { get; set; }

        protected override ActionState ExecuteInternal()
        {
            var success = this.LocationController.SearchRuins(this.Armies, this.Location, out var boon);
            if (success)
            {
                this.Boon = boon;
                this.BoonResult = boon.Result;
            }

            return success ? ActionState.Succeeded : ActionState.Failed;
        }
    }
}