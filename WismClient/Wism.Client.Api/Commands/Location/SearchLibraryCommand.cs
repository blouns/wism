﻿using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class SearchLibraryCommand : Command
    {
        public LocationController LocationController { get; }
        public List<Army> Armies { get; }
        public Location Location { get; }

        public string Knowledge { get; private set; }

        public SearchLibraryCommand(LocationController locationController, List<Army> armies, Location location)
        {
            LocationController = locationController ?? throw new ArgumentNullException(nameof(locationController));
            Armies = armies ?? throw new ArgumentNullException(nameof(armies));
            Location = location ?? throw new ArgumentNullException(nameof(location));
        }

        protected override ActionState ExecuteInternal()
        {
            bool success = LocationController.SearchLibrary(Armies, Location, out string item);

            Knowledge = item;
            return (success) ? ActionState.Succeeded : ActionState.Failed;
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(Armies)} search {Location}";
        }
    }
}