using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.Data.Entities;
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

        public static City Create(CityEntity cityEntity, World world)
        {
            if (cityEntity is null)
            {
                throw new ArgumentNullException(nameof(cityEntity));
            }

            if (world is null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            MapBuilder.AddCity(world, cityEntity.X, cityEntity.Y, cityEntity.CityShortName, cityEntity.ClanShortName);

            return world.Map[cityEntity.X, cityEntity.Y].City;
        }

        private static void LoadBarracks(CityEntity snapshot, World world, Barracks barracks)
        {
            // Armies To Deliver
            if (snapshot.ArmiesToDeliver != null && snapshot.ArmiesToDeliver.Length > 0)
            {
                if (barracks.ArmiesToDeliver == null)
                {
                    barracks.ArmiesToDeliver = new Queue<ArmyInTraining>();
                }

                barracks.ArmiesToDeliver.Clear();
                foreach (var aitEntity in snapshot.ArmiesToDeliver)
                {
                    var ait = ArmyInTrainingFactory.Load(aitEntity, world);
                    barracks.ArmiesToDeliver.Enqueue(ait);
                }
            }

            // Army in training
            barracks.ArmyInTraining = ArmyInTrainingFactory.Load(snapshot.ArmyInTraining, world);

            // Barracks details
            LoadProductionDetails(snapshot.ProductionInfo, barracks);
        }

        private static void LoadProductionDetails(ProductionEntity productionInfo, Barracks barracks)
        {
            if (productionInfo == null ||
                productionInfo.ArmyNames == null || productionInfo.ArmyNames.Length == 0 ||
                productionInfo.ProductionNumbers == null || productionInfo.ProductionNumbers.Length == 0)
            {
                return;
            }

            var productionKinds = barracks.GetProductionKinds();
            if (productionKinds.Count != productionInfo.ArmyNames.Length ||
                productionKinds.Count != productionInfo.ProductionNumbers.Length)
            {
                throw new InvalidOperationException("Production slots do not match");
            }

            for (var i = 0; i < productionKinds.Count; i++)
            {
                barracks.SetProductionNumber(productionInfo.ArmyNames[i], productionInfo.ProductionNumbers[i]);
            }
        }
    }
}