using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit
{
    [TestFixture]
    public class BoonTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        [SetUp]
        public void Setup()
        {
            Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
        }

        [Test]
        public void Redeem_Throne_GodsListen()
        {
            // Assemble
            Game.Current.Random.Next();         // Cycle random to get to Listen roll (lazy) - 50% chance
            var tile = World.Current.Map[2, 2];
            var boon = new ThroneBoon();            

            // Set up hero
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);
            Game.Current.SelectArmies(new List<Army>() { hero });

            // Act            
            var result = boon.Redeem(tile);

            // Assert
            Assert.IsNotNull(boon.Result);
            Assert.IsNotNull(result);
            Assert.AreEqual(result, boon.Result);
            Assert.AreEqual(1, result);

            Assert.AreEqual(5+1, hero.Strength);
        }

        [Test]
        public void Redeem_Throne_GodsIgnore()
        {
            // Assemble
            for (int i = 0; i < 8; i++)
            {
                Game.Current.Random.Next();         // Cycle random to get to Ignore roll (lazy) - 30% chance
            }            
            
            var tile = World.Current.Map[2, 2];
            var boon = new ThroneBoon();

            // Set up hero
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);
            Game.Current.SelectArmies(new List<Army>() { hero });

            // Act            
            var result = boon.Redeem(tile);

            // Assert
            Assert.IsNotNull(boon.Result);
            Assert.IsNotNull(result);
            Assert.AreEqual(result, boon.Result);
            Assert.AreEqual(0, result);

            Assert.AreEqual(5 + 0, hero.Strength);
        }

        [Test]
        public void Redeem_Throne_GodsAngry()
        {
            // Assemble            
            var tile = World.Current.Map[2, 2];
            var boon = new ThroneBoon();

            // Set up hero
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);
            Game.Current.SelectArmies(new List<Army>() { hero });

            // Act            
            var result = boon.Redeem(tile);

            // Assert
            Assert.IsNotNull(boon.Result);
            Assert.IsNotNull(result);
            Assert.AreEqual(result, boon.Result);
            Assert.AreEqual(-1, result);

            Assert.AreEqual(5 - 1, hero.Strength);
        }

        [Test]
        public void Redeem_Allies_OneAlly()
        {
            // Assemble
            //Game.Current.Random.Next();         // Cycle random to get to Listen roll (lazy) - 50% chance
            var tile = World.Current.Map[2, 2];
            var boon = new AlliesBoon(ModFactory.FindArmyInfo("Devils"));

            // Set up hero
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);
            Game.Current.SelectArmies(new List<Army>() { hero });

            // Act            
            var result = boon.Redeem(tile);

            // Assert
            Assert.IsNotNull(boon.Result);
            Assert.IsNotNull(result);
            Assert.AreEqual(result, boon.Result);
            Assert.IsTrue(result is Army[]);
            var armies = result as Army[];
            Assert.AreEqual(1, armies.Length);
            Assert.AreEqual("Devils", armies[0].ShortName);
            Assert.AreEqual(hero.Clan, armies[0].Clan);
            Assert.AreEqual(tile, armies[0].Tile);
            Assert.AreEqual(1, tile.Armies.Count);
            Assert.AreEqual(1, tile.VisitingArmies.Count);
        }

        [Test]
        public void Redeem_Allies_TwoAllies()
        {
            // Assemble
            Game.Current.Random.Next();         // Cycle random to get to "two allies" roll (lazy)
            var tile = World.Current.Map[2, 2];
            var boon = new AlliesBoon(ModFactory.FindArmyInfo("Dragons"));

            // Set up hero
            var player1 = Game.Current.Players[0];
            var hero = player1.HireHero(tile);
            Game.Current.SelectArmies(new List<Army>() { hero });

            // Act            
            var result = boon.Redeem(tile);

            // Assert
            Assert.IsNotNull(boon.Result);
            Assert.IsNotNull(result);
            Assert.AreEqual(result, boon.Result);
            Assert.IsTrue(result is Army[]);
            var armies = result as Army[];
            Assert.AreEqual(2, armies.Length);
            Assert.AreEqual("Dragons", armies[0].ShortName);
            Assert.AreEqual(hero.Clan, armies[0].Clan);
            Assert.AreEqual(tile, armies[0].Tile);
            Assert.AreEqual("Dragons", armies[1].ShortName);
            Assert.AreEqual(hero.Clan, armies[1].Clan);            
            Assert.AreEqual(tile, armies[1].Tile);
            Assert.AreEqual(2, tile.Armies.Count);
            Assert.AreEqual(1, tile.VisitingArmies.Count);
        }


        [Test]
        public void Redeem_Artifact_Firesword()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];

            // Set up artifact
            var artifact = new Artifact(
                ModFactory.FindArtifactInfo("Firesword"));
            var boon = new ArtifactBoon(artifact);

            // Act            
            var result = boon.Redeem(tile);

            // Assert
            Assert.IsNotNull(boon.Result);
            Assert.IsNotNull(result);
            Assert.AreEqual(result, boon.Result);
            Assert.AreEqual(artifact, result);
            Assert.AreEqual(boon.Artifact, result);
            Assert.AreEqual(tile, artifact.Tile);
        }


        [Test]
        public void Redeem_Gold_Player1()
        {
            // Assemble
            var tile = World.Current.Map[2, 2];

            // Set up artifact
            var boon = new GoldBoon();

            // Set up hero
            Game.CreateDefaultGame(TestUtilities.DefaultTestWorld);
            var player1 = Game.Current.Players[0];
            int initialGold = player1.Gold;
            var hero = player1.HireHero(tile);
            Game.Current.SelectArmies(new List<Army>() { hero });

            // Act            
            var result = boon.Redeem(tile);

            // Assert
            Assert.IsNotNull(boon.Result);
            Assert.IsNotNull(result);
            Assert.AreEqual(result, boon.Result);
            Assert.IsTrue(result is int);
            Assert.IsTrue(initialGold < player1.Gold);
        }
    }
}
