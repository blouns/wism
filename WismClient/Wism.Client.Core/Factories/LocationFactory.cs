using System;
using Wism.Client.Core;
using Wism.Client.Entities;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Factories
{
    internal class LocationFactory
    {
        internal static Location Load(LocationEntity locationEntity)
        {
            if (locationEntity is null)
            {
                throw new ArgumentNullException(nameof(locationEntity));
            }

            var info = ModFactory.FindLocationInfo(locationEntity.LocationShortName);
            var location = Location.Create(info);
            location.Id = locationEntity.Id;
            if (locationEntity.Boon != null)
            {
                location.Boon = BoonFactory.Load(locationEntity.Boon);
            }
            location.Monster = locationEntity.Monster;
            location.Searched = locationEntity.Searched;

            return location;
        }
    }
}