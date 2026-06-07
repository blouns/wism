using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI
{
    [TestFixture]
    public class SimpleStrategicModuleTests
    {
        [Test]
        public void AllocateAssets_RejectsLowerUtilityBidThatOverlapsAnyReservedArmy()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];
            var tile = World.Current.Map[6, 4];
            var army1 = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
            var army2 = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), tile);
            var module = new NoOpTacticalModule();
            var highUtilityStackBid = new SimpleBid(new List<Army> { army1, army2 }, module, 6.0);
            var overlappingBid = new SimpleBid(new List<Army> { army2 }, module, 5.0);

            var strategic = new SimpleStrategicModule();
            strategic.AllocateAssets(new IBid[] { overlappingBid, highUtilityStackBid });

            var accepted = strategic.GetAcceptedBids().ToList();

            Assert.That(accepted, Is.EqualTo(new[] { highUtilityStackBid }));
        }

        [Test]
        public void AllocateAssets_AcceptsIndependentBidsInDescendingUtilityOrder()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);

            var player = Game.Current.Players[0];
            var lowUtilityArmy = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[6, 4]);
            var highUtilityArmy = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[6, 5]);
            var module = new NoOpTacticalModule();
            var lowUtilityBid = new SimpleBid(new List<Army> { lowUtilityArmy }, module, 2.0);
            var highUtilityBid = new SimpleBid(new List<Army> { highUtilityArmy }, module, 9.0);

            var strategic = new SimpleStrategicModule();
            strategic.AllocateAssets(new IBid[] { lowUtilityBid, highUtilityBid });

            var accepted = strategic.GetAcceptedBids().ToList();

            Assert.That(accepted, Is.EqualTo(new[] { highUtilityBid, lowUtilityBid }));
        }

        private sealed class NoOpTacticalModule : ITacticalModule
        {
            public IEnumerable<IBid> GenerateBids(World world)
            {
                return Enumerable.Empty<IBid>();
            }

            public IEnumerable<ICommandAction> GenerateCommands(List<Army> armies, World world)
            {
                return Enumerable.Empty<ICommandAction>();
            }
        }
    }
}
