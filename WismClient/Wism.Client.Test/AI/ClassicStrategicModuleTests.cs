using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands;
using Wism.Client.Core;
using Wism.Client.Data;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI
{
    [TestFixture]
    public class ClassicStrategicModuleTests
    {
        [Test]
        public void Planner_ReconcilesAndPersistsExplicitDesiredState()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            var player = Game.Current.GetCurrentPlayer();
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), player.Capitol.Tile);

            var plan = new ClassicStrategicPlanner().Reconcile(World.Current);

            Assert.That(plan, Is.Not.Null);
            Assert.That(plan.ClanShortName, Is.EqualTo(player.Clan.ShortName));
            Assert.That(plan.Objectives, Is.Not.Empty);
            Assert.That(plan.Objectives.Select(objective => objective.Kind), Does.Contain("Produce"));
            Assert.That(plan.Objectives.Any(objective => objective.Kind == "Expand" || objective.Kind == "Siege"), Is.True);
            Assert.That(Game.Current.StrategicPlans.Single().ClanShortName, Is.EqualTo(player.Clan.ShortName));
        }

        [Test]
        public void StrategicPlans_RoundTripThroughGameSnapshot()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            var player = Game.Current.GetCurrentPlayer();
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), player.Capitol.Tile);
            var original = new ClassicStrategicPlanner().Reconcile(World.Current);

            var snapshot = GamePersistance.SnapshotGame(Game.Current);
            GameFactory.Load(snapshot);

            var loaded = Game.Current.StrategicPlans.Single();
            Assert.That(loaded.ClanShortName, Is.EqualTo(original.ClanShortName));
            Assert.That(loaded.Revision, Is.EqualTo(original.Revision));
            Assert.That(loaded.Objectives.Select(objective => objective.Id), Is.EquivalentTo(original.Objectives.Select(objective => objective.Id)));
        }

        [Test]
        public void ClassicStrategicModule_PrioritizesBidMatchingStrategicObjective()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            var player = Game.Current.GetCurrentPlayer();
            var army1 = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), player.Capitol.Tile);
            var army2 = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), player.Capitol.Tile);
            var targetObjective = new ClassicStrategicPlanner()
                .Reconcile(World.Current)
                .Objectives
                .First(objective => !string.IsNullOrWhiteSpace(objective.TargetCityShortName));
            var module = new NoOpTacticalModule();
            var strategic = new ClassicStrategicModule();
            strategic.UpdateGoals(World.Current);

            var matching = new StrategicBid(
                new List<Army> { army1 },
                module,
                1.0,
                targetObjective.Kind,
                targetObjective.TargetCityShortName);
            var unrelated = new SimpleBid(new List<Army> { army2 }, module, 5.0);

            strategic.AllocateAssets(new IBid[] { unrelated, matching });

            var accepted = strategic.GetAcceptedBids().First();
            var metadata = accepted as IStrategicBidMetadata;
            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata.TargetCityShortName, Is.EqualTo(targetObjective.TargetCityShortName));
            Assert.That(accepted.Utility, Is.GreaterThan(unrelated.Utility));
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
