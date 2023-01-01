using System;
using Wism.Client.Core;
using Wism.Client.Data.Entities;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Factories
{
    public class LocationFactory
    {
        internal static Location Load(LocationEntity locationEntity, World world)
        {
            if (locationEntity is null)
            {
                throw new ArgumentNullException(nameof(locationEntity));
            }

            var path = $@"{ModFactory.ModPath}\{ModFactory.WorldsPath}\{world.Name}";
            var builder = new LocationBuilder(path);
            builder.AddLocation(world, locationEntity.X, locationEntity.Y, locationEntity.LocationShortName);

            var location = world.Map[locationEntity.X, locationEntity.Y].Location;
            location.Id = locationEntity.Id;
            if (locationEntity.Boon != null)
            {
                location.Boon = BoonFactory.Load(locationEntity.Boon);
            }

            location.Monster = locationEntity.Monster;
            location.Searched = locationEntity.Searched;

            return location;
        }

        internal static Location Create(LocationEntity locationEntity, World world)
        {
            if (locationEntity is null)
            {
                throw new ArgumentNullException(nameof(locationEntity));
            }

            var path = $@"{ModFactory.ModPath}\{ModFactory.WorldsPath}\{world.Name}";
            var builder = new LocationBuilder(path);
            builder.AddLocation(world, locationEntity.X, locationEntity.Y, locationEntity.LocationShortName);

            return world.Map[locationEntity.X, locationEntity.Y].Location;
        }
    }
}