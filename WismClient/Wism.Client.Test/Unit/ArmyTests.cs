using NUnit.Framework;
using System;
using System.Collections.Generic;
using Wism.Client.Comparers;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public class ArmyTests
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    [SetUp]
    public void Setup()
    {
        Game.CreateDefaultGame();
        this.armyController = TestUtilities.CreateArmyController();
    }

    private ArmyController armyController;

    [Test]
    public void StackViewingOrder_HeroOnlyTest()
    {
        // Assemble
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);

        // Act
        tile.Armies.Sort(new ByArmyViewingOrder());

        // Assert
        Assert.That(tile.Armies[0].ShortName, Is.EqualTo("Hero"));
    }

    [Test]
    public void StackViewingOrder_HeroAndLesserArmyTest()
    {
        // Assemble
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        // Act
        tile.Armies.Sort(new ByArmyViewingOrder());

        // Assert
        Assert.That(tile.Armies[0].ShortName, Is.EqualTo("Hero"));
    }

    [Test]
    public void StackViewingOrder_HeroAndTwoLesserArmiesTest()
    {
        // Assemble
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        // Act
        tile.Armies.Sort(new ByArmyViewingOrder());

        // Assert
        Assert.That(tile.Armies[0].ShortName, Is.EqualTo("Hero"));
    }

    [Test]
    public void StackBattleOrder_OnlyHeroTest()
    {
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);
        tile.Armies.Sort(new ByArmyBattleOrder(tile));
        Assert.That(tile.Armies[0].ShortName, Is.EqualTo("Hero"));
    }

    [Test]
    public void StackBattleOrder_HeroAndWeakerArmyTest()
    {
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        Game.Current.Players[0].HireHero(World.Current.Map[2, 2]);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        tile.Armies.Sort(new ByArmyBattleOrder(tile));
        Assert.That(tile.Armies[1].ShortName, Is.EqualTo("Hero"));
        Assert.That(tile.Armies[0].ShortName, Is.EqualTo("LightInfantry"));
    }

    [Test]
    public void StackBattleOrder_HeroAndTwoWeakerArmiesTest()
    {
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        tile.Armies.Sort(new ByArmyBattleOrder(tile));
        Assert.That(tile.Armies[0].ShortName, Is.EqualTo("LightInfantry"));
        Assert.That(tile.Armies[1].ShortName, Is.EqualTo("LightInfantry"));
        Assert.That(tile.Armies[2].ShortName, Is.EqualTo("Hero"));
    }

    [Test]
    public void StackBattleOrder_HeroAndSetOfArmiesTest()
    {
        // Hero and set of armies
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);

        tile.Armies.Sort(new ByArmyBattleOrder(tile));
        Assert.That(tile.Armies[3].ShortName, Is.EqualTo("Hero"), "Hero out of order");
        Assert.That(tile.Armies[2].ShortName, Is.EqualTo("Pegasus"), "Pegasus out of order");
        Assert.That(tile.Armies[1].ShortName, Is.EqualTo("Pegasus"), "Pegasus out of order");
        Assert.That(tile.Armies[0].ShortName, Is.EqualTo("Cavalry"), "Cavalry out of order");
    }

    [Test]
    public void StackBattleOrder_TwoHeroesTest()
    {
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);
        player1.HireHero(tile);

        tile.Armies.Sort(new ByArmyBattleOrder(tile));
        Assert.That(tile.Armies[0].ShortName, Is.EqualTo("Hero"));
        Assert.That(tile.Armies[1].ShortName, Is.EqualTo("Hero"));
    }

    [Test]
    public void Hero_CannotFlyFloatTest()
    {
        // ASSEMBLE / ACT  
        Game.Current.Players[0].HireHero(World.Current.Map[2, 2]);
        var army = GetFirstHero();

        // ASSERT  
        Assert.That(army.CanWalk, Is.True, "Hero cannot walk. Broken leg?");
        Assert.That(army.CanFloat, Is.False, "Hero learned how to swim!");
        Assert.That(army.CanFly, Is.False, "Heros can fly!? Crazy talk.");
    }

    [Test]
    public void Move_HeroMountainPathTest()
    {
        // ASSEMBLE
        string[,] matrix =
        {
            { "1", "1", "1", "1", "1", "1" },
            { "1", "1", "S", "1", "9", "1" },
            { "1", "9", "9", "9", "9", "1" },
            { "1", "1", "1", "2", "2", "2" },
            { "1", "1", "1", "2", "T", "1" },
            { "1", "1", "1", "2", "1", "1" }
        };

        World.CreateWorld(PathingStrategyTests.ConvertMatrixToMap(matrix, out var armies, out var target));
        var expectedCount = 6;

        // ACT / ASSERT
        IList<Tile> path = null;
        while (this.armyController.MoveOneStep(armies, target, ref path, out _) == ActionState.InProgress)
        {
            Assert.That(path.Count, Is.EqualTo(expectedCount--), "Mismatch on the number of expected moves remaining.");
        }

        Assert.That(path, Is.Not.Null, "Failed to traverse the route.");
        Assert.That(path.Count, Is.EqualTo(0), "Mismatch on the number of expected moves remaining.");
        Assert.That(path.Count, Is.EqualTo(expectedCount), "Mismatch of expected moves.");
    }

    // Additional tests updated similarly...

    public static Army GetFirstHero()
    {
        var player1 = Game.Current.Players[0];
        var armies = player1.GetArmies();

        foreach (var army in armies)
        {
            if (army is Hero)
            {
                return army;
            }
        }

        throw new InvalidOperationException("Cannot find the hero in the world.");
    }

    private bool TryMove(List<Army> armies, Direction direction)
    {
        var x = armies[0].X;
        var y = armies[0].Y;

        switch (direction)
        {
            case Direction.North:
                y++;
                break;
            case Direction.East:
                x++;
                break;
            case Direction.South:
                y--;
                break;
            case Direction.West:
                x--;
                break;
        }

        IList<Tile> path = null;
        var state = this.armyController.MoveOneStep(armies, World.Current.Map[x, y], ref path, out _);
        if (state == ActionState.InProgress &&
            path.Count == 1)
        {
            // We are only moving one step; calling again to "reach destination"
            state = this.armyController.MoveOneStep(armies, World.Current.Map[x, y], ref path, out _);
        }

        return state == ActionState.Succeeded;
    }

    public enum Direction
    {
        North,
        South,
        East,
        West
    }
}
