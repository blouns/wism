using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.AI.Adapta;
using Wism.Client.AI.Adapta.StrategicModules.ValuationStrategies;
using Wism.Client.AI.Adapta.TacticalModules;
using Wism.Client.AI.Intelligence;
using Wism.Client.Core;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI
{
    [TestFixture]
    public class ExpandAndConquerNeutralValuationStrategyTests
    {
        private ExpandAndConquerNeutralValuationStrategy strategy;
        private Player player;
        private World world;

        [SetUp]
        public void SetUp()
        {
            var controller = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controller, "AiTestWorld");
            world = World.Current;
            player = Game.Current.Players[0];
            strategy = new ExpandAndConquerNeutralValuationStrategy(world, player);
        }

        [Test]
        public void CalculateValue_ReturnsZero_ForNonConquestTactic()
        {
            var dummyTactic = new DummyTactic();
            var value = strategy.CalculateValue(dummyTactic, new List<Army>(), new List<City>(), new TargetPortfolio());
            Assert.That(value, Is.EqualTo(0f));
        }

        [Test]
        public void CalculateValue_ReturnsPositive_ForReachableNeutralCity()
        {
            var armyInfo = ModFactory.FindArmyInfo("LightInfantry");
            var army = ArmyFactory.CreateArmy(player, armyInfo);
            var tile = world.Map[1, 1];
            tile.AddArmy(army);

            var city = world.Map[1, 2].City; // Marthos

            var tactic = new ConquerNeutralCitiesTactic(TestUtilities.CreateControllerProvider());
            var portfolio = new TargetPortfolio { NeutralCities = new List<City> { city } };

            var value = strategy.CalculateValue(tactic, new List<Army> { army }, new List<City>(), portfolio);
            Assert.That(value, Is.GreaterThan(0f));
        }

        [Test]
        public void CalculateValue_FavorsCloserCity_OverDistantCity()
        {
            var armyInfo = ModFactory.FindArmyInfo("LightInfantry");
            var army = ArmyFactory.CreateArmy(player, armyInfo);
            var tile = world.Map[1, 1];
            tile.AddArmy(army);

            var closeCity = world.Map[1, 2].City; // Marthos
            var farCity = world.Map[9, 18].City; // Khamar

            var tactic = new ConquerNeutralCitiesTactic(TestUtilities.CreateControllerProvider());
            var portfolio = new TargetPortfolio { NeutralCities = new List<City> { closeCity, farCity } };

            var closeOnly = strategy.CalculateValue(tactic, new List<Army> { army }, new List<City>(), new TargetPortfolio { NeutralCities = new List<City> { closeCity } });
            var farOnly = strategy.CalculateValue(tactic, new List<Army> { army }, new List<City>(), new TargetPortfolio { NeutralCities = new List<City> { farCity } });

            Assert.That(closeOnly, Is.GreaterThan(farOnly), "Closer city should yield higher value when production is equal.");
        }

        [Test]
        public void CalculateValue_FavorsCloserCityEvenIfDistantCityHasBetterProduction()
        {
            var controller = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controller, TestUtilities.DefaultTestWorld);
            var strategy = new ExpandAndConquerNeutralValuationStrategy(World.Current, Game.Current.Players[0]);

            var army = Game.Current.Players[0].HireHero(World.Current.Map[1, 1]);

            // Close City: Weak production
            var closeCity = World.Current.Map[1, 2].City; // Marthos

            // Distant City: Strong production (e.g., Gunthang with Pegasus, Elven Archers, etc.)
            var farCity = World.Current.Map[9, 18].City; // Khamar

            var tactic = new ConquerNeutralCitiesTactic(controller);
            var closeVal = strategy.CalculateValue(tactic, new List<Army> { army }, new List<City>(), new TargetPortfolio { NeutralCities = new List<City> { closeCity } });
            var farVal = strategy.CalculateValue(tactic, new List<Army> { army }, new List<City>(), new TargetPortfolio { NeutralCities = new List<City> { farCity } });

            Assert.That(closeVal, Is.GreaterThan(farVal), "Closer low-value city should be prioritized over high-value distant one.");
        }

        private class DummyTactic : TacticalModule
        {
            public DummyTactic() : base(TestUtilities.CreateControllerProvider()) { }

            public override Dictionary<Army, Bid> GenerateArmyBids(List<Army> myArmies, List<City> myCities, TargetPortfolio targets)
            {
                return new Dictionary<Army, Bid>();
            }

            public override void AssignAssets(Bid bid)
            {
                // Implementation can be left empty for testing purposes
            }
        }
    }
}
