using NUnit.Framework;
using System.Collections.Generic;
using Wism.Client.Agent.Commands;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Integration
{
    [TestFixture]
    public class WarlordsScenarioTests
    {
        /// <summary>
        /// Scenario: Multiple armies moving and attacking independently and regrouping 
        /// to attack as one across a series of turns.
        /// </summary>
        [Test]
        public void TwoClanFourHeroDual()
        {
            // Assemble
            var armyController = TestUtilities.CreateArmyController();
            var commandController = TestUtilities.CreateCommandController();
            var gameController = TestUtilities.CreateGameController();

            // Scenario setup
            //  =========================================================================================
            // (0, 0):[M,]     (1, 0):[M,]     (2, 0):[M,]     (3, 0):[M,]     (4, 0):[M,]     (5, 0):[M,]
            // (0, 1):[M,]     (1, 1):[S,H]    (2, 1):[G,]     (3, 1):[G,]     (4, 1):[G,]     (5, 1):[M,]
            // (0, 2):[M,]     (1, 2):[S,H]    (2, 2):[G,]     (3, 2):[G,]     (4, 2):[G,]     (5, 2):[M,]
            // (0, 3):[M,]     (1, 3):[G,]     (2, 3):[G,]     (3, 3):[G,]     (4, 3):[L,H]    (5, 3):[M,]
            // (0, 4):[M,]     (1, 4):[G,]     (2, 4):[G,]     (3, 4):[G,]     (4, 4):[L,H]    (5, 4):[M,]
            // (0, 5):[M,]     (1, 5):[M,]     (2, 5):[M,]     (3, 5):[M,]     (4, 5):[M,]     (5, 5):[M,]
            //  =========================================================================================            
            Game.CreateDefaultGame();

            // Initial Sirians setup
            Player sirians = Game.Current.Players[0];
            Tile tile1 = World.Current.Map[1, 1];
            Tile tile2 = World.Current.Map[1, 2];
            sirians.HireHero(tile1);
            sirians.HireHero(tile2);            
            var siriansHero1 = new List<Army>(tile1.Armies);
            var siriansHero2 = new List<Army>(tile2.Armies);

            // Initial Lord Bane setup
            Player lordBane = Game.Current.Players[1];
            var tile3 = World.Current.Map[4, 3];
            var tile4 = World.Current.Map[4, 4];
            lordBane.HireHero(tile3);
            lordBane.HireHero(tile4);
            var lordBaneHero1 = new List<Army>(tile3.Armies);
            var lordBaneHero2 = new List<Army>(tile4.Armies);

            // Act

            // Turn 1
            //  =========================================================================================
            // (0, 0):[M,]     (1, 0):[M,]     (2, 0):[M,]     (3, 0):[M,]     (4, 0):[M,]     (5, 0):[M,]
            // (0, 1):[M,]     (1, 1):[G,]     (2, 1):[G,]     (3, 1):[G,]     (4, 1):[G,]     (5, 1):[M,]
            // (0, 2):[M,]     (1, 2):[G,]     (2, 2):[G,]     (3, 2):[G,]     (4, 2):[S,H]    (5, 2):[M,]
            // (0, 3):[M,]     (1, 3):[G,]     (2, 3):[G,]     (3, 3):[S,H]    (4, 3):[L,H]    (5, 3):[M,]
            // (0, 4):[M,]     (1, 4):[G,]     (2, 4):[G,]     (3, 4):[G,]     (4, 4):[L,H]    (5, 4):[M,]
            // (0, 5):[M,]     (1, 5):[M,]     (2, 5):[M,]     (3, 5):[M,]     (4, 5):[M,]     (5, 5):[M,]
            //  =========================================================================================  
            TestUtilities.Select(commandController, armyController,
                siriansHero1);
            TestUtilities.MoveUntilDone(commandController, armyController,
                siriansHero1, 4, 2);
            TestUtilities.Deselect(commandController, armyController,
                siriansHero1);

            TestUtilities.Select(commandController, armyController,
                siriansHero2);
            TestUtilities.MoveUntilDone(commandController, armyController,
                siriansHero2, 3, 3);
            TestUtilities.Deselect(commandController, armyController,
                siriansHero2);

            TestUtilities.EndTurn(commandController, gameController);

            // Turn 2
            //  =========================================================================================
            // (0, 0):[M,]     (1, 0):[M,]     (2, 0):[M,]     (3, 0):[M,]     (4, 0):[M,]     (5, 0):[M,]
            // (0, 1):[M,]     (1, 1):[L,H]    (2, 1):[G,]     (3, 1):[G,]     (4, 1):[G,]     (5, 1):[M,]
            // (0, 2):[M,]     (1, 2):[G,]     (2, 2):[G,]     (3, 2):[G,]     (4, 2):[S,H]    (5, 2):[M,]
            // (0, 3):[M,]     (1, 3):[G,]     (2, 3):[G,]     (3, 3):[L,H]    (4, 3):[G,]     (5, 3):[M,]
            // (0, 4):[M,]     (1, 4):[G,]     (2, 4):[G,]     (3, 4):[G,]     (4, 4):[G,]     (5, 4):[M,]
            // (0, 5):[M,]     (1, 5):[M,]     (2, 5):[M,]     (3, 5):[M,]     (4, 5):[M,]     (5, 5):[M,]
            //  =========================================================================================  
            // Attack and lose
            TestUtilities.Select(commandController, armyController,
                lordBaneHero1);
            TestUtilities.AttackUntilDone(commandController, armyController,
                lordBaneHero1, 3, 3);       
            
            // Run away
            TestUtilities.Select(commandController, armyController,
                lordBaneHero2);
            TestUtilities.MoveUntilDone(commandController, armyController,
                lordBaneHero2, 1, 4);
            TestUtilities.MoveUntilDone(commandController, armyController,
                lordBaneHero2, 1, 1);
            TestUtilities.Deselect(commandController, armyController,
                lordBaneHero2);

            TestUtilities.EndTurn(commandController, gameController);

            // Turn 3
            //  =========================================================================================
            // (0, 0):[M,]     (1, 0):[M,]     (2, 0):[M,]     (3, 0):[M,]     (4, 0):[M,]     (5, 0):[M,]
            // (0, 1):[M,]     (1, 1):[L,H]    (2, 1):[G,]     (3, 1):[G,]     (4, 1):[G,]     (5, 1):[M,]
            // (0, 2):[M,]     (1, 2):[G,]     (2, 2):[G,]     (3, 2):[G,]     (4, 2):[S,H]    (5, 2):[M,]
            // (0, 3):[M,]     (1, 3):[G,]     (2, 3):[G,]     (3, 3):[S,H]    (4, 3):[G,]     (5, 3):[M,]
            // (0, 4):[M,]     (1, 4):[G,]     (2, 4):[G,]     (3, 4):[G,]     (4, 4):[G,]     (5, 4):[M,]
            // (0, 5):[M,]     (1, 5):[M,]     (2, 5):[M,]     (3, 5):[M,]     (4, 5):[M,]     (5, 5):[M,]
            //  =========================================================================================   
            // Chase Lord Bane!
            TestUtilities.Select(commandController, armyController,
                siriansHero1);
            TestUtilities.MoveUntilDone(commandController, armyController,
                Game.Current.GetSelectedArmies(), 2, 2);
            TestUtilities.Deselect(commandController, armyController,
                Game.Current.GetSelectedArmies());

            TestUtilities.Select(commandController, armyController,
                siriansHero2);
            TestUtilities.MoveUntilDone(commandController, armyController,
                Game.Current.GetSelectedArmies(), 2, 2);
            TestUtilities.Deselect(commandController, armyController,
                Game.Current.GetSelectedArmies());

            // Attack with dual heros!            
            TestUtilities.Select(commandController, armyController,
                World.Current.Map[2, 2].Armies);
            TestUtilities.AttackUntilDone(commandController, armyController,
                Game.Current.GetSelectedArmies(), 1, 1);
            
            TestUtilities.EndTurn(commandController, gameController);

            // Assert
            Assert.AreEqual(0, lordBane.GetArmies().Count, "Lord Bane is not yet defeated!");
            Assert.AreEqual(2, sirians.GetArmies().Count, "Sirians army took more losses than expected!");
            Assert.AreEqual(4, Game.Current.Turn, "Unexpected player's turn");
            Assert.AreEqual("LordBane", Game.Current.GetCurrentPlayer().Clan.ShortName, "Unexpected player's turn");
        }
    }
}
