using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Core.Heros;
using Wism.Client.Modules;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class PlayerTests
{
    [Test]
    public void StartTurn_RecruitHeroWithoutCities_NoHero()
    {
        // Assemble            
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];

        // Act
        player1.StartTurn();

        // Assert
        Assert.That(player1.LastHeroTurn, Is.EqualTo(0));
    }

    [Test]
    public void StartTurn_RecruitHeroFirstTurn_NewHero()
    {
        // Assemble            
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];

        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        city.Tile = tile;
        player1.AddCity(city);

        // Act
        player1.StartTurn();

        // Assert
        Assert.That(player1.LastHeroTurn, Is.EqualTo(0));
    }

    [Test]
    public void StartTurn_RecruitHeroTooSoon_NoHero()
    {
        // Assemble            
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];

        var tile = World.Current.Map[1, 1];
        player1.HireHero(tile);
        player1.Turn = 2;

        var city = MapBuilder.FindCity("Marthos");
        city.Tile = tile;
        player1.AddCity(city);

        // Act
        player1.StartTurn();

        // Assert
        Assert.That(player1.LastHeroTurn, Is.EqualTo(1), "Last hero turn not set");
    }

    [Test]
    public void StartTurn_RecruitHero10thTurn_NewHero()
    {
        // Assemble            
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];
        player1.Turn = 10;

        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        city.Tile = tile;
        player1.AddCity(city);

        // Act
        player1.StartTurn();
        var success = player1.RecruitHeroStrategy.IsHeroAvailable(player1);
        var name = player1.RecruitHeroStrategy.GetHeroName();
        var actualCity = player1.RecruitHeroStrategy.GetTargetCity(player1);
        var price = player1.RecruitHeroStrategy.GetHeroPrice(player1);

        // Assert
        Assert.That(success, Is.True, "No hero was available");
        Assert.That(name, Is.Not.Empty, "No hero name");
        Assert.That(actualCity.ShortName, Is.EqualTo(city.ShortName));
        Assert.That(price, Is.GreaterThan(0), "Price too low");
        Assert.That(price, Is.LessThan(int.MaxValue), "Price too high");
        Assert.That(player1.LastHeroTurn, Is.EqualTo(0), "Last hero should be never (zero)");
    }

    [Test]
    public void StartTurn_RecruitHero20thTurnUnlucky_NoHero()
    {
        // Assemble            
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];
        player1.Turn = 10;

        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        city.Tile = tile;
        player1.AddCity(city);
        player1.StartTurn();
        player1.HireHero(tile);

        // Skip a turn (back to Sirians)
        Game.Current.EndTurn();
        Game.Current.StartTurn();
        Game.Current.EndTurn();

        player1.Turn = 20;

        // Act
        player1.StartTurn();

        // Assert
        Assert.That(player1.LastHeroTurn, Is.EqualTo(10), "Last hero should be 10");
    }

    [Test]
    public void StartTurn_RecruitHero21thTurnLucky_NewHero()
    {
        // Assemble            
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];
        player1.Turn = 10;
        player1.Gold = 10000;

        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        city.Tile = tile;
        player1.AddCity(city);
        player1.StartTurn();
        player1.HireHero(tile);

        // Skip a turn (back to Sirians)
        Game.Current.EndTurn();
        Game.Current.StartTurn();
        Game.Current.EndTurn();

        player1.Turn = 19;

        // Skip a turn (back to Sirians)
        Game.Current.EndTurn();
        Game.Current.StartTurn();
        Game.Current.EndTurn();

        // Act
        player1.StartTurn();
        var success = player1.RecruitHeroStrategy.IsHeroAvailable(player1);
        var name = player1.RecruitHeroStrategy.GetHeroName();
        var actualCity = player1.RecruitHeroStrategy.GetTargetCity(player1);
        var price = player1.RecruitHeroStrategy.GetHeroPrice(player1);
        var hired = player1.TryHireHero(tile, price, name, out var hero);

        // Assert
        Assert.That(player1.LastHeroTurn, Is.EqualTo(21), "Last hero should be 10");
        Assert.That(hired, Is.True, "Did not hire the hero");
        Assert.That(hero, Is.Not.Null, "Hero was null");
        Assert.That(success, Is.True, "No hero was available");
        Assert.That(name, Is.Not.Empty, "No hero name");
        Assert.That(actualCity.ShortName, Is.EqualTo(city.ShortName));
        Assert.That(price, Is.GreaterThan(0), "Price too low");
        Assert.That(price, Is.LessThan(int.MaxValue), "Price too high");
    }

    [Test]
    public void StartTurn_RecruitHero10thHero_NoHero()
    {
        // Assemble            
        Game.CreateDefaultGame();
        var player1 = Game.Current.Players[0];

        var tile = World.Current.Map[1, 1];
        var city = MapBuilder.FindCity("Marthos");
        city.Tile = tile;
        player1.AddCity(city);
        player1.HireHero(tile);
        player1.HireHero(tile);
        player1.HireHero(tile);
        player1.HireHero(tile);
        player1.HireHero(tile);
        player1.HireHero(tile);
        player1.HireHero(tile);
        player1.HireHero(tile);
        player1.HireHero(tile);

        player1.Turn = 10;

        // Act
        player1.StartTurn();

        // Assert
        Assert.That(player1.LastHeroTurn, Is.EqualTo(1), "Last hero turn not set");
    }

    private static IRecruitHeroStrategy GetRecruitHeroStrategy()
    {
        var path = ModFactory.ModPath + "\\" + ModFactory.HeroPath;
        return ModFactory.LoadRecruitHeroStrategy(path);
    }
}
