using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class SearchRuinsCommand : SearchLocationCommand
    {
        public SearchRuinsCommand(LocationController locationController, List<Army> armies, Location location)
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