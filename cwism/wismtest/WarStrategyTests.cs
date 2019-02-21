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
