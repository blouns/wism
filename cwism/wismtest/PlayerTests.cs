﻿using BranallyGames.Wism;
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
            World.Current.Reset();
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
                Tile tile = CreateTile('m', i, i + 1);

                player.ConscriptUnit(info, tile);
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
                Tile tile = CreateTile('V', 0, 0);
                player.ConscriptUnit(info, tile);
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

        #region Helper utility methods

        private Tile CreateTile(char symbol, int x, int y)
        {
            Tile tile = new Tile();
            tile.Coordinate = new Coordinate(x, y);
            tile.Terrain = GetTerrain(symbol);

            return tile;
        }

        private Terrain GetTerrain(char symbol)
        {
            IList<Terrain> terrains = ModFactory.LoadTerrains(ModFactory.DefaultPath);
            foreach (Terrain terrain in terrains)
            {
                if (terrain.Symbol == symbol)
                    return terrain;
            }

            throw new InvalidOperationException(
                String.Format("Could not find a '{0}' terrain.", symbol));
        }

        private static Player CreateOrcsOfKorPlayer()
        {
            Player player = new Player();

            IList<Affiliation> affiliationKinds = ModFactory.LoadAffiliations(ModFactory.DefaultPath);
            foreach (Affiliation affiliation in affiliationKinds)
            {
                if (affiliation.DisplayName == "Orcs of Kor")
                    player.Affiliation = affiliation;
            }
            Assert.AreEqual(player.Affiliation.DisplayName, "Orcs of Kor");

            return player;
        }

        private static IList<UnitInfo> GetUnitKinds()
        {
            return ModFactory.LoadModFiles<UnitInfo>(ModFactory.DefaultPath, UnitInfo.FilePattern);
        }

        #endregion
    }
}