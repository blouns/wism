using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data;
using Wism.Client.Data.Entities.Army;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture, NonParallelizable]
public sealed class DisbandHistoryTests
{
    [SetUp]
    public void Setup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Snapshot_RetainsRequestedIdentityAndCommonFieldsBeforeAndAfterExecution(bool execute)
    {
        var player = Game.Current.GetCurrentPlayer();
        var tile = World.Current.Map[2, 2];
        var ordinary = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
        var hero = player.HireHero(tile);
        var command = new DisbandArmiesCommand(TestUtilities.CreateArmyController(), new List<Army> { hero, ordinary }) { Id = 42 };
        if (execute) Assert.That(command.Execute(), Is.EqualTo(ActionState.Succeeded));

        var before = StableState();
        var entity = Snapshot(command);
        Assert.That(entity.RequestedArmyIds, Is.EqualTo(new int?[] { hero.Id, ordinary.Id }));
        Assert.That(entity.Id, Is.EqualTo(42));
        Assert.That(entity.PlayerIndex, Is.EqualTo(Game.Current.Players.IndexOf(player)));
        Assert.That(entity.Result, Is.EqualTo(command.Result));
        var loaded = JsonConvert.DeserializeObject<DisbandArmiesCommandEntity>(JsonConvert.SerializeObject(entity));
        Assert.That(loaded.RequestedArmyIds, Is.EqualTo(entity.RequestedArmyIds));
        Assert.That(loaded.Id, Is.EqualTo(entity.Id));
        Assert.That(loaded.PlayerIndex, Is.EqualTo(entity.PlayerIndex));
        Assert.That(loaded.Result, Is.EqualTo(entity.Result));
        Assert.That(StableState(), Is.EqualTo(before));
        Assert.That(hero.IsDead, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RejectedNullOrEmptyRequest_SnapshotsWithoutInventingAnOwner(bool nullBatch)
    {
        var command = new DisbandArmiesCommand(TestUtilities.CreateArmyController(), nullBatch ? null : new List<Army>());
        Assert.That(command.Execute(), Is.EqualTo(ActionState.Failed));
        var entity = Snapshot(command);
        Assert.That(entity.PlayerIndex, Is.EqualTo(-1));
        Assert.That(entity.Result, Is.EqualTo(ActionState.Failed));
        if (nullBatch) Assert.That(entity.RequestedArmyIds, Is.Null);
        else Assert.That(entity.RequestedArmyIds, Is.Empty);
    }

    [Test]
    public void RejectedBatch_RetainsNullsDuplicatesAndOriginalOrder()
    {
        var ordinary = Game.Current.GetCurrentPlayer().ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[2, 2]);
        var command = new DisbandArmiesCommand(TestUtilities.CreateArmyController(), new List<Army> { ordinary, null, ordinary });
        Assert.That(command.Execute(), Is.EqualTo(ActionState.Failed));
        Assert.That(Snapshot(command).RequestedArmyIds, Is.EqualTo(new int?[] { ordinary.Id, null, ordinary.Id }));
        Assert.That(ordinary.IsDead, Is.False);
    }

    private static DisbandArmiesCommandEntity Snapshot(Command command) =>
        (DisbandArmiesCommandEntity)CommandPersistence.SnapshotCommands(new List<Command> { command }).Single();

    private static string StableState()
    {
        var snapshot = Game.Current.Snapshot();
        snapshot.Timestamp = DateTime.MinValue;
        return JsonConvert.SerializeObject(snapshot);
    }
}
