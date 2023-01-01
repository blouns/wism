using System.Collections.Generic;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class SearchSageCommand : SearchLocationCommand
    {
        public SearchSageCommand(LocationController locationController, List<Army> armies, Location location)
            : base(locationController, armies, location)
        {
        }

        public int Gold { get; private set; }

        protected override ActionState ExecuteInternal()
        {
            var success = this.LocationController.SearchSage(this.Armies, this.Location, out var gold);

            this.Gold = gold;
            return success ? ActionState.Succeeded : ActionState.Failed;
        }
    }
}