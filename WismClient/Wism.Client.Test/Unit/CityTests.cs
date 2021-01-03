using NUnit.Framework;
using System;
using Wism.Client.Core.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit
{
    [TestFixture]
    public class CityTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }

        [SetUp]
        public void Setup()
        {
            Game.CreateDefaultGame();
        }

        [Test]
        public void AddCity_City()
        {
            // Assemble
            var tile = World.Current.Map[1, 1];
            var nineGrid = tile.GetNineGrid();
            var city = MapBuilder.FindCity("Marthos");
            var expectedTerrain = MapBuilder.TerrainKinds["Castle"];

            // Act
            World.Current.AddCity(city, tile);

            // Assert
            Assert.AreEqual(tile, city.Tile);
            Assert.AreEqual("Marthos", city.DisplayName);
            
            var tiles = city.GetTiles();
            Assert.IsNotNull(tiles);
            for (int i = 0; i < 4; i++)
            {
                Assert.IsNotNull(tiles[i]);
                Assert.AreEqual(expectedTerrain, tiles[i].Terrain);                
                Assert.AreEqual(city, tiles[i].City);
            }

            Assert.AreEqual(0, city.MusterArmies().Count, "Did not expect any armies.");
        }

        [Test]
        public void Build_City()
        {
            // Assemble
            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            World.Current.AddCity(city, tile);
            var defense = city.Defense;

            // Act            
            bool result = city.TryBuild();

            // Assert
            Assert.IsTrue(result, $"Build unsuccessful on city: {city}");
            Assert.AreEqual(defense + 1, city.Defense, "Defense value did not increase after successful build.");

        }

        [Test]
        public void Raze_City()
        {
            // Assemble
            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            var expectedTerrain = MapBuilder.TerrainKinds["Ruins"];
            World.Current.AddCity(city, tile);

            // Act
            city.Raze();

            // Assert           
            var tiles = city.GetTiles();
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(expectedTerrain, tiles[i].Terrain);
                Assert.IsNull(tiles[i].City);
            }
        }

        [Test]
        public void Claim_City()
        {
            // Assemble
            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            var expectedTerrain = MapBuilder.TerrainKinds["Ruins"];
            var expectedPlayer = Game.Current.GetCurrentPlayer();

            World.Current.AddCity(city, tile);

            // Act
            city.Claim(expectedPlayer);

            // Assert
            var tiles = city.GetTiles();
            for (int i = 0; i < 4; i++)
            {
                Assert.IsNotNull(tiles[i]);
                Assert.AreEqual(tiles[i].City.Clan, city.Clan);
            }
            Assert.AreEqual(expectedPlayer.Clan, city.Clan);
        }

        [Test]
        public void Production_StartProduction_SufficientGold()
        {
            // Assemble
            Game.CreateDefaultGame();
            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            var expectedPlayer = Game.Current.GetCurrentPlayer();
            var armyInfo = ModFactory.FindArmyInfo("LightInfantry");
            var gold = expectedPlayer.Gold;
            World.Current.AddCity(city, tile);
            expectedPlayer.ClaimCity(city);

            // Act
            var result = city.Barracks.StartProduction(armyInfo);

            // Assert    
            Assert.IsTrue(result, "Production failed to start.");
            Assert.IsNotNull(city.Barracks.ArmyInTraining);
            Assert.IsNull(city.Barracks.ArmiesToDeliver);
            Assert.IsTrue(city.Barracks.ProducingArmy());
            Assert.IsFalse(city.Barracks.HasDeliveries());

            var ait = city.Barracks.ArmyInTraining;
            Assert.AreEqual(armyInfo, ait.ArmyInfo);
            Assert.AreEqual(1, ait.TurnsToProduce, "Turns to produce are off expectation");
            Assert.AreEqual(gold - 4, expectedPlayer.Gold, "Player's gold was off expectation.");
        }

        [Test]
        public void Production_StartProduction_InsufficientGold()
        {
            // Assemble
            Game.CreateDefaultGame();
            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            var expectedPlayer = Game.Current.GetCurrentPlayer();
            var armyInfo = ModFactory.FindArmyInfo("LightInfantry");            
            World.Current.AddCity(city, tile);
            expectedPlayer.ClaimCity(city);
            expectedPlayer.Gold = 0;

            // Act
            var result = city.Barracks.StartProduction(armyInfo);

            // Assert           
            Assert.IsFalse(result, "Production started even though there wasn't enough gold.");
            Assert.IsNull(city.Barracks.ArmyInTraining);
            Assert.IsFalse(city.Barracks.ProducingArmy());
        }

        [Test]
        public void Production_StartProduction_UnsupportedArmyKind()
        {
            // Assemble
            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            var expectedPlayer = Game.Current.GetCurrentPlayer();
            var armyInfo = ModFactory.FindArmyInfo("DwarvenLegion");
            World.Current.AddCity(city, tile);
            expectedPlayer.ClaimCity(city);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                city.Barracks.StartProduction(armyInfo)
            );       
        }

        [Test]
        public void Production_StopProduction()
        {
            // Assemble
            Game.CreateDefaultGame();
            var startingGold = 100;
            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            var player = Game.Current.GetCurrentPlayer();
            var armyInfo = ModFactory.FindArmyInfo("LightInfantry");

            World.Current.AddCity(city, tile);
            player.ClaimCity(city);

            player.Gold = startingGold;            
            var result = city.Barracks.StartProduction(armyInfo);        

            // Act
            city.Barracks.StopProduction();

            // Assert           
            Assert.IsTrue(result, "Production failed to start.");
            Assert.AreEqual(startingGold - 4, player.Gold, "Gold should have reflected the production costs.");
            Assert.IsNull(city.Barracks.ArmyInTraining);
            Assert.IsFalse(city.Barracks.ProducingArmy());
        }

        [Test]
        public void Production_CompleteProduction_LocalCity()
        {
            // Assemble
            Game.CreateDefaultGame();
            var tile = World.Current.Map[1, 1];
            var city = MapBuilder.FindCity("Marthos");
            var player = Game.Current.GetCurrentPlayer();
            var armyInfo = ModFactory.FindArmyInfo("LightInfantry");
            var gold = player.Gold;
            World.Current.AddCity(city, tile);
            player.ClaimCity(city);

            // Act
            TestContext.WriteLine("Starting production.");
            var result = city.Barracks.StartProduction(armyInfo);
            while(!city.Barracks.Produce())
            {
                TestContext.WriteLine("Produced one turn.");
            }

            // Assert    
            Assert.IsTrue(result, "Production failed to start.");
            Assert.IsFalse(city.Barracks.ProducingArmy());
            Assert.IsFalse(city.Barracks.HasDeliveries());

            Assert.AreEqual(1, player.GetArmies().Count, "Army was not deployed.");
            Assert.IsNotNull(tile.Armies, "Army was not deployed");
            Assert.AreEqual(1, tile.Armies.Count);

            // Army validation
            var army = tile.Armies[0];
            Assert.AreEqual(armyInfo.ShortName, army.ShortName, "Did not produce the correct army.");
            Assert.AreEqual(10, army.Moves);
            Assert.AreEqual(3, army.Strength);
            Assert.AreEqual(4, army.Upkeep);
        }

        [Test]
        public void Production_CompleteProduction_RemoteCity()
        {
            // Assemble
            Game.CreateDefaultGame();
            var player = Game.Current.GetCurrentPlayer();
            var armyInfo = ModFactory.FindArmyInfo("LightInfantry");
            int turnsToProduce = 0;
            int turnsToDeliver = 0;

            var marthos = MapBuilder.FindCity("Marthos");
            World.Current.AddCity(marthos, World.Current.Map[1, 1]);
            player.ClaimCity(marthos);

            var stormheim = MapBuilder.FindCity("Stormheim");
            World.Current.AddCity(stormheim, World.Current.Map[3, 3]);
            player.ClaimCity(stormheim);

            // Act
            TestContext.WriteLine("Starting production with delivery to Stormheim.");
            var result = marthos.Barracks.StartProduction(armyInfo, stormheim);
            do
            {
                TestContext.WriteLine("Produced one turn.");
                turnsToProduce++;
            } while (!marthos.Barracks.Produce());

            do
            {
                TestContext.WriteLine("Delivered one turn.");
                turnsToDeliver++;
            } while (!marthos.Barracks.Deliver());

            // Assert    
            Assert.IsTrue(result, "Production failed to start.");
            Assert.AreEqual(1, turnsToProduce, "Took an unexpected amount of time to produce.");
            Assert.AreEqual(3, turnsToDeliver, "Took an unexpected amount of time to deliver.");
            Assert.IsFalse(marthos.Barracks.ProducingArmy());
            Assert.IsFalse(marthos.Barracks.HasDeliveries());

            Assert.AreEqual(1, player.GetArmies().Count, "Army was not deployed.");
            Assert.IsNotNull(stormheim.Tile.Armies, "Army was not deployed.");
            Assert.AreEqual(1, stormheim.Tile.Armies.Count);
            Assert.IsNull(marthos.Tile.Armies, "Army was deployed to wrong city.");

            // Army validation
            var army = stormheim.Tile.Armies[0];
            Assert.AreEqual(armyInfo.ShortName, army.ShortName, "Did not produce the correct army.");
            Assert.AreEqual(10, army.Moves);
            Assert.AreEqual(3, army.Strength);
            Assert.AreEqual(4, army.Upkeep);
            Assert.AreEqual("Marthos 1st Light Infantry", army.DisplayName);
        }
    }
}
