using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.Commands.Cities;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Core.Telemetry;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
[NonParallelizable]
public sealed class GameStateFingerprintTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    [Test]
    public void Fingerprint_OneHundredSeedRoundTripsPreserveStateAndNextCommand()
    {
        for (var seed = 20260811; seed < 20260911; seed++)
        {
            var settings = TestGameFactory.CreateDefaultNewGameSettings(TestUtilities.DefaultTestWorld, seed);
            GameFactory.Create(settings);
            var snapshot = Game.Current.Snapshot();

            AssertRoundTripAndNextEndTurn(snapshot, $"seed {seed}");
        }
    }

    [Test]
    public void Fingerprint_NamedSaveSeamsPreserveStateAndNextCommand()
    {
        var seams = new Dictionary<string, Func<GameEntity>>
        {
            ["setup"] = CreateSetupSnapshot,
            ["production"] = CreateProductionSnapshot,
            ["move"] = CreateMoveSnapshot,
            ["battle"] = CreateBattleSnapshot,
            ["capture"] = CreateCaptureSnapshot,
            ["raze"] = CreateRazeSnapshot
        };

        foreach (var seam in seams)
        {
            AssertRoundTripAndNextEndTurn(seam.Value(), seam.Key);
        }
    }

    [Test]
    public void Fingerprint_InjectedGoldFaultReportsFirstCommandAndChangedField()
    {
        Game.CreateDefaultGame();
        var expected = GameStateFingerprint.Capture(Game.Current);

        Game.Current.Players[0].Gold++;
        var actual = GameStateFingerprint.Capture(Game.Current);
        var divergence = GameStateFingerprint.LocateFirstDivergence(17, expected, actual);

        Assert.Multiple(() =>
        {
            Assert.That(divergence, Is.Not.Null);
            Assert.That(divergence.CommandIndex, Is.EqualTo(17));
            Assert.That(divergence.Path, Does.EndWith(".Gold"));
            Assert.That(divergence.Expected, Is.Not.EqualTo(divergence.Actual));
        });
    }

    [Test]
    public void Fingerprint_DefaultEvidenceIsCompactAndDoesNotRetainSnapshots()
    {
        Game.CreateDefaultGame();
        var fingerprint = GameStateFingerprint.Capture(Game.Current);

        Assert.Multiple(() =>
        {
            Assert.That(fingerprint.Hash, Has.Length.EqualTo(64));
            Assert.That(fingerprint.FieldCount, Is.GreaterThan(0));
            Assert.That(fingerprint.CanonicalByteCount, Is.LessThanOrEqualTo(5 * 1024 * 1024));
        });
    }

    [Test]
    public void Load_PreservesAdvancedRandomAndArmyAllocatorState()
    {
        for (var seed = 20260811; seed < 20260821; seed++)
        {
            var settings = TestGameFactory.CreateDefaultNewGameSettings(TestUtilities.DefaultTestWorld, seed);
            GameFactory.Create(settings);
            for (var i = 0; i < 7; i++)
            {
                _ = Game.Current.Random.Next();
            }

            var snapshot = Game.Current.Snapshot();
            var expected = ReadRandomSequence(Game.Current.Random, 10);

            GameFactory.Load(snapshot);
            var first = ReadRandomSequence(Game.Current.Random, 10);
            var firstLastArmyId = ArmyFactory.LastId;

            GameFactory.Load(snapshot);
            var replay = ReadRandomSequence(Game.Current.Random, 10);

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.EqualTo(expected), $"Advanced random state diverged for seed {seed}.");
                Assert.That(replay, Is.EqualTo(expected), $"Snapshot random state was mutated for seed {seed}.");
                Assert.That(firstLastArmyId, Is.EqualTo(snapshot.LastArmyId));
                Assert.That(ArmyFactory.LastId, Is.EqualTo(snapshot.LastArmyId));
            });
        }
    }

    [Test]
    public void Capture_TransferredCapitalLeavesOneOwnerAndSnapshotLoads()
    {
        Game.CreateDefaultGame();
        var attacker = Game.Current.Players[0];
        var defender = Game.Current.Players[1];
        var capital = MapBuilder.FindCity("Marthos");
        var secondCity = MapBuilder.FindCity("BanesCitadel");
        World.Current.AddCity(capital, World.Current.Map[1, 1]);
        World.Current.AddCity(secondCity, World.Current.Map[4, 4]);
        defender.ClaimCity(capital);
        defender.ClaimCity(secondCity);

        attacker.ClaimCity(secondCity);

        Assert.Multiple(() =>
        {
            Assert.That(defender.Capitol, Is.SameAs(capital));
            Assert.That(defender.GetCities(), Is.EquivalentTo(new[] { capital }));
        });

        attacker.ClaimCity(capital);
        var snapshot = Game.Current.Snapshot();

        Assert.Multiple(() =>
        {
            Assert.That(defender.GetCities(), Is.Empty);
            Assert.That(defender.Capitol, Is.Null);
            Assert.DoesNotThrow(() => GameFactory.Load(snapshot));
            Assert.That(Game.Current.Players[1].Capitol, Is.Null);
            Assert.That(Game.Current.Players[0].GetCities().Select(city => city.ShortName),
                Is.EquivalentTo(new[] { "Marthos", "BanesCitadel" }));
        });
    }

    private static int[] ReadRandomSequence(Random random, int count)
    {
        var values = new int[count];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = random.Next();
        }

        return values;
    }

    private static void AssertRoundTripAndNextEndTurn(GameEntity snapshot, string context)
    {
        var expected = GameStateFingerprint.From(snapshot);

        GameFactory.Load(snapshot);
        var loaded = GameStateFingerprint.Capture(Game.Current);
        var loadDivergence = GameStateFingerprint.LocateFirstDivergence(0, expected, loaded);
        Assert.That(loaded.Hash, Is.EqualTo(expected.Hash),
            $"State diverged at {context} load: {loadDivergence?.Path} " +
            $"expected={loadDivergence?.Expected} actual={loadDivergence?.Actual}.");
        Assert.That(loadDivergence, Is.Null);

        var firstControllers = TestUtilities.CreateControllerProvider();
        var firstResult = TestUtilities.EndTurn(
            firstControllers.CommandController,
            firstControllers.GameController);
        var firstNext = GameStateFingerprint.Capture(Game.Current);

        GameFactory.Load(snapshot);
        var secondControllers = TestUtilities.CreateControllerProvider();
        var secondResult = TestUtilities.EndTurn(
            secondControllers.CommandController,
            secondControllers.GameController);
        var secondNext = GameStateFingerprint.Capture(Game.Current);

        Assert.Multiple(() =>
        {
            Assert.That(secondResult, Is.EqualTo(firstResult), $"Next command result diverged at {context}.");
            Assert.That(secondNext.Hash, Is.EqualTo(firstNext.Hash), $"Next command state diverged at {context}.");
        });
    }

    private static GameEntity CreateSetupSnapshot()
    {
        var controllers = TestUtilities.CreateControllerProvider();
        TestUtilities.NewGame(controllers, TestUtilities.DefaultTestWorld);
        return Game.Current.Snapshot();
    }

    private static GameEntity CreateProductionSnapshot()
    {
        Game.CreateDefaultGame();
        var player = Game.Current.Players[0];
        var city = MapBuilder.FindCity("Marthos");
        World.Current.AddCity(city, World.Current.Map[1, 1]);
        player.ClaimCity(city);
        player.Gold = 1000000;
        Assert.That(city.Barracks.StartProduction(ModFactory.FindArmyInfo("LightInfantry")), Is.True);
        return Game.Current.Snapshot();
    }

    private static GameEntity CreateMoveSnapshot()
    {
        Game.CreateDefaultGame();
        var controllers = TestUtilities.CreateControllerProvider();
        var player = Game.Current.Players[0];
        var origin = World.Current.Map[1, 1];
        var army = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), origin);
        TestUtilities.Select(controllers, new List<Army> { army });
        Assert.That(TestUtilities.MoveUntilDone(
            controllers.CommandController,
            controllers.ArmyController,
            new List<Army> { army },
            2,
            1), Is.EqualTo(ActionState.Succeeded));
        return Game.Current.Snapshot();
    }

    private static GameEntity CreateBattleSnapshot()
    {
        Game.CreateDefaultGame();
        Game.Current.Random = new Random(1990);
        var controllers = TestUtilities.CreateControllerProvider();
        var attacker = Game.Current.Players[0].HireHero(World.Current.Map[1, 1]);
        var defender = Game.Current.Players[1].ConscriptArmy(
            ArmyInfo.GetArmyInfo("LightInfantry"),
            World.Current.Map[2, 1]);
        attacker.Strength = 9;
        defender.Strength = 1;

        TestUtilities.Select(controllers, new List<Army> { attacker });
        Assert.That(TestUtilities.AttackUntilDone(
            controllers.CommandController,
            controllers.ArmyController,
            new List<Army> { attacker },
            2,
            1), Is.EqualTo(ActionState.Succeeded));
        return Game.Current.Snapshot();
    }

    private static GameEntity CreateCaptureSnapshot()
    {
        var controllers = TestUtilities.CreateControllerProvider();
        TestUtilities.NewGame(controllers, TestUtilities.DefaultTestWorld);
        TestUtilities.StartTurn(controllers);
        var player = Game.Current.Players[0];
        var origin = World.Current.Map[6, 4];
        var target = World.Current.Map[7, 4];
        player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), origin);
        TestUtilities.Select(controllers, origin.GetAllArmies());
        Assert.That(TestUtilities.ExecuteCommandUntilDone(
            controllers.CommandController,
            new CaptureCityCommand(controllers.CityController, player, Game.Current.GetSelectedArmies(), target.City)),
            Is.EqualTo(ActionState.Succeeded));
        return Game.Current.Snapshot();
    }

    private static GameEntity CreateRazeSnapshot()
    {
        Game.CreateDefaultGame();
        var player = Game.Current.Players[0];
        var city = MapBuilder.FindCity("Marthos");
        World.Current.AddCity(city, World.Current.Map[1, 1]);
        player.ClaimCity(city);
        player.RazeCity(city);
        return Game.Current.Snapshot();
    }
}
