using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
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
        Assert.AreEqual(tile.Armies[0].ShortName, "Hero");
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
        Assert.AreEqual("Hero", tile.Armies[0].ShortName);
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
        Assert.AreEqual("Hero", tile.Armies[0].ShortName);
    }

    [Test]
    public void StackBattleOrder_OnlyHeroTest()
    {
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);
        tile.Armies.Sort(new ByArmyBattleOrder(tile));
        Assert.AreEqual("Hero", tile.Armies[0].ShortName);
    }

    [Test]
    public void StackBattleOrder_HeroAndWeakerArmyTest()
    {
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        Game.Current.Players[0].HireHero(World.Current.Map[2, 2]);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        tile.Armies.Sort(new ByArmyBattleOrder(tile));
        Assert.AreEqual("Hero", tile.Armies[1].ShortName);
        Assert.AreEqual("LightInfantry", tile.Armies[0].ShortName);
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
        Assert.AreEqual("LightInfantry", tile.Armies[0].ShortName);
        Assert.AreEqual("LightInfantry", tile.Armies[1].ShortName);
        Assert.AreEqual("Hero", tile.Armies[2].ShortName);
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
        Assert.AreEqual("Hero", tile.Armies[3].ShortName, "Hero out of order");
        Assert.AreEqual("Pegasus", tile.Armies[2].ShortName, "Pegasus out of order");
        Assert.AreEqual("Pegasus", tile.Armies[1].ShortName, "Pegasus out of order");
        Assert.AreEqual("Cavalry", tile.Armies[0].ShortName, "Cavalry out of order");
    }

    [Test]
    public void StackBattleOrder_TwoHeroesTest()
    {
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);
        player1.HireHero(tile);

        tile.Armies.Sort(new ByArmyBattleOrder(tile));
        Assert.AreEqual("Hero", tile.Armies[0].ShortName);
        Assert.AreEqual("Hero", tile.Armies[1].ShortName);
    }

    public void StackBattleOrder_TwoHeroesAndSomeArmiesTest()
    {
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.HireHero(tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        player1.HireHero(tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);

        tile.Armies.Sort(new ByArmyBattleOrder(tile));
        Assert.AreEqual("Hero", tile.Armies[7].ShortName, "Hero out of order");
        Assert.AreEqual("Hero", tile.Armies[6].ShortName, "Hero out of order");
        Assert.AreEqual("Pegasus", tile.Armies[5].ShortName, "Pegasus out of order");
        Assert.AreEqual("Pegasus", tile.Armies[4].ShortName, "Pegasus out of order");
        Assert.AreEqual("Cavalry", tile.Armies[3].ShortName, "Cavalry out of order");
        Assert.AreEqual("HeavyInfantry", tile.Armies[2].ShortName, "Heavy infantry out of order");
        Assert.AreEqual("LightInfantry", tile.Armies[1].ShortName, "Light infantry out of order");
        Assert.AreEqual("LightInfantry", tile.Armies[0].ShortName, "Light infantry out of order");
    }

    public void StackBattleOrder_NoHeroesTest()
    {
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), tile);

        var battleSortedArmies = player1.GetArmies();
        battleSortedArmies.Sort(new ByArmyBattleOrder(tile));
        Assert.AreEqual("Pegasus", battleSortedArmies[5].ShortName, "Pegasus out of order");
        Assert.AreEqual("Pegasus", battleSortedArmies[4].ShortName, "Pegasus out of order");
        Assert.AreEqual("Cavalry", battleSortedArmies[3].ShortName, "Cavalry out of order");
        Assert.AreEqual("HeavyInfantry", battleSortedArmies[2].ShortName, "Heavy infantry out of order");
        Assert.AreEqual("LightInfantry", battleSortedArmies[1].ShortName, "Light infantry out of order");
        Assert.AreEqual("LightInfantry", battleSortedArmies[0].ShortName, "Light infantry out of order");
    }

    [Test]
    public void StackBattleOrder_CavalryAfterInfantryTest()
    {
        // Assemble
        var player1 = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), tile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);

        // Act
        var battleSortedArmies = player1.GetArmies();
        battleSortedArmies.Sort(new ByArmyBattleOrder(tile));

        // Assert
        Assert.AreEqual("Cavalry", battleSortedArmies[3].ShortName, "Cavalry out of order");
        Assert.AreEqual("Cavalry", battleSortedArmies[2].ShortName, "Cavalry out of order");
        Assert.AreEqual("HeavyInfantry", battleSortedArmies[1].ShortName, "Heavy infantry out of order");
        Assert.AreEqual("LightInfantry", battleSortedArmies[0].ShortName, "Light infantry out of order");
    }


    /* TODO: Tests to be added
        * - Different terrain, clan, army bonuses, 
        * - Greater strength than hero
        * - Special, 2 specials, special+fly
        * - 2 fliers
        * - Navy
        */

    [Test]
    public void Hero_CannotFlyFloatTest()
    {
        // ASSEMBLE / ACT
        Game.Current.Players[0].HireHero(World.Current.Map[2, 2]);
        var army = GetFirstHero();

        // ASSERT
        Assert.IsTrue(army.CanWalk, "Hero cannot walk. Broken leg?");
        Assert.IsFalse(army.CanFloat, "Hero learned how to swim!");
        Assert.IsFalse(army.CanFly, "Heros can fly!? Crazy talk.");
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
            Assert.AreEqual(expectedCount--, path.Count, "Mismatch on the number of expected moves remaining.");
        }

        Assert.IsNotNull(path, "Failed to traverse the route.");
        Assert.AreEqual(0, path.Count, "Mismatch on the number of expected moves remaining.");
        Assert.AreEqual(expectedCount, path.Count, "Mismatch of expected moves.");
    }

    [Test]
    public void Move_HeroWaterPathTest()
    {
        // ASSEMBLE
        string[,] matrix =
        {
            { "1", "1", "1", "1", "1", "1" },
            { "1", "1", "S", "1", "0", "1" },
            { "1", "0", "0", "0", "0", "1" },
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
            Assert.AreEqual(expectedCount--, path.Count, "Mismatch on the number of expected moves remaining.");
        }

        Assert.IsNotNull(path, "Failed to traverse the route.");
        Assert.AreEqual(expectedCount, path.Count, "Mismatch of expected moves.");
    }

    [Test]
    public void Move_HeroFlyOnDragonPathTest()
    {
        // ASSEMBLE
        string[,] matrix =
        {
            { "1", "1", "1", "1", "1", "1" },
            { "1", "1", "S", "1", "0", "1" },
            { "0", "0", "0", "0", "0", "0" },
            { "1", "1", "1", "2", "2", "2" },
            { "1", "1", "1", "2", "T", "1" },
            { "1", "1", "1", "2", "1", "1" }
        };

        World.CreateWorld(PathingStrategyTests.ConvertMatrixToMap(matrix, out var armies, out var target));
        var player1 = Game.Current.Players[0];
        var dragon = player1.ConscriptArmy(ModFactory.FindArmyInfo("Dragons"), armies[0].Tile);
        armies.Add(dragon);
        var expectedCount = 5;

        // ACT / ASSERT
        IList<Tile> path = null;
        while (this.armyController.MoveOneStep(armies, target, ref path, out _) == ActionState.InProgress)
        {
            Assert.AreEqual(expectedCount--, path.Count, "Mismatch on the number of expected moves remaining.");
        }

        Assert.IsNotNull(path, "Failed to traverse the route.");
        Assert.AreEqual(expectedCount, path.Count, "Mismatch of expected moves.");
    }

    [Test]
    public void Move_NavyPathTest()
    {
        // ASSEMBLE
        // Note: '1' = Water for Navy tests
        string[,] matrix =
        {
            { "2", "2", "2", "2", "2", "2" },
            { "2", "S", "2", "2", "2", "2" },
            { "2", "1", "1", "1", "1", "2" },
            { "2", "2", "2", "2", "1", "2" },
            { "2", "2", "2", "2", "T", "2" },
            { "2", "2", "2", "2", "2", "2" }
        };

        World.CreateWorld(PathingStrategyTests.ConvertMatrixToMapForNavy(matrix, out var armies, out var target));
        var expectedCount = 4;

        // ACT / ASSERT
        IList<Tile> path = null;
        while (this.armyController.MoveOneStep(armies, target, ref path, out _) == ActionState.InProgress)
        {
            Assert.AreEqual(expectedCount--, path.Count, "Mismatch of expected moves.");
        }

        Assert.IsNotNull(path, "Failed to traverse the route.");
        Assert.AreEqual(expectedCount, path.Count, "Mismatch of expected moves.");
    }

    [Test]
    public void Move_NavyWithCrewPathTest()
    {
        // ASSEMBLE
        // Note: '1' = Water for Navy tests
        string[,] matrix =
        {
            { "2", "2", "2", "2", "2", "2" },
            { "2", "S", "2", "2", "2", "2" },
            { "2", "1", "1", "1", "1", "2" },
            { "2", "2", "2", "2", "1", "2" },
            { "2", "2", "2", "2", "T", "2" },
            { "2", "2", "2", "2", "2", "2" }
        };

        World.CreateWorld(PathingStrategyTests.ConvertMatrixToMapForNavy(matrix, out var armies, out var target));
        var player1 = Game.Current.Players[0];
        var infantry = player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), armies[0].Tile);
        armies.Add(infantry);
        var expectedCount = 4;

        // ACT / ASSERT
        IList<Tile> path = null;
        while (this.armyController.MoveOneStep(armies, target, ref path, out _) == ActionState.InProgress)
        {
            Assert.AreEqual(expectedCount--, path.Count, "Mismatch of expected moves.");
        }

        Assert.IsNotNull(path, "Failed to traverse the route.");
        Assert.AreEqual(expectedCount, path.Count, "Mismatch of expected moves.");
    }

    [Test]
    public void Move_NavyWithCrewOutOfMovesPathTest()
    {
        // ASSEMBLE
        // Note: '1' = Water for Navy tests
        string[,] matrix =
        {
            { "2", "2", "2", "2", "2", "2" },
            { "2", "S", "2", "2", "2", "2" },
            { "2", "1", "1", "1", "1", "2" },
            { "2", "2", "2", "2", "1", "2" },
            { "2", "2", "2", "2", "T", "2" },
            { "2", "2", "2", "2", "2", "2" }
        };

        World.CreateWorld(PathingStrategyTests.ConvertMatrixToMapForNavy(matrix, out var armies, out var target));
        var player1 = Game.Current.Players[0];
        var infantry = player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), armies[0].Tile);
        infantry.MovesRemaining = 0;
        armies.Add(infantry);
        var expectedCount = 4;

        // ACT / ASSERT
        IList<Tile> path = null;
        while (this.armyController.MoveOneStep(armies, target, ref path, out _) == ActionState.InProgress)
        {
            Assert.AreEqual(expectedCount--, path.Count, "Mismatch of expected moves.");
        }

        Assert.IsNotNull(path, "Failed to traverse the route.");
        Assert.AreEqual(expectedCount, path.Count, "Mismatch of expected moves.");
    }

    [Test]
    public void Move_NonHeroFlyOnDragonPathTest_Fail()
    {
        // ASSEMBLE
        string[,] matrix =
        {
            { "1", "1", "1", "1", "1", "1" },
            { "1", "1", "S", "1", "0", "1" },
            { "0", "0", "0", "0", "0", "0" },
            { "1", "1", "1", "2", "2", "2" },
            { "1", "1", "1", "2", "T", "1" },
            { "1", "1", "1", "2", "1", "1" }
        };

        World.CreateWorld(PathingStrategyTests.ConvertMatrixToMap(matrix, out var armies, out var target));
        var player1 = Game.Current.Players[0];
        var dragon = player1.ConscriptArmy(ModFactory.FindArmyInfo("Dragons"), armies[0].Tile);
        var infantry = player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), armies[0].Tile);
        armies.Clear();
        armies.Add(dragon);
        armies.Add(infantry);

        // ACT / ASSERT
        IList<Tile> path = null;
        var result = this.armyController.MoveOneStep(armies, target, ref path, out _);

        Assert.AreEqual(ActionState.Failed, result, "Infantry was able to ride the dragon!");
    }

    [Test]
    public void MovementCost_BasicTest()
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

        const int expectedCost = 7;
        const int initialMoves = 10;
        armies[0].MovesRemaining = initialMoves;

        // ACT
        IList<Tile> path = null;
        while (this.armyController.MoveOneStep(armies, target, ref path, out _) == ActionState.InProgress)
        {
            // do nothing
        }

        // ASSERT
        Assert.AreEqual(initialMoves - expectedCost, armies[0].MovesRemaining,
            "Mismatch on the number of expected moves remaining.");
    }

    [Test]
    public void MovementCost_NoMovesRemainingTest()
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

        const int initialMoves = 6;
        armies[0].MovesRemaining = initialMoves;

        // ACT
        IList<Tile> path = null;
        while (this.armyController.MoveOneStep(armies, target, ref path, out _) == ActionState.InProgress)
        {
            // do nothing
        }

        // ASSERT
        Assert.AreEqual(0, armies[0].MovesRemaining, "Mismatch on the number of expected moves remaining.");
    }

    [Test]
    public void Move_SelectedArmy_Basic()
    {
        // ASSEMBLE
        var player1 = Game.Current.Players[0];
        var originalTile = World.Current.Map[2, 2];

        player1.HireHero(World.Current.Map[2, 2]);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), originalTile);

        var originalArmies = new List<Army>(originalTile.Armies);
        var expectedX = originalArmies[0].X;
        var expectedY = originalArmies[0].Y + 1;

        // Select two armies from the tile            
        var selectedArmies = new List<Army>
        {
            originalTile.Armies[4],
            originalTile.Armies[5]
        };
        originalArmies.RemoveAt(4);
        originalArmies.RemoveAt(5);
        this.armyController.SelectArmy(selectedArmies);

        // ACT: Move the selected armies
        if (!this.TryMove(selectedArmies, Direction.North))
        {
            Assert.Fail("Could not move the army.");
        }

        // Deselect the armies
        this.armyController.DeselectArmy(selectedArmies);

        // ASSERT
        var newTile = selectedArmies[0].Tile;
        Assert.IsNotNull(newTile.Armies, "Army should be set on new tile");
        Assert.IsNotNull(newTile.Armies[0].Tile, "Army's tile should be set on new tile");
        Assert.AreEqual(selectedArmies.Count, newTile.Armies.Count,
            "Selected army does not have the expected number of armies.");
        Assert.AreEqual(originalArmies.Count, originalTile.Armies.Count,
            "Standing army does not have the expect number of armies.");
        Assert.AreEqual(expectedX, newTile.X, "Selected armies did not move as expected.");
        Assert.AreEqual(expectedY, newTile.Y, "Selected armies did not move as expected.");
        Assert.AreEqual(originalTile.X, originalArmies[0].X, "Standing army did not stay as expected.");
        Assert.AreEqual(originalTile.Y, originalArmies[0].Y, "Standing army did not stay as expected.");
    }

    [Test]
    public void Move_SelectedArmy_BasicFail()
    {
        // ASSEMBLE
        var player1 = Game.Current.Players[0];
        var originalTile = World.Current.Map[2, 2];

        player1.HireHero(World.Current.Map[2, 2]);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), originalTile);

        var originalArmies = new List<Army>(originalTile.Armies);
        var expectedX = originalArmies[0].X;
        var expectedY = originalArmies[0].Y + 2;

        // Select two armies from the tile            
        var selectedArmies = new List<Army>
        {
            originalTile.Armies[4],
            originalTile.Armies[5]
        };
        originalArmies.RemoveAt(4);
        originalArmies.RemoveAt(5);
        this.armyController.SelectArmy(selectedArmies);

        // ACT: Move the selected armies
        if (!this.TryMove(selectedArmies, Direction.North))
        {
            Assert.Fail("Could not move the army.");
        }

        // Move north
        if (!this.TryMove(selectedArmies, Direction.North))
        {
            Assert.Fail("Could not move the army.");
        }

        // Attempt to move into the mountains (should fail)
        if (this.TryMove(selectedArmies, Direction.North))
        {
            Assert.Fail("Could not move the army.");
        }

        // Deselect the armies
        this.armyController.DeselectArmy(selectedArmies);

        // ASSERT
        var newTile = selectedArmies[0].Tile;
        Assert.AreEqual(expectedX, newTile.X, "Army is not in the expected position.");
        Assert.AreEqual(expectedY, newTile.Y, "Army is not in the expected position.");
        Assert.IsNotNull(newTile.Armies, "Army should be set on new tile");
        Assert.IsNotNull(newTile.Armies[0].Tile, "Army's tile should be set on new tile");
        Assert.AreEqual(selectedArmies.Count, newTile.Armies.Count,
            "Selected army does not have the expected number of armies.");
        Assert.AreEqual(originalArmies.Count, originalTile.Armies.Count,
            "Standing army does not have the expect number of armies.");
        Assert.AreEqual(originalTile.X, originalArmies[0].X, "Standing army did not stay as expected.");
        Assert.AreEqual(originalTile.Y, originalArmies[0].Y, "Standing army did not stay as expected.");
    }

    [Test]
    public void Move_SelectedArmy_Merge()
    {
        // ASSEMBLE
        var player1 = Game.Current.Players[0];

        var originalTile = World.Current.Map[2, 2];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), originalTile);
        var originalArmies = new List<Army>(originalTile.Armies);

        var mergeTile = World.Current.Map[2, 4];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), mergeTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), mergeTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), mergeTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), mergeTile);
        var mergeArmies = new List<Army>(mergeTile.Armies);

        var expectedX = mergeArmies[0].X;
        var expectedY = mergeArmies[0].Y;

        // Select all armies from the original tile            
        var selectedArmies = new List<Army>(originalTile.Armies);
        this.armyController.SelectArmy(selectedArmies);

        // ACT

        // Move the selected armies one north
        if (!this.TryMove(selectedArmies, Direction.North))
        {
            Assert.Fail("Could not move the army.");
        }

        // Move the selected armies one north onto merge armies
        if (!this.TryMove(selectedArmies, Direction.North))
        {
            Assert.Fail("Could not move the army.");
        }

        // Deselect the armies
        this.armyController.DeselectArmy(selectedArmies);

        // ASSERT
        var newTile = selectedArmies[0].Tile;
        Assert.IsNull(newTile.VisitingArmies, "Visiting Army should not be set on new tile");
        Assert.IsNull(originalTile.VisitingArmies, "Visiting Army should not be set on original tile");
        Assert.AreEqual(selectedArmies.Count + mergeArmies.Count, newTile.Armies.Count,
            "Selected army does not have the expected number of armies.");
        Assert.AreEqual(expectedX, newTile.X, "Selected armies did not move as expected.");
        Assert.AreEqual(expectedY, newTile.Y, "Selected armies did not move as expected.");
    }

    [Test]
    public void Move_SelectedArmy_PassThroughArmy()
    {
        // ASSEMBLE
        var player1 = Game.Current.Players[0];

        var originalTile = World.Current.Map[2, 2];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), originalTile);
        var originalArmies = new List<Army>(originalTile.Armies);

        var mergeTile = World.Current.Map[2, 3];
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Cavalry"), mergeTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), mergeTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), mergeTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("Pegasus"), mergeTile);
        var mergeArmies = new List<Army>(mergeTile.Armies);

        var expectedX = 2;
        var expectedY = 4;

        // Select all armies from the original tile            
        this.armyController.SelectArmy(originalArmies);

        // ACT

        // Move the selected armies one north onto merge armies
        if (!this.TryMove(originalArmies, Direction.North))
        {
            Assert.Fail("Could not move the army.");
        }

        // Move the selected armies one north away from merge armies
        if (!this.TryMove(originalArmies, Direction.North))
        {
            Assert.Fail("Could not move the army.");
        }

        // Deselect the armies
        this.armyController.DeselectArmy(originalArmies);

        // ASSERT
        var newTile = originalArmies[0].Tile;
        Assert.IsNull(newTile.VisitingArmies, "Visiting Army should not be set on new tile");
        Assert.IsNull(originalTile.VisitingArmies, "Visiting Army should not be set on original tile");
        Assert.IsNull(mergeTile.VisitingArmies, "Visiting Army should not be set on merge tile");
        Assert.AreEqual(originalArmies.Count, newTile.Armies.Count,
            "Selected army does not have the expected number of armies.");
        Assert.AreEqual(expectedX, newTile.X, "Selected armies did not move as expected.");
        Assert.AreEqual(expectedY, newTile.Y, "Selected armies did not move as expected.");
    }

    [Test]
    public void Attack_SelectedArmy_DefeatEnemyStack()
    {
        // ASSEMBLE
        var player1 = Game.Current.Players[0];
        var player2 = Game.Current.Players[1];

        var originalTile = World.Current.Map[2, 2];
        player1.HireHero(World.Current.Map[2, 2]);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), originalTile);
        var originalArmies = new List<Army>(originalTile.Armies);

        var enemyTile = World.Current.Map[2, 3];
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), enemyTile);
        var enemyArmies = new List<Army>(enemyTile.Armies);

        var expectedX = originalArmies[0].X;
        var expectedY = originalArmies[0].Y;
        var expectedHumanArmies = 3;
        var expectedEnemyArmies = 0;

        // Select all armies from the original tile            
        var selectedArmies = new List<Army>(originalArmies);
        this.armyController.SelectArmy(selectedArmies);

        // ACT
        // Attack: Should win but not advance
        this.armyController.PrepareForBattle();
        var result = this.armyController.AttackOnce(selectedArmies, enemyTile);
        Assert.AreEqual(AttackResult.AttackerWinsRound, result, "Attacker did not win round.");
        result = this.armyController.AttackOnce(selectedArmies, enemyTile);

        // Deselect the armies
        this.armyController.DeselectArmy(selectedArmies);

        // ASSERT
        var newTile = selectedArmies[0].Tile;
        Assert.AreEqual(AttackResult.AttackerWinsBattle, result, "Original army was defeated.");
        Assert.AreEqual(3, selectedArmies.Count, "Selected army does not have the expected number of armies.");
        Assert.IsNull(enemyTile.Armies, "Enemy army still exists.");
        Assert.AreEqual(expectedX, originalArmies[0].X, "Selected armies moved unexpectedly.");
        Assert.AreEqual(expectedY, originalArmies[0].Y, "Selected armies moved unexpectedly.");
        enemyArmies.ForEach(e => Assert.IsTrue(e.IsDead, "Enemy is not dead."));
        Assert.AreEqual(expectedHumanArmies, player1.GetArmies().Count, "Human player has incorrect army count.");
        Assert.AreEqual(expectedEnemyArmies, player2.GetArmies().Count, "Enemy still has armies.");
    }

    [Test]
    public void AttackOnce_SelectedArmy_Win()
    {
        // ASSEMBLE
        var player1 = Game.Current.Players[0];
        var player2 = Game.Current.Players[1];

        var originalTile = World.Current.Map[2, 2];
        player1.HireHero(World.Current.Map[2, 2]);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), originalTile);
        var originalArmies = new List<Army>(originalTile.Armies);

        var enemyTile = World.Current.Map[2, 3];
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), enemyTile);
        var enemyArmies = new List<Army>(enemyTile.Armies);

        var expectedX = originalArmies[0].X;
        var expectedY = originalArmies[0].Y;
        var expectedHumanArmies = 2;
        var expectedEnemyArmies = 0;

        // Select all armies from the original tile            
        var selectedArmies = new List<Army>(originalArmies);
        this.armyController.SelectArmy(selectedArmies);
        this.armyController.PrepareForBattle();

        // ACT
        // Attack: Should win but not advance
        var result = this.armyController.AttackOnce(selectedArmies, enemyTile);

        // Deselect the armies
        this.armyController.DeselectArmy(selectedArmies);

        // ASSERT
        var newTile = selectedArmies[0].Tile;
        Assert.AreEqual(AttackResult.AttackerWinsBattle, result, "Original army was defeated.");
        Assert.IsNull(enemyTile.Armies, "Enemy army still exists.");
        Assert.AreEqual(expectedX, originalArmies[0].X, "Selected armies moved unexpectedly.");
        Assert.AreEqual(expectedY, originalArmies[0].Y, "Selected armies moved unexpectedly.");
        enemyArmies.ForEach(e => Assert.IsTrue(e.IsDead, "Enemy is not dead."));
        Assert.AreEqual(expectedHumanArmies, player1.GetArmies().Count, "Human player has incorrect army count.");
        Assert.AreEqual(expectedEnemyArmies, player2.GetArmies().Count, "Enemy still has armies.");
    }

    [Test]
    public void AttackOnce_SelectedArmy_Lose()
    {
        // ASSEMBLE
        var player1 = Game.Current.Players[0];
        var player2 = Game.Current.Players[1];

        var originalTile = World.Current.Map[2, 2];
        // Only hero (from setup)
        player1.HireHero(World.Current.Map[2, 2]);
        var originalArmies = new List<Army>(originalTile.Armies);

        var enemyTile = World.Current.Map[2, 3];
        player2.HireHero(enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("Dragons"), enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("Dragons"), enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("Dragons"), enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("Dragons"), enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("Dragons"), enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("Dragons"), enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("Dragons"), enemyTile);
        var enemyArmies = new List<Army>(enemyTile.Armies);

        var expectedX = originalArmies[0].X;
        var expectedY = originalArmies[0].Y;
        var expectedHumanArmies = 0;
        var expectedEnemyArmies = 8;

        // Select all armies from the original tile            
        var selectedArmies = new List<Army>(originalArmies);
        this.armyController.SelectArmy(selectedArmies);
        this.armyController.PrepareForBattle();

        // ACT
        // Attack: Should win but not advance
        var result = this.armyController.AttackOnce(selectedArmies, enemyTile);

        // ASSERT
        Assert.AreEqual(AttackResult.DefenderWinBattle, result, "Original army was defeated.");
        Assert.IsNull(originalTile.Armies, "Attacking army still exists.");
        Assert.IsNull(originalTile.VisitingArmies, "Attacking army still exists.");
        originalArmies.ForEach(e => Assert.IsTrue(e.IsDead, "Attacker is not dead."));
        Assert.AreEqual(expectedHumanArmies, player1.GetArmies().Count, "Human player has incorrect army count.");
        Assert.AreEqual(expectedEnemyArmies, player2.GetArmies().Count, "Enemy still has armies.");
    }

    [Test]
    public void AttackOnce_SelectedArmy_AttackUntilDone()
    {
        // ASSEMBLE
        var player1 = Game.Current.Players[0];
        var player2 = Game.Current.Players[1];

        var originalTile = World.Current.Map[2, 2];
        player1.HireHero(World.Current.Map[2, 2]);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), originalTile);
        player1.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), originalTile);
        var originalArmies = new List<Army>(originalTile.Armies);

        var enemyTile = World.Current.Map[2, 3];
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), enemyTile);
        player2.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), enemyTile);
        var enemyArmies = new List<Army>(enemyTile.Armies);

        var expectedX = originalArmies[0].X;
        var expectedY = originalArmies[0].Y;
        var expectedHumanArmies = 6;
        var expectedEnemyArmies = 0;

        // Select all armies from the original tile            
        var selectedArmies = new List<Army>(originalArmies);
        this.armyController.SelectArmy(selectedArmies);
        this.armyController.PrepareForBattle();

        // ACT
        // Attack until battle completed: Should win but not advance
        var result = AttackResult.NotStarted;
        do
        {
            result = this.armyController.AttackOnce(selectedArmies, enemyTile);
        } while (result == AttackResult.AttackerWinsRound ||
                 result == AttackResult.DefenderWinsRound);

        // Deselect the armies
        this.armyController.DeselectArmy(selectedArmies);

        // ASSERT
        var newTile = selectedArmies[0].Tile;
        Assert.AreEqual(AttackResult.AttackerWinsBattle, result, "Original army was defeated.");
        Assert.IsNull(enemyTile.Armies, "Enemy army still exists.");
        Assert.AreEqual(expectedX, originalArmies[0].X, "Selected armies moved unexpectedly.");
        Assert.AreEqual(expectedY, originalArmies[0].Y, "Selected armies moved unexpectedly.");
        enemyArmies.ForEach(e => Assert.IsTrue(e.IsDead, "Enemy is not dead."));
        Assert.AreEqual(expectedHumanArmies, player1.GetArmies().Count, "Human player has incorrect army count.");
        Assert.AreEqual(expectedEnemyArmies, player2.GetArmies().Count, "Enemy still has armies.");
    }

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