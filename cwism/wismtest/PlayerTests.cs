using BranallyGames.Wism;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.IO;

namespace wism.Tests
{
    [TestFixture]
    public class PlayerTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        [SetUp]
        public void Setup()
        {
            World.CreateDefaultWorld();
        }

        [Test]
        public void CreateTest()
        {
            World.Current.Players.Clear();
            Player player = CreateOrcsOfKorPlayer();
            Assert.IsNotNull(player);
        }

        [Test]
        public void ConscriptUnitsTest()
        {
            Player player = CreateOrcsOfKorPlayer();

            IList<UnitInfo> unitKinds = GetUnitKinds();
            for (int i = 0; i < unitKinds.Count; i++)
            {
                UnitInfo info = unitKinds[i];
                Tile tile = CreateTile("Grass", i, i + 1);

                player.ConscriptArmy(info, tile);
            }

            IList<Army> armies = player.GetArmies();
            Assert.IsNotNull(armies, "Player had null units.");
            Assert.IsTrue(armies.Count > 0, "Count of units was not > 0.");
        }        

        [Test]
        public void ConscriptUnitOnNontraversableTileTest()
        {
            void ConscriptUnit()
            {
                Player player = CreateOrcsOfKorPlayer();

                IList<UnitInfo> unitKinds = GetUnitKinds();
                UnitInfo info = unitKinds[0];

                // Add player to Void; should fail
                Tile tile = CreateTile("Void", 0, 0);
                player.ConscriptArmy(info, tile);
            }

            Assert.Throws<ArgumentException>(
                ConscriptUnit, "Failed to conscript with unexpected exception or deployed incorrectly.");
        }

        [Test]
        public void HireHeroTest()
        {
            Player player = CreateOrcsOfKorPlayer();
            player.HireHero();

            IList<Army> armies = player.GetArmies();
            Assert.IsNotNull(armies, "Player had null armies.");
            Assert.IsTrue(armies.Count > 0, "Count of armies was not > 0.");
        }

        [Test]
        public void NextPlayerTest()
        {
            SetupWorldWithTwoPlayers();
            World world = World.Current;

            Player currentPlayer = world.GetCurrentPlayer();
            Assert.AreEqual("OrcsOfKor", currentPlayer.Affiliation.ID);

            currentPlayer = world.NextTurn();
            Assert.AreEqual("Elvallie", currentPlayer.Affiliation.ID);

            currentPlayer = world.NextTurn();
            Assert.AreEqual("OrcsOfKor", currentPlayer.Affiliation.ID);
        }

        #region Helper utility methods

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

        private Tile CreateTile(string id, int x, int y)
        {
            Tile tile = new Tile();
            tile.Coordinates = new Coordinates(x, y);
            tile.Terrain = GetTerrain(id);

            return tile;
        }

        private Terrain GetTerrain(string id)
        {
            IList<Terrain> terrains = ModFactory.LoadTerrains(ModFactory.ModPath);
            foreach (Terrain terrain in terrains)
            {
                if (terrain.ID == id)
                    return terrain;
            }

            throw new InvalidOperationException(
                String.Format("Could not find a '{0}' terrain.", id));
        }

        private static Player CreateOrcsOfKorPlayer()
        {
            Player player = new Player();

            IList<Affiliation> affiliationKinds = ModFactory.LoadAffiliations(ModFactory.ModPath);
            foreach (Affiliation affiliation in affiliationKinds)
            {
                if (affiliation.ID == "OrcsOfKor")
                {
                    player.Affiliation = affiliation;
                    break;
                }
            }
            Assert.AreEqual(player.Affiliation.DisplayName, "Orcs of Kor");

            return player;
        }

        private static IList<UnitInfo> GetUnitKinds()
        {
            string filePath = String.Format(@"{0}\{1}", ModFactory.ModPath, UnitInfo.FileName);
            return ModFactory.LoadModFiles<UnitInfo>(filePath);
        }

        #endregion
    }
}
