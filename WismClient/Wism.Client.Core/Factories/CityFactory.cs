using System;
using Wism.Client.Core;
using Wism.Client.Entities;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Factories
{
    public static class CityFactory
    {
        public static City Load(CityEntity snapshot, World world)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (world is null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            MapBuilder.AddCity(world, snapshot.X, snapshot.Y, snapshot.CityShortName, snapshot.ClanShortName);
            
            var city = world.Map[snapshot.X, snapshot.Y].City;            
            city.Defense = snapshot.Defense;
            city.Id = snapshot.Id;

            LoadBarracks(snapshot, world, city.Barracks);

            return city;
        }

        private static void LoadBarracks(CityEntity snapshot, World world, Barracks barracks)
        {
            // Armies To Deliver
            if (snapshot.ArmiesToDeliver != null && snapshot.ArmiesToDeliver.Length > 0)
            {
                if (barracks.ArmiesToDeliver == null)
                {
                    barracks.ArmiesToDeliver = new System.Collections.Generic.Queue<ArmyInTraining>();
                }
                barracks.ArmiesToDeliver.Clear();
                foreach (var aitEntity in snapshot.ArmiesToDeliver)
                {
                    var ait = ArmyInTrainingFactory.Load(aitEntity, world);
                    barracks.ArmiesToDeliver.Enqueue(ait);
                    // TODO: Check to see if this is queued in the same order
                }
            }

            barracks.ArmyInTraining = ArmyInTrainingFactory.Load(snapshot.ArmyInTraining, world);
        }
    }
}