using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Core.Boons;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class HeroTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    [SetUp]
    public void Setup()
    {
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
    }

    [Test]
    public void Take_Artifact_HeroCombatItem()
    {
        // Assemble
        // Set up location            
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, tile);

        // Set up boon
        var artifact = FindArtifact("Firesword");
        var boon = new ArtifactBoon(artifact);
        location.Boon = boon;

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });
        var success = tile.Location.Search(new List<Army> { hero }, out var result);
        Assert.That(success, Is.True, "Test setup failed");

        // Act            
        hero.Take(tile.Items);

        // Assert
        Assert.That(hero.HasItems(), Is.True, "Hero should have taken the item.");
        Assert.That(hero.Items.Count, Is.EqualTo(1), "Hero does not have correct items.");
        Assert.That(hero.Items[0] is Artifact, Is.True);
        Assert.That(tile.HasItems(), Is.False, "Tile still has item(s)");

        var actualArtifactName = hero.Items[0].ShortName;
        Assert.That(actualArtifactName, Is.EqualTo(artifact.ShortName), "Did not take the correct object.");
        Assert.That(hero.Strength, Is.EqualTo(5 + 1), "Hero did not get the correct Combat Bonus.");
    }

    [Test]
    public void Take_Artifact_HeroTwoCombatItems()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];

        // Set up artifacts
        var artifact1 = FindArtifact("Firesword");
        tile.AddItem(artifact1);
        var artifact2 = FindArtifact("Icesword");
        tile.AddItem(artifact2);

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });

        // Act            
        hero.Take(tile.Items[0]); // Taking one removes one from the tile
        hero.Take(tile.Items[0]); // So, need to grab index 0 twice (or take all)

        // Assert
        Assert.That(hero.HasItems(), Is.True, "Hero should have taken the item.");
        Assert.That(hero.Items.Count, Is.EqualTo(2), "Hero does not have correct items.");
        Assert.That(hero.Items[0] is Artifact, Is.True);
        Assert.That(hero.Items[1] is Artifact, Is.True);
        Assert.That(tile.HasItems(), Is.False, "Tile still has item(s)");

        var actualArtifactName1 = hero.Items[0].ShortName;
        var actualArtifactName2 = hero.Items[1].ShortName;
        Assert.That(actualArtifactName1, Is.EqualTo(artifact1.ShortName), "Did not take the correct object.");
        Assert.That(actualArtifactName2, Is.EqualTo(artifact2.ShortName), "Did not take the correct object.");
        Assert.That(hero.Strength, Is.EqualTo(5 + 1 + 1), "Hero did not get the correct Combat Bonus.");
    }

    private static Artifact FindArtifact(string artifactName)
    {
        var artifactInfos = new List<Artifact>(
            ModFactory.LoadArtifacts(ModFactory.ModPath));
        return artifactInfos.Find(a => a.ShortName == artifactName) ?? throw new InvalidOperationException($"Artifact '{artifactName}' not found.");
    }
}
