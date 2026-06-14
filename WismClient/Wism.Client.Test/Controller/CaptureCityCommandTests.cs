using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.Commands.Cities;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Controller
{
    [TestFixture]
    public class CaptureCityCommandTests
    {
        [Test]
        public void CaptureCityCommand_CapturesEmptyEnemyCityAndMovesSelectedStack()
        {
            // Assemble
            var controllers = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllers, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllers);

            var player = Game.Current.Players[0];
            var origin = World.Current.Map[6, 4];
            var target = World.Current.Map[7, 4];
            var army = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), origin);
            TestUtilities.Select(controllers, origin.GetAllArmies());

            // Act
            var result = TestUtilities.ExecuteCommandUntilDone(
                controllers.CommandController,
                new CaptureCityCommand(controllers.CityController, player, Game.Current.GetSelectedArmies(), target.City));

            // Assert
            Assert.That(result, Is.EqualTo(ActionState.Succeeded));
            Assert.That(target.City.Clan, Is.EqualTo(player.Clan));
            Assert.That(target.GetAllArmies().Single(), Is.EqualTo(army));
            Assert.That(origin.GetAllArmies(), Is.Empty);
            Assert.That(Game.Current.GameState, Is.EqualTo(GameState.Ready));
        }

        [Test]
        public void CaptureCityCommand_CapturesEmptyEnemyCityWithUnselectedStackWithoutLeavingVisitors()
        {
            // Assemble
            var controllers = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllers, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllers);

            var player = Game.Current.Players[0];
            var origin = World.Current.Map[6, 4];
            var target = World.Current.Map[7, 4];
            var army = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), origin);

            // Act
            var result = TestUtilities.ExecuteCommandUntilDone(
                controllers.CommandController,
                new CaptureCityCommand(controllers.CityController, player, origin.GetAllArmies(), target.City));

            // Assert
            Assert.That(result, Is.EqualTo(ActionState.Succeeded));
            Assert.That(target.City.Clan, Is.EqualTo(player.Clan));
            Assert.That(target.HasVisitingArmies(), Is.False);
            Assert.That(target.GetAllArmies().Single(), Is.EqualTo(army));
            Assert.That(origin.GetAllArmies(), Is.Empty);
            Assert.That(Game.Current.GameState, Is.EqualTo(GameState.Ready));
        }

        [Test]
        public void CaptureCityCommand_DoesNotOverwriteHostileVisitingArmyOnCityFootprint()
        {
            var controllers = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllers, TestUtilities.DefaultTestWorld);
            TestUtilities.StartTurn(controllers);

            var player = Game.Current.Players[0];
            var enemy = Game.Current.Players[1];
            var origin = World.Current.Map[6, 4];
            var target = World.Current.Map[7, 4];
            var army = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), origin);
            var hostile = enemy.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[1, 1]);

            hostile.Tile.RemoveArmies(new List<Army> { hostile });
            target.VisitingArmies = new List<Army> { hostile };
            hostile.Tile = target;

            TestUtilities.Select(controllers, origin.GetAllArmies());

            var result = TestUtilities.ExecuteCommandUntilDone(
                controllers.CommandController,
                new CaptureCityCommand(controllers.CityController, player, Game.Current.GetSelectedArmies(), target.City));

            Assert.That(result, Is.EqualTo(ActionState.Failed));
            Assert.That(target.VisitingArmies.Single(), Is.EqualTo(hostile));
            Assert.That(hostile.Tile, Is.EqualTo(target));
            Assert.That(hostile.IsDead, Is.False);
            Assert.That(player.GetArmies(), Does.Contain(army));
        }
    }
}
