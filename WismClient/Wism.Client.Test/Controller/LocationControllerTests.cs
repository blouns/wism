using System.Collections.Generic;
using System.Diagnostics.Metrics;
using NUnit.Framework;
using NUnit.Framework.Internal.Execution;
using Wism.Client.Commands.Armies;
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
        Assert.That(result, Is.True);
        Assert.That(location.Searched, Is.True);
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
            Assert.That(success, Is.True);
        Assert.That(gold + initialGold, Is.EqualTo(player1.Gold));
        Assert.That(player1.Gold > initialGold, Is.True    );
        Assert.That(location.Searched, Is.True);
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

        // Act
        var success = locationController.SearchLibrary(armies, location, out var knowledge);

        // Assert
        Assert.That(success, Is.True);
        Assert.That(knowledge, Is.Not.Null);
        Assert.That(location.Searched, Is.True);
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
        Assert.That(success, Is.True);
        Assert.That(boon, Is.Not.Null);
        Assert.That(location.Searched, Is.True);
        Assert.That(location.HasMonster(), Is.False);
        Assert.That(location.HasBoon(), Is.False);
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
        Assert.That(success, Is.True);
        var expectedGold = player1.Gold;

        // Act
        success = locationController.SearchSage(armies, location, out gold);

        // Assert
        // Sage can always be searched
        Assert.That(success, Is.True   );
        Assert.That(gold, Is.EqualTo(0));
        Assert.That(player1.Gold, Is.EqualTo(expectedGold));
        Assert.That(location.Searched, Is.True);
    }
}