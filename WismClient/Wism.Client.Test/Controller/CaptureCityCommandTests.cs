using System.Linq;
using NUnit.Framework;
using Wism.Client.Commands.Cities;
using Wism.Client.Controllers;
using Wism.Client.Core;
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
    }
}
