using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Commands.Armies;
using Wism.Client.Core;
using Wism.Client.Core.Armies;
using Wism.Client.Core.Boons;
using Wism.Client.Data.Entities;
using Wism.Client.MapObjects;

namespace Wism.Client.Test.Common;

public static class EntityValidator
{
    public static void ValidateLocations(List<Location> locations, LocationEntity[] locationEntities)
    {
        if (locations == null || locations.Count == 0)
        {
            Assert.That(locationEntities, Is.Not.Null);
            return;
        }

        Assert.That(locationEntities, Is.Not.Null);
        Assert.That(locationEntities.Length, Is.EqualTo(locations.Count));
        for (var i = 0; i < locationEntities.Length; i++)
        {
            Assert.That(locationEntities[i], Is.Not.Null);
            Assert.That(locationEntities[i].Boon != null, Is.EqualTo(locations[i].HasBoon()));
            if (locations[i].HasBoon())
            {
                ValidateBoon(locations[i].Boon, locationEntities[i].Boon);
            }

            Assert.That(locationEntities[i].Id, Is.EqualTo(locations[i].Id));
            Assert.That(locationEntities[i].LocationShortName, Is.EqualTo(locations[i].ShortName));
            Assert.That(locationEntities[i].Monster, Is.EqualTo(locations[i].Monster));
            Assert.That(locationEntities[i].Searched, Is.EqualTo(locations[i].Searched));
            Assert.That(locationEntities[i].X, Is.EqualTo(locations[i].X));
            Assert.That(locationEntities[i].Y, Is.EqualTo(locations[i].Y));
        }
    }

    public static void ValidateBoon(IBoon boon, BoonEntity boonEntity)
    {

        Assert.That(string.IsNullOrWhiteSpace(boonEntity.BoonAssemblyName), Is.False);
        Assert.That(string.IsNullOrWhiteSpace(boonEntity.BoonTypeName), Is.False);
        if (boon is AlliesBoon)
        {
            Assert.That(boonEntity.AlliesShortName, Is.EqualTo(((AlliesBoon)boon).ArmyInfo.ShortName));
        }
        else if (boon is ArtifactBoon)
        {
            Assert.That(boonEntity.ArtifactShortName, Is.EqualTo(((ArtifactBoon)boon).Artifact.ShortName));
        }
    }

    public static void ValidateCities(List<City> cities, CityEntity[] cityEntities)
    {
        if (cities == null || cities.Count == 0)
        {
            Assert.That(cityEntities, Is.Not.Null);
            return;
        }

        Assert.That(cityEntities, Is.Not.Null);
        Assert.That(cityEntities.Length, Is.EqualTo(cities.Count));
        for (var i = 0; i < cities.Count; i++)
        {
            Assert.That(cityEntities[i], Is.Not.Null);
            if (cities[i].Clan != null)
            {
                Assert.That(cityEntities[i].ClanShortName, Is.EqualTo(cities[i].Clan.ShortName));
            }

            Assert.That(cityEntities[i].Defense, Is.EqualTo(cities[i].Defense));
            Assert.That(cityEntities[i].Id, Is.EqualTo(cities[i].Id));
            Assert.That(cityEntities[i].X, Is.EqualTo(cities[i].X));
            Assert.That(cityEntities[i].Y, Is.EqualTo(cities[i].Y));

            ValidateProduction(cities[i].Barracks, cityEntities[i]);
        }
    }

    public static void ValidateProduction(Barracks barracks, CityEntity cityEntity)
    {
        Assert.That(cityEntity.ArmiesToDeliver != null, Is.EqualTo(barracks.HasDeliveries()));

        // Production slots
        var productionSlots = barracks.GetProductionKinds();
        Assert.That(cityEntity.ProductionInfo != null, Is.EqualTo(productionSlots.Count > 0));
        if (productionSlots.Count > 0)
        {
            Assert.That(cityEntity.ProductionInfo.ArmyNames, Is.Not.Null);
            Assert.That(cityEntity.ProductionInfo.ProductionNumbers, Is.Not.Null);
            for (var i = 0; i < productionSlots.Count; i++)
            {
                Assert.That(cityEntity.ProductionInfo.ArmyNames[i], Is.EqualTo(productionSlots[i].ArmyInfoName));
                Assert.That(
                    cityEntity.ProductionInfo.ProductionNumbers[i], Is.EqualTo(barracks.GetProductionNumber(productionSlots[i].ArmyInfoName)));
            }
        }

        // Armies to deliver
        if (barracks.HasDeliveries())
        {
            Assert.That(cityEntity.ArmiesToDeliver, Is.Not.Null);
            for (var i = 0; i < cityEntity.ArmiesToDeliver.Length; i++)
            {
                Assert.That(cityEntity.ArmiesToDeliver[i], Is.Not.Null);
                foreach (var ait in barracks.ArmiesToDeliver)
                {
                    var aitEntity = cityEntity.ArmiesToDeliver[i];
                    ValidateArmyInTraining(ait, aitEntity);
                }
            }
        }

        // Army in training
        Assert.That(cityEntity.ArmyInTraining != null, Is.EqualTo(barracks.ProducingArmy()));
        if (barracks.ProducingArmy())
        {
            Assert.That(cityEntity.ArmyInTraining, Is.Not.Null);
            ValidateArmyInTraining(barracks.ArmyInTraining, cityEntity.ArmyInTraining);
        }
    }

    public static void ValidateArmyInTraining(ArmyInTraining ait, ArmyInTrainingEntity aitEntity)
    {
        Assert.That(aitEntity.ArmyShortName, Is.EqualTo(ait.ArmyInfo.ShortName));
        Assert.That(aitEntity.DestinationCityShortName, Is.EqualTo(ait.DestinationCity.ShortName));
        Assert.That(aitEntity.DisplayName, Is.EqualTo(ait.DisplayName));
        Assert.That(aitEntity.Moves, Is.EqualTo(ait.Moves));
        Assert.That(aitEntity.ProductionCityShortName, Is.EqualTo(ait.ProductionCity.ShortName));
        Assert.That(aitEntity.Strength, Is.EqualTo(ait.Strength));
        Assert.That(aitEntity.TurnsToDeliver, Is.EqualTo(ait.TurnsToDeliver));
        Assert.That(aitEntity.TurnsToProduce, Is.EqualTo(ait.TurnsToProduce));
        Assert.That(aitEntity.Upkeep, Is.EqualTo(ait.Upkeep));
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
                Assert.That(entityTile.X, Is.EqualTo(worldTile.X));
                Assert.That(entityTile.Y, Is.EqualTo(worldTile.Y));

                // Verify terrain
                Assert.That(entityTile.TerrainShortName, Is.EqualTo(worldTile.Terrain.ShortName));

                // Verify armies
                Assert.That(entityTile.ArmyIds != null, Is.EqualTo(worldTile.HasArmies()));
                if (worldTile.HasArmies())
                {
                    Assert.That(entityTile.ArmyIds.Length, Is.EqualTo(worldTile.Armies.Count));
                    for (var j = 0; j < entityTile.ArmyIds.Length; j++)
                    {
                        Assert.That(entityTile.ArmyIds[j], Is.EqualTo(worldTile.Armies[j].Id));
                    }
                }

                // Verify visiting armies
                Assert.That(entityTile.VisitingArmyIds != null, Is.EqualTo(worldTile.HasVisitingArmies()));
                if (worldTile.HasVisitingArmies())
                {
                    Assert.That(entityTile.VisitingArmyIds.Length, Is.EqualTo(worldTile.VisitingArmies.Count));
                    for (var j = 0; j < entityTile.VisitingArmyIds.Length; j++)
                    {
                        Assert.That(entityTile.VisitingArmyIds[j], Is.EqualTo(worldTile.VisitingArmies[j].Id));
                    }
                }

                // Verify locations
                Assert.That(entityTile.LocationShortName != null, Is.EqualTo(worldTile.HasLocation()));
                if (worldTile.HasLocation())
                {
                    Assert.That(entityTile.LocationShortName, Is.EqualTo(worldTile.Location.ShortName));
                }

                // Verify cities
                Assert.That(entityTile.CityShortName != null, Is.EqualTo(worldTile.HasCity()));
                if (worldTile.HasCity())
                {
                    Assert.That(entityTile.CityShortName, Is.EqualTo(worldTile.City.ShortName));
                }

                // Verify items
                Assert.That(entityTile.Items != null, Is.EqualTo(worldTile.HasItems()));
                if (worldTile.HasItems())
                {
                    Assert.That(entityTile.Items.Length, Is.EqualTo(worldTile.Items.Count));
                    for (var j = 0; j < entityTile.Items.Length; j++)
                    {
                        Assert.That(entityTile.Items[j].Id, Is.EqualTo(worldTile.Items[j].Id));
                        Assert.That(entityTile.Items[j].ArtifactShortName, Is.EqualTo(worldTile.Items[j].ShortName));
                    }
                }
            }
        }
    }
}