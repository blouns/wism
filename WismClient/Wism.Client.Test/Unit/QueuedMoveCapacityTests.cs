using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands;
using Wism.Client.Data;
using Wism.Client.Data.Entities.Army;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Pathing;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

[TestFixture]
public sealed class QueuedMoveCapacityTests
{
    [TestCase(true, true)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void QueuedRouteRespectsChangedCapacityWithoutChangingHumanMovement(bool revalidate, bool detour)
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        Game.CreateDefaultGame();
        var map = new Tile[7, 3];
        for (int x = 0; x < 7; x++)
            for (int y = 0; y < 3; y++)
                map[x, y] = new Tile { X = x, Y = y,
                    Terrain = Terrain.Create(ModFactory.FindTerrainInfo(y == 1 || detour ? "Grass" : "Water")) };
        World.CreateWorld(map);
        var player = Game.Current.GetCurrentPlayer();
        var armies = new List<Army>();
        for (int i = 0; i < 5; i++)
        {
            var army = player.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), map[0, 1]);
            army.MovesRemaining = 100;
            armies.Add(army);
        }
        var controller = TestUtilities.CreateArmyController();
        var command = new MoveOnceCommand(controller, armies, 6, 1) {
            Path = Enumerable.Range(0, 7).Select(x => map[x, 1]).ToList(),
            RevalidateFriendlyCapacity = revalidate
        };
        // The command was planned before the other stack arrived on its route.
        for (int i = 0; i < 5; i++)
            player.ConscriptArmy(ModFactory.FindArmyInfo("HeavyInfantry"), map[3, 1]);
        controller.SelectArmy(armies);
        var visited = new HashSet<Tile> { armies[0].Tile };
        var result = ActionState.InProgress;
        for (int step = 0; step < 30 && result == ActionState.InProgress; step++)
        {
            var before = armies[0].Tile;
            result = command.Execute();
            if (armies[0].Tile != before)
                Assert.That(visited.Add(armies[0].Tile), Is.True, "Must not double back.");
            Assert.That(map.Cast<Tile>().All(t => t.GetAllArmies().Count <= Army.MaxArmies), Is.True);
        }
        Assert.That(result, Is.EqualTo(revalidate && detour ? ActionState.Succeeded : ActionState.Failed));
        Assert.That(armies.All(a => a.Tile == (revalidate && detour ? map[6, 1] : revalidate ? map[0, 1] : map[2, 1])), Is.True);
        if (revalidate && !detour)
        {
            Assert.That(armies.All(a => a.MovesRemaining == 100), Is.True, "No route must not spend movement.");
            Assert.That(command.Execute(), Is.EqualTo(ActionState.Failed), "Retry must remain safe.");
        }
        var saved = (MoveCommandEntity)CommandPersistence.SnapshotCommands(new List<Command> { command })[0];
        Assert.Multiple(() => {
            Assert.That(saved.TargetX, Is.EqualTo(6));
            Assert.That(saved.TargetY, Is.EqualTo(1));
            Assert.That(saved.RevalidateFriendlyCapacity, Is.EqualTo(revalidate));
            Assert.That(saved.RouteChecked, Is.EqualTo(command.RouteChecked));
            Assert.That(saved.RouteRevalidationFailed, Is.EqualTo(revalidate && !detour));
        });
    }
}
