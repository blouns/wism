using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Wism.Client.AI.Framework;
using Wism.Client.AI.Services;
using Wism.Client.AI.Strategic;
using Wism.Client.Commands.Cities;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class LivePlaytestRegressionTests
{
    [TestCase(true, true, false, "HeavyInfantry")]
    [TestCase(false, true, false, "LightInfantry")]
    [TestCase(true, false, false, "LightInfantry")]
    [TestCase(false, true, true, "HeavyInfantry")]
    public void ProductionChoice_UsesLocalStatsAndExposure(bool defended, bool supported, bool distant, string expected)
    {
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
        var map = new Tile[20, 12];
        for (var x = 0; x < 20; x++)
            for (var y = 0; y < 12; y++)
                map[x, y] = new Tile { Terrain = MapBuilder.TerrainKinds["Grass"] };
        MapBuilder.AffixMapObjects(map);
        World.CreateWorld(map);
        var player = Game.Current.GetCurrentPlayer();
        var enemy = Game.Current.Players.First(p => p != player);
        player.IsHuman = false;
        var roster = new[] {
            new ProductionInfo { ArmyInfoName = "LightInfantry", Moves = 10, Strength = 3, TurnsToProduce = 1, Upkeep = 4 },
            new ProductionInfo { ArmyInfoName = "HeavyInfantry", Moves = distant ? 20 : 8, Strength = 5, TurnsToProduce = 2, Upkeep = 4 },
            new ProductionInfo { ArmyInfoName = "Cavalry", Moves = distant ? 1 : 16, Strength = 6, TurnsToProduce = 6, Upkeep = 8 } };
        MapBuilder.AddCitiesFromInfos(World.Current, new List<CityInfo> {
            new CityInfo { ShortName = "ProductionProbe", DisplayName = "Production Probe", ClanName = player.Clan.ShortName,
                X = 2, Y = 2, Income = 30, Defense = 4, ProductionInfos = roster },
            new CityInfo { ShortName = "FortressProbe", DisplayName = "Fortress Probe", ClanName = enemy.Clan.ShortName,
                X = distant ? 13 : 6, Y = 2, Income = 30, Defense = 4, ProductionInfos = roster } });
        var source = World.Current.FindCity("ProductionProbe");
        if (supported) player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), source.Tile);
        if (defended) enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.FindCity("FortressProbe").Tile);
        var provider = TestUtilities.CreateControllerProvider();
        var command = new ProductionModule(provider.CityController, TestUtilities.CreateLogFactory().CreateLogger())
            .GenerateCommands(World.Current).OfType<StartProductionCommand>().Single(c => c.ProductionCity == source);
        Assert.That(command.ArmyInfo.ShortName, Is.EqualTo(expected));
    }

    [Test]
    public void SuppliedCityDefinition_DoesNotReplaceTheCachedModTemplate()
    {
        Game.CreateDefaultGame();
        var template = MapBuilder.FindCityInfo("Marthos");
        var before = JToken.FromObject(template);
        var definition = new CityInfo { ShortName = "Marthos", DisplayName = "Custom", ClanName = "Sirians",
            X = 2, Y = 2, Income = 99, Defense = 8, ProductionInfos = new[] {
                new ProductionInfo { ArmyInfoName = "Cavalry", Moves = 16, Strength = 6, TurnsToProduce = 6, Upkeep = 8 } } };
        MapBuilder.AddCitiesFromInfos(World.Current, new List<CityInfo> { definition });
        Assert.That(JToken.DeepEquals(before, JToken.FromObject(MapBuilder.FindCityInfo("Marthos"))), Is.True);
    }

    [Test]
    public void CitySnapshot_ProductionDefinitionsAreIndependentInBothDirections()
    {
        var provider = TestUtilities.CreateControllerProvider();
        TestUtilities.NewGame(provider, TestUtilities.DefaultTestWorld);
        var city = Game.Current.Players[0].Capitol;
        var snapshot = Game.Current.Snapshot();
        var saved = snapshot.World.Cities.Single(c => c.CityShortName == city.ShortName).Definition;
        var original = saved.ProductionInfos[0].Strength;
        city.Barracks.GetProductionKinds()[0].Strength = original + 1;
        Assert.That(saved.ProductionInfos[0].Strength, Is.EqualTo(original), "Live state cannot rewrite a checkpoint.");
        new Wism.Client.Commands.Games.LoadGameCommand(provider.GameController, snapshot).Execute();
        World.Current.FindCity(city.ShortName).Barracks.GetProductionKinds()[0].Strength = original + 2;
        Assert.That(saved.ProductionInfos[0].Strength, Is.EqualTo(original), "Reloaded state cannot alias a checkpoint.");
    }

    [Test]
    public void LegacyCitySnapshot_WithoutDefinitionStillLoadsMatchingModRoster()
    {
        var provider = TestUtilities.CreateControllerProvider();
        TestUtilities.NewGame(provider, TestUtilities.DefaultTestWorld);
        var snapshot = Game.Current.Snapshot();
        foreach (var city in snapshot.World.Cities) city.Definition = null;
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(snapshot);
        var legacy = Newtonsoft.Json.JsonConvert.DeserializeObject<Wism.Client.Data.Entities.GameEntity>(json);
        Assert.That(new Wism.Client.Commands.Games.LoadGameCommand(provider.GameController, legacy).Execute(),
            Is.EqualTo(Wism.Client.Controllers.ActionState.Succeeded));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnsupportedBuildRisk_HeroAloneDoesNotReplaceAStandingArmy(bool standingArmy)
    {
        var provider = TestUtilities.CreateControllerProvider();
        TestUtilities.NewGame(provider, TestUtilities.DefaultTestWorld);
        var player = Game.Current.Players[0];
        foreach (var army in player.GetArmies().ToList()) army.Kill();
        player.HireHero(player.Capitol.Tile);
        if (standingArmy) player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), player.Capitol.Tile);
        var quick = player.Capitol.Barracks.GetProductionKinds().Min(info => info.TurnsToProduce);
        var slow = new ProductionInfo { ArmyInfoName = "Cavalry", TurnsToProduce = quick + 5 };
        Assert.That(ProductionModule.UnsupportedBuildDelay(player.Capitol, slow), Is.EqualTo(standingArmy ? 0 : 5));
    }

    [Test]
    public void CityBuilder_UsesSuppliedProductionInsteadOfCachedTemplate()
    {
        Game.CreateDefaultGame();
        var production = new ProductionInfo { ArmyInfoName = "Cavalry", Moves = 16, Strength = 6, TurnsToProduce = 6, Upkeep = 8 };
        var info = new CityInfo { ShortName = "Marthos", DisplayName = "Marthos", ClanName = "Sirians", X = 2, Y = 2,
            Income = 33, Defense = 6, ProductionInfos = new[] { production } };
        MapBuilder.AddCitiesFromInfos(World.Current, new List<CityInfo> { info });
        var city = World.Current.Map[2, 2].City;
        Assert.That(city.Barracks.GetProductionKinds().Single().TurnsToProduce, Is.EqualTo(6));
        Assert.That(city.Barracks.GetProductionKinds().Single().ArmyInfoName, Is.EqualTo("Cavalry"));
        Assert.That(city.Income, Is.EqualTo(33));
        Assert.That(city.Defense, Is.EqualTo(6));
        var snapshot = Game.Current.Snapshot();
        MapBuilder.Initialize(ModFactory.ModPath, ModFactory.WorldPath);
        var provider = TestUtilities.CreateControllerProvider();
        var result = new Wism.Client.Commands.Games.LoadGameCommand(provider.GameController, snapshot).Execute();
        Assert.That(result, Is.EqualTo(Wism.Client.Controllers.ActionState.Succeeded));
        Assert.That(World.Current.Map[2, 2].City.Barracks.GetProductionKinds().Single().TurnsToProduce, Is.EqualTo(6),
            "Checkpoint reload cannot substitute the cached mod roster for the generated six-turn fixture.");
    }

    [Test]
    public void Illuria_All80AuthoredProductionRostersReachBothRuntimeCopies()
    {
        var root = FindRepository();
        var original = Path.Combine(root, "WismUnity/Assets/Mod/Worlds/Illuria/City.json");
        Assert.That(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(original))),
            Is.EqualTo("F96E90D372F77F9E2FC7BDD87F26113BE9801FA316EC1393133C8AE5A5741962"),
            "The independent authored roster baseline must not silently change with runtime data.");
        var baseline = JArray.Parse(File.ReadAllText(original));
        var aliases = new Dictionary<string, string> { ["Beleri"] = "Bereri", ["Dunethel"] = "Dunethal", ["Herueth"] = "Hereuth" };
        foreach (var relative in new[] { "WismClient/Wism.Client.Core/mod/Worlds/Illuria/City.json", "WismUnity/Assets/Plugins/WismClient/Mods/Worlds/Illuria/City.json" })
        {
            var actual = JArray.Parse(File.ReadAllText(Path.Combine(root, relative)));
            Assert.That(actual.Count, Is.EqualTo(80));
            var matched = new HashSet<string>();
            Assert.Multiple(() =>
            {
                foreach (var city in actual)
                {
                    var name = (string)city["ShortName"];
                    var sourceName = aliases.TryGetValue(name, out var alias) ? alias : name;
                    var source = baseline.Single(item => string.Equals((string)item["ShortName"], sourceName, StringComparison.OrdinalIgnoreCase));
                    Assert.That(matched.Add(sourceName), Is.True, "Each authored city must map exactly once.");
                    Assert.That(JToken.DeepEquals(city["ProductionInfos"], source["ProductionInfos"]), Is.True,
                        relative + ": " + name + " must retain every ordered slot and production stat.");
                }
            });
            Assert.That(matched.Count, Is.EqualTo(80));
        }
    }

    [Test]
    public void EmptyCapital_ProducesQuickLocalDefenderInsteadOfSlowAssaultUnit()
    {
        var provider = TestUtilities.CreateControllerProvider();
        TestUtilities.NewGame(provider, TestUtilities.DefaultTestWorld);
        TestUtilities.StartTurn(provider);
        var player = Game.Current.GetCurrentPlayer();
        player.IsHuman = false;
        foreach (var army in player.GetArmies().Where(a => !(a is Hero)).ToList()) army.Kill();
        var commander = WarlordsClassicAiFactory.CreateCommandProvider(provider, TestUtilities.CreateLogFactory().CreateLogger());
        commander.GenerateCommands();
        var command = commander.GetBufferedCommands().OfType<StartProductionCommand>().Single(c => c.ProductionCity == player.Capitol);
        var chosen = player.Capitol.Barracks.GetProductionKinds().Single(p => p.ArmyInfoName == command.ArmyInfo.ShortName);
        var quickest = player.Capitol.Barracks.GetProductionKinds().Where(p => ArmyInfo.GetArmyInfo(p.ArmyInfoName).CanWalk).Min(p => p.TurnsToProduce);
        Assert.That(chosen.TurnsToProduce, Is.EqualTo(quickest));
        Assert.That(command.DestinationCity == null || command.DestinationCity == player.Capitol, Is.True);
    }

    private static string FindRepository()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "WismUnity"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("WISM source checkout required for distribution contract.");
    }
}
