using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class SearchRuinsCommand : SearchLocationCommand
    {
        public IBoon Boon { get; private set; }
        public object BoonResult { get; set; }

        public SearchRuinsCommand(LocationController locationController, List<Army> armies, Location location)
            : base(locationController, armies, location)
        {
        }

        protected override ActionState ExecuteInternal()
        {
            bool success = this.LocationController.SearchRuins(this.Armies, this.Location, out IBoon boon);
            if (success)
            {
                this.Boon = boon;
                this.BoonResult = boon.Result;
            }

            return (success) ? ActionState.Succeeded : ActionState.Failed;
        }
    }
}
