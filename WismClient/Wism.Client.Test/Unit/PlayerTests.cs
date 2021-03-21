using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Test.Unit
{
    [TestFixture]
    public class PlayerTests
    {
        [Test]
        public void StartTurn_RecruitHeroWithoutCities_NoHero()
        {
            // Assemble            
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];

            // Act
            player1.StartTurn();

            // Assert
            Assert.AreEqual(0, player1.LastHeroTurn);
            Assert.AreEqual(Int32.MaxValue, player1.NewHeroPrice);
        }

        [Test]
        public void StartTurn_RecruitHeroFirstTurn_NewHero()
        {
            // Assemble            
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];

            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            city.Tile = tile;
            player1.AddCity(city);

            // Act
            player1.StartTurn();

            // Assert
            Assert.AreEqual(0, player1.LastHeroTurn);
            Assert.AreEqual(0, player1.NewHeroPrice);
        }

        [Test]
        public void StartTurn_RecruitHeroTooSoon_NoHero()
        {
            // Assemble            
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];            

            var tile = World.Current.Map[1, 1];
            player1.HireHero(tile, 0);
            player1.Turn = 2;

            var city = MapBuilder.FindCity("Marthos");
            city.Tile = tile;
            player1.AddCity(city);

            // Act
            player1.StartTurn();

            // Assert
            Assert.AreEqual(1, player1.LastHeroTurn, "Last hero turn not set");
            Assert.AreEqual(Int32.MaxValue, player1.NewHeroPrice, "Hero should not be available");
        }

        [Test]
        public void StartTurn_RecruitHero10thTurn_NewHero()
        {
            // Assemble            
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];
            player1.Turn = 10;

            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            city.Tile = tile;
            player1.AddCity(city);

            // Act
            player1.StartTurn();

            // Assert
            Assert.AreEqual(0, player1.LastHeroTurn, "Last hero should be never (zero)");
            Assert.IsTrue(player1.NewHeroPrice >= Hero.MinGoldToHire, "Hero costs too little.");
            Assert.IsTrue(player1.NewHeroPrice < Hero.MaxGoldToHire, "Hero costs too much.");
        }

        [Test]
        public void StartTurn_RecruitHero20thTurnUnlucky_NoHero()
        {
            // Assemble            
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];
            player1.Turn = 10;

            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            city.Tile = tile;
            player1.AddCity(city);
            player1.StartTurn();
            player1.HireHero(tile, 0);
            
            // Skip a turn (back to Sirians)
            Game.Current.EndTurn();
            Game.Current.StartTurn();
            Game.Current.EndTurn();

            player1.Turn = 20;

            // Act
            player1.StartTurn();

            // Assert
            Assert.AreEqual(10, player1.LastHeroTurn, "Last hero should be 10");
            Assert.AreEqual(Int32.MaxValue, player1.NewHeroPrice, "Hero should not be available");
        }

        [Test]
        public void StartTurn_RecruitHero21thTurnLucky_NewHero()
        {
            // Assemble            
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];
            player1.Turn = 10;

            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            city.Tile = tile;
            player1.AddCity(city);
            player1.StartTurn();
            player1.HireHero(tile, 0);

            // Skip a turn (back to Sirians)
            Game.Current.EndTurn();
            Game.Current.StartTurn();
            Game.Current.EndTurn();

            player1.Turn = 20;

            // Skip a turn (back to Sirians)
            Game.Current.EndTurn();
            Game.Current.StartTurn();
            Game.Current.EndTurn();

            // Act
            player1.StartTurn();

            // Assert
            Assert.AreEqual(10, player1.LastHeroTurn, "Last hero should be 10");
            Assert.IsTrue(player1.NewHeroPrice >= Hero.MinGoldToHire, "Hero costs too little.");
            Assert.IsTrue(player1.NewHeroPrice < Hero.MaxGoldToHire, "Hero costs too much.");
        }

        [Test]
        public void StartTurn_RecruitHero10thHero_NoHero()
        {
            // Assemble            
            Game.CreateDefaultGame();
            Player player1 = Game.Current.Players[0];            

            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            city.Tile = tile;
            player1.AddCity(city);
            player1.HireHero(tile, 0);
            player1.HireHero(tile, 0);
            player1.HireHero(tile, 0);
            player1.HireHero(tile, 0);
            player1.HireHero(tile, 0);
            player1.HireHero(tile, 0);
            player1.HireHero(tile, 0);
            player1.HireHero(tile, 0);
            player1.HireHero(tile, 0);

            player1.Turn = 10;

            // Act
            player1.StartTurn();

            // Assert
            Assert.AreEqual(1, player1.LastHeroTurn, "Last hero turn not set");
            Assert.AreEqual(Int32.MaxValue, player1.NewHeroPrice, "Hero should not be available");
        }        
    }
}
