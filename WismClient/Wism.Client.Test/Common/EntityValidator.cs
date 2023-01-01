using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Entities;
using Wism.Client.MapObjects;

namespace Wism.Client.Test.Common;

public static class EntityValidator
{
    public static void ValidateLocations(List<Location> locations, LocationEntity[] locationEntities)
    {
        if (locations == null || locations.Count == 0)
        {
            Assert.IsNull(locationEntities);
            return;
        }

        Assert.IsNotNull(locationEntities);
        Assert.AreEqual(locations.Count, locationEntities.Length);
        for (var i = 0; i < locationEntities.Length; i++)
        {
            Assert.IsNotNull(locationEntities[i]);
            Assert.AreEqual(locations[i].HasBoon(), locationEntities[i].Boon != null);
            if (locations[i].HasBoon())
            {
                ValidateBoon(locations[i].Boon, locationEntities[i].Boon);
            }

            Assert.AreEqual(locations[i].Id, locationEntities[i].Id);
            Assert.AreEqual(locations[i].ShortName, locationEntities[i].LocationShortName);
            Assert.AreEqual(locations[i].Monster, locationEntities[i].Monster);
            Assert.AreEqual(locations[i].Searched, locationEntities[i].Searched);
            Assert.AreEqual(locations[i].X, locationEntities[i].X);
            Assert.AreEqual(locations[i].Y, locationEntities[i].Y);
        }
    }

    public static void ValidateBoon(IBoon boon, BoonEntity boonEntity)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(boonEntity.BoonAssemblyName));
        Assert.IsFalse(string.IsNullOrWhiteSpace(boonEntity.BoonTypeName));
        if (boon is AlliesBoon)
        {
            Assert.AreEqual(((AlliesBoon)boon).ArmyInfo.ShortName, boonEntity.AlliesShortName);
        }
        else if (boon is ArtifactBoon)
        {
            Assert.AreEqual(((ArtifactBoon)boon).Artifact.ShortName, boonEntity.ArtifactShortName);
        }
    }

    public static void ValidateCities(List<City> cities, CityEntity[] cityEntities)
    {
        if (cities == null || cities.Count == 0)
        {
            Assert.IsNull(cityEntities);
            return;
        }

        Assert.IsNotNull(cityEntities);
        Assert.AreEqual(cities.Count, cityEntities.Length);
        for (var i = 0; i < cities.Count; i++)
        {
            Assert.IsNotNull(cityEntities[i]);
            if (cities[i].Clan != null)
            {
                Assert.AreEqual(cities[i].Clan.ShortName, cityEntities[i].ClanShortName);
            }

            Assert.AreEqual(cities[i].Defense, cityEntities[i].Defense);
            Assert.AreEqual(cities[i].Id, cityEntities[i].Id);
            Assert.AreEqual(cities[i].X, cityEntities[i].X);
            Assert.AreEqual(cities[i].Y, cityEntities[i].Y);

            ValidateProduction(cities[i].Barracks, cityEntities[i]);
        }
    }

    public static void ValidateProduction(Barracks barracks, CityEntity cityEntity)
    {
        Assert.AreEqual(barracks.HasDeliveries(), cityEntity.ArmiesToDeliver != null);

        // Production slots
        var productionSlots = barracks.GetProductionKinds();
        Assert.AreEqual(productionSlots.Count > 0, cityEntity.ProductionInfo != null);
        if (productionSlots.Count > 0)
        {
            Assert.IsNotNull(cityEntity.ProductionInfo.ArmyNames);
            Assert.IsNotNull(cityEntity.ProductionInfo.ProductionNumbers);
            for (var i = 0; i < productionSlots.Count; i++)
            {
                Assert.AreEqual(productionSlots[i].ArmyInfoName, cityEntity.ProductionInfo.ArmyNames[i]);
                Assert.AreEqual(
                    barracks.GetProductionNumber(productionSlots[i].ArmyInfoName),
                    cityEntity.ProductionInfo.ProductionNumbers[i]);
            }
        }

        // Armies to deliver
        if (barracks.HasDeliveries())
        {
            Assert.IsNotNull(cityEntity.ArmiesToDeliver);
            for (var i = 0; i < cityEntity.ArmiesToDeliver.Length; i++)
            {
                Assert.IsNotNull(cityEntity.ArmiesToDeliver[i]);
                foreach (var ait in barracks.ArmiesToDeliver)
                {
                    var aitEntity = cityEntity.ArmiesToDeliver[i];
                    ValidateArmyInTraining(ait, aitEntity);
                }
            }
        }

        // Army in training
        Assert.AreEqual(barracks.ProducingArmy(), cityEntity.ArmyInTraining != null);
        if (barracks.ProducingArmy())
        {
            Assert.IsNotNull(cityEntity.ArmyInTraining);
            ValidateArmyInTraining(barracks.ArmyInTraining, cityEntity.ArmyInTraining);
        }
    }

    public static void ValidateArmyInTraining(ArmyInTraining ait, ArmyInTrainingEntity aitEntity)
    {
        Assert.AreEqual(ait.ArmyInfo.ShortName, aitEntity.ArmyShortName);
        Assert.AreEqual(ait.DestinationCity.ShortName, aitEntity.DestinationCityShortName);
        Assert.AreEqual(ait.DisplayName, aitEntity.DisplayName);
        Assert.AreEqual(ait.Moves, aitEntity.Moves);
        Assert.AreEqual(ait.ProductionCity.ShortName, aitEntity.ProductionCityShortName);
        Assert.AreEqual(ait.Strength, aitEntity.Strength);
        Assert.AreEqual(ait.TurnsToDeliver, aitEntity.TurnsToDeliver);
        Assert.AreEqual(ait.TurnsToProduce, aitEntity.TurnsToProduce);
        Assert.AreEqual(ait.Upkeep, aitEntity.Upkeep);
    }

    public static void ValidateTiles(World world, TileEntity[] tiles, int xBound, int yBound)
    {
        for (var y = 0; y < yBound; y++)
        {
            for (var x = 0; x < xBound; x++)
            {
                var worldTile = world.Map[x, y];
                var entityTile = tiles[x + y * xBound];

                // Verify position
                Assert.AreEqual(worldTile.X, entityTile.X);
                Assert.AreEqual(worldTile.Y, entityTile.Y);

                // Verify terrain
                Assert.AreEqual(worldTile.Terrain.ShortName, entityTile.TerrainShortName);

                // Verify armies
                Assert.AreEqual(worldTile.HasArmies(), entityTile.ArmyIds != null);
                if (worldTile.HasArmies())
                {
                    Assert.AreEqual(worldTile.Armies.Count, entityTile.ArmyIds.Length);
                    for (var j = 0; j < entityTile.ArmyIds.Length; j++)
                    {
                        Assert.AreEqual(worldTile.Armies[j].Id, entityTile.ArmyIds[j]);
                    }
                }

                // Verify visiting armies
                Assert.AreEqual(worldTile.HasVisitingArmies(), entityTile.VisitingArmyIds != null);
                if (worldTile.HasVisitingArmies())
                {
                    Assert.AreEqual(worldTile.VisitingArmies.Count, entityTile.VisitingArmyIds.Length);
                    for (var j = 0; j < entityTile.VisitingArmyIds.Length; j++)
                    {
                        Assert.AreEqual(worldTile.VisitingArmies[j].Id, entityTile.VisitingArmyIds[j]);
                    }
                }

                // Verify locations
                Assert.AreEqual(worldTile.HasLocation(), entityTile.LocationShortName != null);
                if (worldTile.HasLocation())
                {
                    Assert.AreEqual(worldTile.Location.ShortName, entityTile.LocationShortName);
                }

                // Verify cities
                Assert.AreEqual(worldTile.HasCity(), entityTile.CityShortName != null);
                if (worldTile.HasCity())
                {
                    Assert.AreEqual(worldTile.City.ShortName, entityTile.CityShortName);
                }

                // Verify items
                Assert.AreEqual(worldTile.HasItems(), entityTile.Items != null);
                if (worldTile.HasItems())
                {
                    Assert.AreEqual(worldTile.Items.Count, entityTile.Items.Length);
                    for (var j = 0; j < entityTile.Items.Length; j++)
                    {
                        Assert.AreEqual(worldTile.Items[j].Id, entityTile.Items[j].Id);
                        Assert.AreEqual(worldTile.Items[j].ShortName, entityTile.Items[j].ArtifactShortName);
                    }
                }
            }
        }
    }
}