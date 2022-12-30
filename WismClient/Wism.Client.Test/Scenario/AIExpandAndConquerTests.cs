using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wism.Client.AI.CommandProviders;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Scenario
{
    [TestFixture]
    public class AIExpandAndConquerTests
    {
        /// <summary>
        /// Scenario: AI race to capture the neutral city, Deserton.
        /// </summary>
        [Test]
        public void CaptureNeutralCity_TwoAI()
        {
            // Assemble
            var controller = TestUtilities.CreateControllerProvider();
            var commander = new AdaptaCommandProvider(TestUtilities.CreateLogFactory(), controller);

            TestUtilities.NewGame(controller, TestUtilities.DefaultTestWorld);
            Game.Current.IgnoreGameOver = true;

            // Initial Sirians setup
            Player sirians = Game.Current.Players[0];
            sirians.IsHuman = false;
            Tile tile1 = World.Current.Map[3, 4];
            sirians.HireHero(tile1, 0);
            var siriansHero1 = new List<Army>(tile1.Armies);

            // Initial Lord Bane setup
            Player lordBane = Game.Current.Players[1];
            lordBane.IsHuman = false;
            var tile2 = World.Current.Map[7, 4];
            lordBane.HireHero(tile2, 0);
            var lordBaneHero1 = new List<Army>(tile2.Armies);

            // Act

            // Turn 1: Sirians: Start
            TestUtilities.ExecuteCurrentTurnAsAIUntilDone(controller, commander);

            // Turn 1: Sirians: End
            Assert.AreEqual(1, lordBane.Turn, "Expected to be on turn zero for next player.");
            Assert.AreEqual(lordBane, Game.Current.GetCurrentPlayer(), "Expected to be next player's turn.");
            Assert.AreEqual(2, sirians.GetCities().Count, "Expected to have conquered Deserton.");

            // Turn 1: Lord Bane: Start
            TestUtilities.StartTurn(controller);
            TestUtilities.ExecuteCurrentTurnAsAIUntilDone(controller, commander);

            // Turn 1: Lord Bane: End
            Assert.AreEqual(2, sirians.Turn, "Expected to be on next turn for next player.");
            Assert.AreEqual(sirians, Game.Current.GetCurrentPlayer(), "Expected to be next player's turn.");
        }
    }
}
