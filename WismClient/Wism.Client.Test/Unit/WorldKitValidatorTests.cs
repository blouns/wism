using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.Modules.Worlds;

namespace Wism.Client.Test.Unit;

[TestFixture]
public sealed class WorldKitValidatorTests
{
    [Test]
    public void TestWorld_ValidatesForTwoKnownStarts()
    {
        var report = WorldKitValidator.ValidateModRoot(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "mod"),
            "TestWorld",
            new WorldKitValidationOptions
            {
                RequestedPlayers = 2,
                ActiveClans = new[] { "Sirians", "LordBane" }
            });

        Assert.That(report.IsValid, Is.True, string.Join(Environment.NewLine, report.Issues));
        Assert.That(report.Coverage.CityCount, Is.EqualTo(3));
        Assert.That(report.Coverage.ClansWithStarts, Is.EqualTo(2));
        Assert.That(report.Coverage.ReachableCityPairs, Is.GreaterThan(0));
        Assert.That(report.Issues.Any(issue => issue.Code == "location-coordinate-missing"), Is.True);
    }

    [Test]
    public void MissingRequestedClanStart_BlocksPlayableValidation()
    {
        var report = WorldKitValidator.ValidateModRoot(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "mod"),
            "TestWorld",
            new WorldKitValidationOptions
            {
                ActiveClans = new[] { "Sirians", "StormGiants" }
            });

        Assert.That(report.IsValid, Is.False);
        Assert.That(report.Issues.Any(issue =>
            issue.Code == "start-clan-missing-city"
            && issue.Message.Contains("StormGiants")), Is.True);
    }

    [Test]
    public void OverlappingCityFootprints_ReturnCoordinateError()
    {
        var modRoot = CreateFixture(
            "OverlapWorld",
            CreateMap(5, 5),
            CityJson(
                City("Alpha", "Sirians", 1, 2),
                City("Beta", "LordBane", 2, 2)),
            "[]");

        var report = WorldKitValidator.ValidateModRoot(modRoot, "OverlapWorld", new WorldKitValidationOptions { RequestedPlayers = 2 });

        Assert.That(report.IsValid, Is.False);
        Assert.That(report.Issues.Any(issue =>
            issue.Code == "city-footprint-overlap"
            && issue.X.HasValue
            && issue.Y.HasValue), Is.True);
    }

    [Test]
    public void UnknownTerrainAndMissingTiles_ReturnActionableErrors()
    {
        var tiles = new[]
        {
            new { X = 0, Y = 0, TerrainShortName = "Grass" },
            new { X = 1, Y = 1, TerrainShortName = "Bogus" }
        };
        var map = JsonConvert.SerializeObject(new { Name = "BrokenWorld", Tiles = tiles });
        var modRoot = CreateFixture(
            "BrokenWorld",
            map,
            CityJson(City("Alpha", "Sirians", 0, 1)),
            "[]");

        var report = WorldKitValidator.ValidateModRoot(modRoot, "BrokenWorld");

        Assert.That(report.IsValid, Is.False);
        Assert.That(report.Issues.Any(issue => issue.Code == "map-tile-terrain-unknown"), Is.True);
        Assert.That(report.Issues.Any(issue => issue.Code == "map-tile-missing"), Is.True);
    }

    [Test]
    public void LocationOutOfBounds_ReturnsCoordinateError()
    {
        var modRoot = CreateFixture(
            "LocationWorld",
            CreateMap(4, 4),
            CityJson(City("Alpha", "Sirians", 1, 2)),
            "[{\"ShortName\":\"FarRuins\",\"DisplayName\":\"Far Ruins\",\"Kind\":\"Ruins\",\"Terrain\":\"Ruins\",\"X\":99,\"Y\":1}]");

        var report = WorldKitValidator.ValidateModRoot(modRoot, "LocationWorld");

        Assert.That(report.IsValid, Is.False);
        Assert.That(report.Issues.Any(issue =>
            issue.Code == "location-coordinate-out-of-bounds"
            && issue.X == 99
            && issue.Y == 1), Is.True);
    }

    static string CreateFixture(string worldId, string mapJson, string cityJson, string locationJson)
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "worldkit-fixture-" + Guid.NewGuid().ToString("N"));
        Write(root, "Terrain.json",
            "[" +
            "{\"ShortName\":\"Grass\",\"DisplayName\":\"Grass\",\"AllowWalk\":true,\"AllowFlight\":true,\"AllowFloat\":false,\"Movement\":2}," +
            "{\"ShortName\":\"Ruins\",\"DisplayName\":\"Ruins\",\"AllowWalk\":true,\"AllowFlight\":true,\"AllowFloat\":false,\"Movement\":2}" +
            "]");
        Write(root, "Clan.json",
            "[" +
            "{\"ShortName\":\"Sirians\",\"DisplayName\":\"The Sirians\"}," +
            "{\"ShortName\":\"LordBane\",\"DisplayName\":\"Lord Bane\"}," +
            "{\"ShortName\":\"StormGiants\",\"DisplayName\":\"Storm Giants\"}" +
            "]");
        Write(root, "Army.json", "[{\"ShortName\":\"LightInfantry\",\"DisplayName\":\"Light Infantry\"}]");
        Write(root, Path.Combine("Worlds", worldId, "Map.json"), mapJson);
        Write(root, Path.Combine("Worlds", worldId, "City.json"), cityJson);
        Write(root, Path.Combine("Worlds", worldId, "Location.json"), locationJson);
        return root;
    }

    static string CreateMap(int width, int height)
    {
        var tiles = new List<object>();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                tiles.Add(new { X = x, Y = y, TerrainShortName = "Grass" });
            }
        }

        return JsonConvert.SerializeObject(new { Name = "FixtureWorld", Tiles = tiles });
    }

    static object City(string shortName, string clanName, int x, int y)
    {
        return new
        {
            ShortName = shortName,
            DisplayName = shortName,
            X = x,
            Y = y,
            ClanName = clanName,
            Defense = 4,
            Income = 20,
            ProductionInfos = new[] { new { ArmyInfoName = "LightInfantry", TurnsToProduce = 1, Upkeep = 4, Moves = 10, Strength = 3 } }
        };
    }

    static string CityJson(params object[] cities)
    {
        return JsonConvert.SerializeObject(cities);
    }

    static void Write(string root, string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
