using System;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class LoadPreparationTests
{
    [SetUp]
    public void Setup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
    }

    [TestCase("valid")]
    [TestCase("random")]
    [TestCase("terrain")]
    public void ValidateLoad_PreservesLiveStateOnSuccessAndFailure(string failure)
    {
        var game = Game.Current;
        var world = World.Current;
        var random = game.Random;
        var id = ArmyFactory.LastId;
        var snapshot = game.Snapshot();
        snapshot.LastArmyId = id + 100;
        if (failure == "random") snapshot.Random = null;
        if (failure == "terrain") snapshot.World.Tiles[0].TerrainShortName = "MissingTerrain";

        if (failure != "valid") Assert.Catch(() => GameFactory.ValidateLoad(snapshot));
        else Assert.DoesNotThrow(() => GameFactory.ValidateLoad(snapshot));

        Assert.That(Game.Current, Is.SameAs(game));
        Assert.That(World.Current, Is.SameAs(world));
        Assert.That(Game.Current.Random, Is.SameAs(random));
        Assert.That(ArmyFactory.LastId, Is.EqualTo(id));
        Assert.DoesNotThrow(() => GameFactory.Load(game.Snapshot()));
        Assert.That(Game.Current, Is.Not.SameAs(game), "A later real load still publishes.");
    }
}
