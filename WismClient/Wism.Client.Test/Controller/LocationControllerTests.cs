using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Controller;

[TestFixture]
public class LocationControllerTests
{
    [Test]
    public void SearchTemple_HeroUnexplored()
    {
        // Assemble
        var locationController = TestUtilities.CreateLocationController();
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
        var player1 = Game.Current.Players[0];
        var location = MapBuilder.FindLocation("TempleDog");
        var tile = World.Current.Map[1, 1];
        World.Current.AddLocation(location, tile);
        Army army = player1.HireHero(tile);
        var armies = new List<Army> { army };

        // Act
        var result = locationController.SearchTemple(armies, location, out var armiesBlessed);

        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(location.Searched);
        Assert.That(armiesBlessed, Is.EqualTo(1));
    }

    [Test]
    public void SearchSage_HeroUnexplored()
    {
        // Assemble
        var locationController = TestUtilities.CreateLocationController();
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
        var player1 = Game.Current.Players[0];
        var location = MapBuilder.FindLocation("SagesHut");
        var tile = World.Current.Map[1, 1];
        World.Current.AddLocation(location, tile);
        Army army = player1.HireHero(tile);
        var armies = new List<Army> { army };
        var initialGold = player1.Gold;

        // Act
        var success = locationController.SearchSage(armies, location, out var gold);

        // Assert
        Assert.IsTrue(success);
        Assert.That(gold + initialGold, Is.EqualTo(player1.Gold));
        Assert.IsTrue(player1.Gold > initialGold);
        Assert.IsTrue(location.Searched);
    }

    [Test]
    public void SearchLibrary_Unexplored()
    {
        // Assemble
        var locationController = TestUtilities.CreateLocationController();
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
        var player1 = Game.Current.Players[0];
        var location = MapBuilder.FindLocation("Suzzallo");
        var tile = World.Current.Map[1, 1];
        World.Current.AddLocation(location, tile);
        Army army = player1.HireHero(tile);
        var armies = new List<Army> { army };
        var expectedKnowledge = "Lord Lowenbrau will return!";

        // Act
        var success = locationController.SearchLibrary(armies, location, out var knowledge);

        // Assert
        Assert.IsTrue(success);
        Assert.That(knowledge, Is.EqualTo(expectedKnowledge));
        Assert.IsTrue(location.Searched);
    }

    [Test]
    public void SearchTomb_Unexplored()
    {
        // Assemble
        var locationController = TestUtilities.CreateLocationController();
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
        var player1 = Game.Current.Players[0];
        var location = MapBuilder.FindLocation("CryptKeeper");
        var tile = World.Current.Map[1, 1];
        World.Current.AddLocation(location, tile);
        TestUtilities.AllocateBoons();
        Army army = player1.HireHero(tile);
        var armies = new List<Army> { army };
        Game.Current.SelectArmies(armies);

        // Act
        var success = locationController.SearchTomb(armies, location, out var boon);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(boon);
        Assert.IsTrue(location.Searched);
        Assert.IsFalse(location.HasMonster());
        Assert.IsFalse(location.HasBoon());
    }

    [Test]
    public void SearchSage_HeroExplored()
    {
        // Assemble
        var locationController = TestUtilities.CreateLocationController();
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
        var player1 = Game.Current.Players[0];
        var location = MapBuilder.FindLocation("SagesHut");
        var tile = World.Current.Map[1, 1];
        World.Current.AddLocation(location, tile);
        Army army = player1.HireHero(tile);
        var armies = new List<Army> { army };
        var success = locationController.SearchSage(armies, location, out var gold);
        Assert.IsTrue(success);
        var expectedGold = player1.Gold;

        // Act
        success = locationController.SearchSage(armies, location, out gold);

        // Assert
        // Sage can always be searched
        Assert.IsTrue(success);
        Assert.That(gold, Is.EqualTo(0));
        Assert.That(player1.Gold, Is.EqualTo(expectedGold));
        Assert.IsTrue(location.Searched);
    }
}