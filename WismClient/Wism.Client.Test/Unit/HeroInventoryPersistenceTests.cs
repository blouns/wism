using System;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class HeroInventoryPersistenceTests
{
    [SetUp]
    public void Setup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void RoundTrip_PreservesEveryItemAndDoesNotReapplyBonuses(int count)
    {
        var tile = World.Current.Map[2, 2];
        var hero = Game.Current.Players[0].HireHero(tile);
        var baseStrength = hero.Strength;
        var names = new[] { "Firesword", "Icesword" }.Take(count).ToArray();
        foreach (var name in names)
        {
            var artifact = Artifact.Create(ModFactory.FindArtifactInfo(name));
            tile.AddItem(artifact);
            hero.Take(artifact);
        }
        var strength = hero.Strength;
        var savedItems = JsonConvert.SerializeObject(Game.Current.Snapshot().Players[0].Armies[0].Artifacts);

        for (var round = 0; round < 3; round++)
        {
            var json = JsonConvert.SerializeObject(Game.Current.Snapshot());
            GameFactory.Load(JsonConvert.DeserializeObject<GameEntity>(json));
            hero = (Hero)Game.Current.Players[0].GetArmies().Single();
            Assert.Multiple(() =>
            {
                Assert.That(hero.Items?.Select(item => item.ShortName).ToArray() ?? Array.Empty<string>(), Is.EqualTo(names));
                Assert.That(hero.Strength, Is.EqualTo(strength));
                Assert.That(JsonConvert.SerializeObject(Game.Current.Snapshot().Players[0].Armies[0].Artifacts), Is.EqualTo(savedItems));
                Assert.That(hero.Tile.HasItems(), Is.False, "Carried items must not also appear on the ground.");
            });
        }

        hero.DropAll();
        Assert.Multiple(() =>
        {
            Assert.That(hero.HasItems(), Is.False);
            Assert.That(hero.Strength, Is.EqualTo(baseStrength));
            Assert.That(hero.Tile.Items?.Select(item => item.ShortName).ToArray() ?? Array.Empty<string>(), Is.EqualTo(names));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Load_AbsentOrEmptyInventoryCanTakeAnItem(bool empty)
    {
        Game.Current.Players[0].HireHero(World.Current.Map[2, 2]);
        var snapshot = Game.Current.Snapshot();
        snapshot.Players[0].Armies[0].Artifacts = empty ? Array.Empty<ArtifactEntity>() : null;
        GameFactory.Load(snapshot);
        var hero = (Hero)Game.Current.Players[0].GetArmies().Single();
        Assert.That(hero.HasItems(), Is.False);
        var artifact = Artifact.Create(ModFactory.FindArtifactInfo("Firesword"));
        hero.Tile.AddItem(artifact);
        hero.TakeAll();
        Assert.That(hero.Items, Has.Count.EqualTo(1));
        Assert.DoesNotThrow(() => Game.Current.Snapshot());
    }
}
