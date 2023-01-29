using System.Collections.Generic;
using Wism.Client.Api.Commands;
using Wism.Client.Controllers;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Commands.Location
{
    public class SearchSageCommand : SearchLocationCommand
    {
        public SearchSageCommand(LocationController locationController, List<MapObjects.Army> armies, MapObjects.Location location)
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