using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BranallyGames.Wism;

namespace wism.Tests
{
    [TestFixture]
    public class WarStrategyTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;            
        }

        [SetUp]
        public void SetupWorldWithTwoPlayers()
        {
            World.CreateDefaultWorld();
            Player orcs = CreatePlayer("Orcs of Kor");
            Player elves = CreatePlayer("Elvallie");
            
            World.Current.Reset();
            World.Current.Random = new Random(1990);
            World.Current.Players.Clear();
            World.Current.Players.Add(orcs);
            World.Current.Players.Add(elves);

            orcs.HireHero(World.Current.Map[1, 1]);
            orcs.ConscriptArmy(UnitInfo.GetUnitInfo("LightInfantry"), World.Current.Map[1, 2]);

            elves.HireHero(World.Current.Map[3, 1]);
            elves.ConscriptArmy(UnitInfo.GetUnitInfo("LightInfantry"), World.Current.Map[3, 2]);
        }

        [Test]
        public void CreateTest()
        {
            IWarStrategy war = new DefaultWarStrategy();
            Assert.IsNotNull(war);
        }

        [Test]
        public void AttackTest()
        {
            Army attacker = World.Current.Players[0].GetArmies()[0];
            Tile tile = World.Current.Map[3, 2];
            
            IWarStrategy war = new DefaultWarStrategy();
            Assert.IsTrue(war.Attack(attacker, tile));
        }

        [Test]
        public void AttackOnceWinTest()
        {
            Army attacker = World.Current.Players[0].GetArmies()[0];
            Tile tile = World.Current.Map[3, 2];

            IWarStrategy war = new DefaultWarStrategy();
            Assert.IsTrue(war.AttackOnce(attacker, tile));
        }

        [Test]
        public void AttackOnceLoseTest()
        {
            World.CreateDefaultWorld();
            Player player1 = World.Current.Players[0];
            Tile tile = World.Current.Map[2, 2];
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            
            Player player2 = World.Current.Players[1];
            tile = World.Current.Map[3, 2];            
            player2.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);

            IWarStrategy war = new DefaultWarStrategy();
            Assert.IsFalse(war.AttackOnce(player1.GetArmies()[0], tile));
        }

        [Test]
        public void AttackUntilWinTest()
        {
            World.CreateDefaultWorld();
            Player player1 = World.Current.Players[0];
            Tile tile = World.Current.Map[2, 2];
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);

            Player player2 = World.Current.Players[1];
            tile = World.Current.Map[3, 2];
            player2.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);

            Army attacker = World.Current.Players[0].GetArmies()[0];
            IWarStrategy war = new DefaultWarStrategy();

            while (attacker.Size > 0 && tile.Army.Size > 0)
            {
                bool won = war.AttackOnce(attacker, tile);
            }

            Assert.IsTrue(attacker.Size > 0, "Defender was not supposed to win.");
        }

        [Test]
        public void AttackUntilLoseTest()
        {
            World.CreateDefaultWorld();
            Player player1 = World.Current.Players[0];
            Tile tile = World.Current.Map[2, 2];
            player1.HireHero(tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);
            player1.ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), tile);

            Player player2 = World.Current.Players[1];
            tile = World.Current.Map[3, 2];
            player2.HireHero(tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);
            player2.ConscriptArmy(ModFactory.FindUnitInfo("HeavyInfantry"), tile);

            Army attacker = World.Current.Players[0].GetArmies()[0];
            IWarStrategy war = new DefaultWarStrategy();

            while (attacker.Size > 0 && tile.Army.Size > 0)
            {
                bool won = war.AttackOnce(attacker, tile);
            }

            Assert.IsTrue(attacker.Size == 0, "Attacker was not supposed to win.");
        }


        #region Helper methods        

        private static Player CreatePlayer(string name)
        {
            Player player = new Player();

            IList<Affiliation> affiliationKinds = ModFactory.LoadAffiliations(ModFactory.ModPath);
            foreach (Affiliation affiliation in affiliationKinds)
            {
                if (affiliation.DisplayName == name)
                {
                    player.Affiliation = affiliation;
                    break;
                }
            }

            return player;
        }

        #endregion
    }
}
