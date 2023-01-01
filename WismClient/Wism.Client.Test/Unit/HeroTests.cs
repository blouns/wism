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
        Assert.IsTrue(success, "Test setup failed");

        // Act            
        hero.Take(tile.Items);

        // Assert
        Assert.IsTrue(hero.HasItems(), "Hero should have taken the item.");
        Assert.AreEqual(1, hero.Items.Count, "Hero does not have correct items.");
        Assert.IsTrue(hero.Items[0] is Artifact);
        Assert.IsFalse(tile.HasItems(), "Tile still has item(s)");

        var actualArtifactName = hero.Items[0].ShortName;
        Assert.AreEqual(artifact.ShortName, actualArtifactName, "Did not take the correct object.");
        Assert.AreEqual(5 + 1, hero.Strength, "Hero did not get the correct Combat Bonus.");
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
        Assert.IsTrue(hero.HasItems(), "Hero should have taken the item.");
        Assert.AreEqual(2, hero.Items.Count, "Hero does not have correct items.");
        Assert.IsTrue(hero.Items[0] is Artifact);
        Assert.IsTrue(hero.Items[1] is Artifact);
        Assert.IsFalse(tile.HasItems(), "Tile still has item(s)");

        var actualArtifactName1 = hero.Items[0].ShortName;
        var actualArtifactName2 = hero.Items[1].ShortName;
        Assert.AreEqual(artifact1.ShortName, actualArtifactName1, "Did not take the correct object.");
        Assert.AreEqual(artifact2.ShortName, actualArtifactName2, "Did not take the correct object.");
        Assert.AreEqual(5 + 1 + 1, hero.Strength, "Hero did not get the correct Combat Bonus.");
    }

    [Test]
    public void Take_Artifact_HeroCommandItem()
    {
        // Assemble
        // Set up location            
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, tile);

        // Set up boon
        var artifact = FindArtifact("StaffOfRuling");
        var boon = new ArtifactBoon(artifact);
        location.Boon = boon;

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });
        var success = tile.Location.Search(new List<Army> { hero }, out var result);
        Assert.IsTrue(success, "Test setup failed");

        // Act            
        hero.Take(tile.Items);

        // Assert
        Assert.IsTrue(hero.HasItems(), "Hero should have taken the item.");
        Assert.AreEqual(1, hero.Items.Count, "Hero does not have correct items.");
        Assert.IsTrue(hero.Items[0] is Artifact);
        Assert.IsFalse(tile.HasItems(), "Tile still has item(s)");

        var actualArtifactName = hero.Items[0].ShortName;
        Assert.AreEqual(artifact.ShortName, actualArtifactName, "Did not take the correct object.");
        Assert.AreEqual(3, hero.GetCommandBonus(), "Hero did not get the correct Command Bonus.");
    }

    [Test]
    public void Take_Artifact_HeroTwoCommandItems()
    {
        // Assemble
        // Set up location            
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, tile);

        // Set up boon
        var artifact1 = FindArtifact("StaffOfRuling");
        tile.AddItem(artifact1);
        var artifact2 = FindArtifact("HornOfAges");
        tile.AddItem(artifact2);

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });

        // Act            
        hero.Take(tile.Items);

        // Assert
        Assert.IsTrue(hero.HasItems(), "Hero should have taken the item.");
        Assert.AreEqual(2, hero.Items.Count, "Hero does not have correct items.");
        Assert.IsTrue(hero.Items[0] is Artifact);
        Assert.IsTrue(hero.Items[1] is Artifact);
        Assert.IsFalse(tile.HasItems(), "Tile still has item(s)");

        var actualArtifactName1 = hero.Items[0].ShortName;
        var actualArtifactName2 = hero.Items[1].ShortName;
        Assert.AreEqual(artifact1.ShortName, actualArtifactName1, "Did not take the correct object.");
        Assert.AreEqual(artifact2.ShortName, actualArtifactName2, "Did not take the correct object.");
        Assert.AreEqual(3 + 2, hero.GetCommandBonus(), "Hero did not get the correct Command Bonus.");
    }

    [Test]
    public void Take_Artifact_HeroOneCommandOneCombatItem()
    {
        // Assemble
        // Set up location            
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, tile);

        // Set up boon
        var artifact1 = FindArtifact("StaffOfRuling");
        tile.AddItem(artifact1);
        var artifact2 = FindArtifact("Firesword");
        tile.AddItem(artifact2);

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });

        // Act            
        hero.Take(tile.Items);

        // Assert
        Assert.IsTrue(hero.HasItems(), "Hero should have taken the item.");
        Assert.AreEqual(2, hero.Items.Count, "Hero does not have correct items.");
        Assert.IsTrue(hero.Items[0] is Artifact);
        Assert.IsTrue(hero.Items[1] is Artifact);
        Assert.IsFalse(tile.HasItems(), "Tile still has item(s)");

        var actualArtifactName1 = hero.Items[0].ShortName;
        var actualArtifactName2 = hero.Items[1].ShortName;
        Assert.AreEqual(artifact1.ShortName, actualArtifactName1, "Did not take the correct object.");
        Assert.AreEqual(artifact2.ShortName, actualArtifactName2, "Did not take the correct object.");
        Assert.AreEqual(3, hero.GetCommandBonus(), "Hero did not get the correct Command Bonus.");
        Assert.AreEqual(1, hero.GetCombatBonus(), "Hero did not get the correct Command Bonus.");
        Assert.AreEqual(6, hero.Strength, "Hero did not get the correct Command Bonus.");
    }

    [Test]
    public void Take_Nothing_Hero()
    {
        // Assemble
        // Set up hero
        var tile = World.Current.Map[2, 2];
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });

        // Act            
        hero.Take(tile.Items);

        // Assert
        Assert.IsFalse(hero.HasItems(), "Hero should not have items.");
    }

    [Test]
    public void Drop_Artifact_HeroCombatItem()
    {
        // Assemble
        // Set up location            
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, tile);
        var artifact = FindArtifact("Firesword");
        var boon = new ArtifactBoon(artifact);
        location.Boon = boon;

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });
        _ = tile.Location.Search(new List<Army> { hero }, out var result);
        var item = tile.Items[0];
        hero.Take(tile.Items);

        // Act            
        hero.Drop(item);

        // Assert
        Assert.IsFalse(hero.HasItems(), "Hero should have dropped the item.");
        Assert.AreEqual(0, hero.Items.Count, "Hero does not have correct items.");
        Assert.IsTrue(tile.HasItems(), "Tile did not get the item.");
        Assert.IsTrue(tile.Items[0] is Artifact);
        var actualArtifactName = tile.Items[0].ShortName;
        Assert.AreEqual(artifact.ShortName, actualArtifactName, "Did not drop the correct object.");
        Assert.AreEqual(5 + 0, hero.Strength, "Hero did not get the correct Combat Bonus.");
    }

    [Test]
    public void Drop_Artifact_HeroOneCommandOneCombatItem()
    {
        // Assemble
        // Set up artifact            
        var tile = World.Current.Map[2, 2];
        var artifact1 = FindArtifact("StaffOfRuling");
        tile.AddItem(artifact1);
        var artifact2 = FindArtifact("Firesword");
        tile.AddItem(artifact2);

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });
        hero.Take(tile.Items);
        var item1 = hero.Items[0];
        var item2 = hero.Items[1];

        // Act
        hero.Drop(item1);
        hero.Drop(item2);

        // Assert
        Assert.IsFalse(hero.HasItems(), "Hero should have dropped the item.");
        Assert.AreEqual(2, tile.Items.Count, "Tile does not have correct items.");
        Assert.IsTrue(tile.Items[0] is Artifact);
        Assert.IsTrue(tile.Items[1] is Artifact);

        var actualArtifactName1 = tile.Items[0].ShortName;
        var actualArtifactName2 = tile.Items[1].ShortName;
        Assert.AreEqual(artifact1.ShortName, actualArtifactName1, "Did not drop the correct object.");
        Assert.AreEqual(artifact2.ShortName, actualArtifactName2, "Did not drop the correct object.");
        Assert.AreEqual(0, hero.GetCommandBonus(), "Hero did not get the correct Command Bonus.");
        Assert.AreEqual(0, hero.GetCombatBonus(), "Hero did not get the correct Command Bonus.");
        Assert.AreEqual(5 + 0, hero.Strength, "Hero did not get the correct Command Bonus.");
    }

    [Test]
    public void Drop_Artifact_HeroOneOfTwoCommandOneOfTwoCombatItem()
    {
        // Assemble
        // Set up artifacts
        var tile = World.Current.Map[2, 2];
        var artifact1 = FindArtifact("Icesword");
        tile.AddItem(artifact1);
        var artifact2 = FindArtifact("Firesword");
        tile.AddItem(artifact2);
        var artifact3 = FindArtifact("StaffOfRuling");
        tile.AddItem(artifact3);
        var artifact4 = FindArtifact("HornOfAges");
        tile.AddItem(artifact4);

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });
        hero.Take(tile.Items);
        var item1 = hero.Items[0];
        var item2 = hero.Items[2];

        // Act
        hero.Drop(item1);
        hero.Drop(item2);

        // Assert
        Assert.IsTrue(hero.HasItems(), "Hero should have items.");
        Assert.AreEqual(2, hero.Items.Count, "Hero does not have correct items.");
        Assert.IsTrue(hero.Items[0] is Artifact);
        Assert.IsTrue(hero.Items[1] is Artifact);

        Assert.IsTrue(tile.HasItems(), "Tile should have items.");
        Assert.AreEqual(2, tile.Items.Count, "Hero does not have correct items.");
        Assert.IsTrue(tile.Items[0] is Artifact);
        Assert.IsTrue(tile.Items[1] is Artifact);

        var actualArtifactName2 = hero.Items[0].ShortName;
        var actualArtifactName4 = hero.Items[1].ShortName;
        Assert.AreEqual(artifact2.ShortName, actualArtifactName2, "Did not drop the correct object.");
        Assert.AreEqual(artifact4.ShortName, actualArtifactName4, "Did not drop the correct object.");

        var actualArtifactName1 = tile.Items[0].ShortName;
        var actualArtifactName3 = tile.Items[1].ShortName;
        Assert.AreEqual(artifact1.ShortName, actualArtifactName1, "Did not drop the correct object.");
        Assert.AreEqual(artifact3.ShortName, actualArtifactName3, "Did not drop the correct object.");

        Assert.AreEqual(2, hero.GetCommandBonus(), "Hero did not get the correct Command Bonus.");
        Assert.AreEqual(1, hero.GetCombatBonus(), "Hero did not get the correct Command Bonus.");
        Assert.AreEqual(5 + 1, hero.Strength, "Hero did not get the correct Command Bonus.");
    }

    [Test]
    public void Drop_Artifact_KillHeroOneCommandOneCombatItem()
    {
        // Assemble
        // Set up artifacts
        var tile = World.Current.Map[2, 2];
        var artifact1 = FindArtifact("StaffOfRuling");
        tile.AddItem(artifact1);
        var artifact2 = FindArtifact("Firesword");
        tile.AddItem(artifact2);

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });
        hero.Take(tile.Items);

        // Act
        hero.Kill();

        // Assert
        Assert.IsFalse(hero.HasItems(), "Hero should have dropped the item.");
        Assert.AreEqual(2, tile.Items.Count, "Tile does not have correct items.");
        Assert.IsTrue(tile.Items[0] is Artifact);
        Assert.IsTrue(tile.Items[1] is Artifact);

        var actualArtifactName1 = tile.Items[0].ShortName;
        var actualArtifactName2 = tile.Items[1].ShortName;
        Assert.AreEqual(artifact1.ShortName, actualArtifactName1, "Did not drop the correct object.");
        Assert.AreEqual(artifact2.ShortName, actualArtifactName2, "Did not drop the correct object.");
        Assert.AreEqual(0, hero.GetCommandBonus(), "Hero did not get the correct Command Bonus.");
        Assert.AreEqual(0, hero.GetCombatBonus(), "Hero did not get the correct Command Bonus.");
        Assert.AreEqual(5 + 0, hero.Strength, "Hero did not get the correct Command Bonus.");
        Assert.IsTrue(hero.IsDead, "Hero has been resurrected and is haunting you.");
    }

    [Test]
    public void Take_CompanionCooper_Pass()
    {
        // Assemble
        // Set up location            
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("Doghouse");
        World.Current.AddLocation(location, tile);

        // Set up boon
        var artifact = FindArtifact("Cooper");
        var boon = new ArtifactBoon(artifact);
        location.Boon = boon;

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });
        var success = tile.Location.Search(new List<Army> { hero }, out var result);
        Assert.IsTrue(success, "Test setup failed");

        // Act            
        hero.Take(tile.Items);
        var interaction = hero.GetCompanionInteraction();

        // Assert
        Assert.IsTrue(hero.HasItems(), "Hero should have taken the item.");
        Assert.AreEqual(1, hero.Items.Count, "Hero does not have correct items.");
        Assert.IsTrue(hero.Items[0] is Artifact);
        Assert.IsFalse(tile.HasItems(), "Tile still has item(s)");

        var actualArtifactName = hero.Items[0].ShortName;
        Assert.AreEqual(artifact.ShortName, actualArtifactName, "Did not take the correct object.");
        Assert.AreEqual(1, hero.GetCommandBonus(), "Hero did not get the correct Command Bonus.");
        Assert.AreEqual("Cooper layed down and got fur everywhere.", interaction,
            "Cooper did not have the expected reaction");
    }

    [Test]
    public void Take_CompanionWilson_Pass()
    {
        // Assemble
        // Set up location            
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("Yakima");
        World.Current.AddLocation(location, tile);

        // Set up boon
        var artifact = FindArtifact("Wilson");
        var boon = new ArtifactBoon(artifact);
        location.Boon = boon;

        // Set up hero
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });
        var success = tile.Location.Search(new List<Army> { hero }, out var result);
        Assert.IsTrue(success, "Test setup failed");

        // Act            
        hero.Take(tile.Items);
        var interaction = hero.GetCompanionInteraction();

        // Assert
        Assert.IsTrue(hero.HasItems(), "Hero should have taken the item.");
        Assert.AreEqual(1, hero.Items.Count, "Hero does not have correct items.");
        Assert.IsTrue(hero.Items[0] is Artifact);
        Assert.IsFalse(tile.HasItems(), "Tile still has item(s)");

        var actualArtifactName = hero.Items[0].ShortName;
        Assert.AreEqual(artifact.ShortName, actualArtifactName, "Did not take the correct object.");
        Assert.AreEqual(1, hero.GetCombatBonus(), "Hero did not get the correct Command Bonus.");
        Assert.AreEqual("Wilson sat down and farted.", interaction, "Wilson did not have the expected reaction");
    }

    private static Artifact FindArtifact(string artifactName)
    {
        var artifactInfos = new List<Artifact>(
            ModFactory.LoadArtifacts(ModFactory.ModPath));
        return artifactInfos.Find(a => a.ShortName == artifactName);
    }
}