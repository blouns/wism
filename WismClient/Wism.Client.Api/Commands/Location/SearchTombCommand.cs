﻿using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Api.Commands
{
    public class SearchTombCommand : Command
    {
        public LocationController LocationController { get; }
        public List<Army> Armies { get; }
        public Location Location { get; }

        public IBoon Boon { get; private set; }

        public object BoonResult { get; set; }

        public SearchTombCommand(LocationController locationController, List<Army> armies, Location location)
        {
            LocationController = locationController ?? throw new ArgumentNullException(nameof(locationController));
            Armies = armies ?? throw new ArgumentNullException(nameof(armies));
            Location = location ?? throw new ArgumentNullException(nameof(location));
        }

        protected override ActionState ExecuteInternal()
        {
            bool success = LocationController.SearchTomb(Armies, Location, out IBoon boon);
            if (success)
            {
                Boon = boon;
                BoonResult = boon.Result;
            }
            return (success) ? ActionState.Succeeded : ActionState.Failed;
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(Armies)} search {Location}";
        }
    }
}