using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Core.Boons;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Searchables;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class LocationTests
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
    public void Add_Tomb()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("CryptKeeper");

        // Act
        World.Current.AddLocation(location, tile);

        // Assert
        Assert.That(tile.HasLocation(), Is.True, "No location found.");
        Assert.That(tile.Location.DisplayName, Is.EqualTo("Crypt Keeper's Lair"));
        Assert.That(tile.Location.Kind, Is.EqualTo("Tomb"));
    }

    [Test]
    public void Add_Ruins()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("Stonehenge");

        // Act
        World.Current.AddLocation(location, tile);

        // Assert
        Assert.That(tile.HasLocation(), Is.True, "No location found.");
        Assert.That(tile.Location.DisplayName, Is.EqualTo("Stonehenge"));
        Assert.That(tile.Location.Kind, Is.EqualTo("Ruins"));
    }

    [Test]
    public void Add_Library()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("Suzzallo");

        // Act
        World.Current.AddLocation(location, tile);

        // Assert
        Assert.That(tile.HasLocation(), Is.True, "No location found.");
        Assert.That(tile.Location.DisplayName, Is.EqualTo("Suzzallo"));
        Assert.That(tile.Location.Kind, Is.EqualTo("Library"));
    }

    [Test]
    public void Add_Sage()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("SagesHut");

        // Act
        World.Current.AddLocation(location, tile);

        // Assert
        Assert.That(tile.HasLocation(), Is.True, "No location found.");
        Assert.That(tile.Location.DisplayName, Is.EqualTo("Sage's Hut"));
        Assert.That(tile.Location.Kind, Is.EqualTo("Sage"));
    }

    [Test]
    public void Add_Temple()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("TempleDog");

        // Act
        World.Current.AddLocation(location, tile);

        // Assert
        Assert.That(tile.HasLocation(), Is.True, "No location found.");
        Assert.That(tile.Location.DisplayName, Is.EqualTo("Temple of the Dog"));
        Assert.That(tile.Location.Kind, Is.EqualTo("Temple"));
    }

    [Test]
    public void Search_Tomb_HeroUnexplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, tile);
        MapBuilder.AllocateBoons(World.Current.GetLocations());
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });
        var boon = tile.Location.Boon;

        // Act
        var success = tile.Location.Search(new List<Army> { hero }, out var result);

        // Assert
        Assert.That(success, Is.True, "Failed to search the location.");
        Assert.That(tile.Location.Boon, Is.Null, "Boon not redeemed.");
        Assert.That(boon.Result, Is.Not.Null, "Did not get a boon.");
    }

    [Test]
    public void Search_Ruins_HeroUnexplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("Stonehenge");
        World.Current.AddLocation(location, tile);
        MapBuilder.AllocateBoons(World.Current.GetLocations());
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });
        var boon = tile.Location.Boon;

        // Act
        var success = tile.Location.Search(new List<Army> { hero }, out var result);

        // Assert            
        Assert.That(success, Is.True, "Failed to search the location.");
        Assert.That(tile.Location.Boon, Is.Null, "Boon not redeemed.");
        Assert.That(boon.Result, Is.Not.Null, "Did not get a boon.");
    }

    [Test]
    public void Search_Library_HeroUnexplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("Suzzallo");
        World.Current.AddLocation(location, tile);
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        var expectedKnowledge = "Lord Lowenbrau will return!";

        // Act
        var success = tile.Location.Search(new List<Army> { hero }, out var result);

        // Assert
        var knowledge = result as string;
        Assert.That(success, Is.True, "Failed to search the location.");
        Assert.That(knowledge, Is.EqualTo(expectedKnowledge), "Did not find the expected knowledge.");
    }

    [Test]
    public void Search_Sage_HeroUnexplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("SagesHut");
        World.Current.AddLocation(location, tile);
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);

        // Act
        var success = tile.Location.Search(new List<Army> { hero }, out var result);

        // Assert
        var gold = result as int?;
        Assert.That(success, Is.True, "Failed to search the location.");
        Assert.That(gold >= SearchSage.MinGold && gold <= SearchSage.MaxGold, Is.True, "Did not find the expected item.");
    }

    [Test]
    public void Search_Temple_HeroUnexplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("TempleDog");
        World.Current.AddLocation(location, tile);
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        var expectedStrength = hero.Strength + 1;

        // Act
        var success = tile.Location.Search(new List<Army> { hero }, out var result);

        // Assert
        var blessed = result as int?;
        Assert.That(success, Is.True, "Failed to search the location.");
        Assert.That(hero.Strength, Is.EqualTo(expectedStrength), "Army was not blessed.");
        Assert.That(blessed, Is.EqualTo(1), "Expected one army to be blessed.");
    }

    [Test]
    public void Search_Tomb_HeroExplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, tile);
        MapBuilder.AllocateBoons(World.Current.GetLocations());
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });

        var success = tile.Location.Search(new List<Army> { hero }, out var result);
        Assert.That(success, Is.True, "Setup failed");

        // Act
        success = tile.Location.Search(new List<Army> { hero }, out result);

        // Assert
        Assert.That(success, Is.False, "Successfully searched an explored location.");
        Assert.That(result, Is.Null, "Found an item in explored location.");
    }

    [Test]
    public void Search_Ruins_HeroExplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("Stonehenge");
        World.Current.AddLocation(location, tile);
        MapBuilder.AllocateBoons(World.Current.GetLocations());
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        Game.Current.SelectArmies(new List<Army> { hero });

        var success = tile.Location.Search(new List<Army> { hero }, out var result);
        Assert.That(success, Is.True, "Setup failed");

        // Act
        success = tile.Location.Search(new List<Army> { hero }, out result);

        // Assert
        Assert.That(success, Is.False, "Successfully searched an explored location.");
        Assert.That(result, Is.Null, "Found an item in explored location.");
    }

    [Test]
    public void Search_Library_HeroExplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("Suzzallo");
        World.Current.AddLocation(location, tile);
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        var success = tile.Location.Search(new List<Army> { hero }, out var result);
        Assert.That(success, Is.True, "Setup failed");
        var expectedKnowledge = "Branally the Fist is the great creator!";

        // Act            
        success = tile.Location.Search(new List<Army> { hero }, out result);

        // Assert
        var knowledge = result as string;
        Assert.That(success, Is.True, "Failed to search the location.");
        Assert.That(knowledge, Is.EqualTo(expectedKnowledge), "Did not find the expected knowledge.");
    }

    [Test]
    public void Search_Sage_HeroExplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("SagesHut");
        World.Current.AddLocation(location, tile);
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        var success = tile.Location.Search(new List<Army> { hero }, out var result);
        var originalGold = player1.Gold;
        Assert.That(success, Is.True, "Setup failed");

        // Act
        success = tile.Location.Search(new List<Army> { hero }, out result);

        // Assert
        Assert.That(success, Is.True, "Sages can always be explored.");
        Assert.That(result, Is.Null, "Found an item in explored location.");
        Assert.That(player1.Gold, Is.EqualTo(originalGold), "Should not receive a second gem.");
    }

    [Test]
    public void Search_Temple_HeroExplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("TempleDog");
        World.Current.AddLocation(location, tile);
        var player1 = Game.Current.Players[0];
        var hero = player1.HireHero(tile);
        var expectedStrength = hero.Strength + 1;
        var success = tile.Location.Search(new List<Army> { hero }, out var result);
        Assert.That(success, Is.True, "Setup failed");

        // Act
        success = tile.Location.Search(new List<Army> { hero }, out result);

        // Assert
        var blessed = result as int?;
        Assert.That(success, Is.False, "Successfully searched an explored location.");
        Assert.That(hero.Strength, Is.EqualTo(expectedStrength), "Army was blessed twice.");
        Assert.That(blessed, Is.EqualTo(0), "Expected one army to be blessed.");
    }

    [Test]
    public void Search_Tomb_LightInfantryUnexplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("CryptKeeper");
        World.Current.AddLocation(location, tile);
        var player1 = Game.Current.Players[0];
        var lightInfantry = player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        MapBuilder.AllocateBoons(World.Current.GetLocations());

        // Act
        var success = tile.Location.Search(new List<Army> { lightInfantry }, out var result);

        // Assert
        Assert.That(success, Is.False, "Army searched a location only for heros.");
        Assert.That(result, Is.Null, "Army found an item only for heros.");
    }

    [Test]
    public void Search_Ruins_LightInfantryUnexplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("Stonehenge");
        World.Current.AddLocation(location, tile);
        var player1 = Game.Current.Players[0];
        var lightInfantry = player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        MapBuilder.AllocateBoons(World.Current.GetLocations());

        // Act
        var success = tile.Location.Search(new List<Army> { lightInfantry }, out var result);

        // Assert
        Assert.That(success, Is.False, "Army searched a location only for heros.");
        Assert.That(result, Is.Null, "Army found an item only for heros.");
    }

    [Test]
    public void Search_Library_LightInfantryUnexplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("Suzzallo");
        World.Current.AddLocation(location, tile);
        var player1 = Game.Current.Players[0];
        var lightInfantry = player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        // Act
        var success = tile.Location.Search(new List<Army> { lightInfantry }, out var result);

        // Assert
        Assert.That(success, Is.False, "Army searched a location only for heros.");
        Assert.That(result, Is.Null, "Army found an item only for heros.");
    }

    [Test]
    public void Search_Sage_LightInfantryUnexplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("SagesHut");
        World.Current.AddLocation(location, tile);
        var player1 = Game.Current.Players[0];
        var lightInfantry = player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        // Act
        var success = tile.Location.Search(new List<Army> { lightInfantry }, out var result);

        // Assert
        Assert.That(success, Is.False, "Army searched a location only for heros.");
        Assert.That(result, Is.Null, "Army found an item only for heros.");
    }

    [Test]
    public void Search_Temple_LightInfantryUnexplored()
    {
        // Assemble
        var tile = World.Current.Map[2, 2];
        var location = MapBuilder.FindLocation("TempleDog");
        World.Current.AddLocation(location, tile);
        var player1 = Game.Current.Players[0];
        var lightInfantry = player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        var expectedStrength = lightInfantry.Strength + 1;

        // Act
        var success = tile.Location.Search(new List<Army> { lightInfantry }, out var result);

        // Assert
        var blessed = result as int?;
        Assert.That(success, Is.True, "Failed to search the location.");
        Assert.That(lightInfantry.Strength, Is.EqualTo(expectedStrength), "Army was not blessed.");
        Assert.That(blessed, Is.EqualTo(1), "Expected one army to be blessed.");
    }
}
