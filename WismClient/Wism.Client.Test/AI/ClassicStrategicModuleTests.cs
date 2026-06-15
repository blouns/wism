using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.AI.Framework;
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
            Assert.That(plan.PersonalityProfile, Is.EqualTo("balanced"));
            Assert.That(Game.Current.StrategicPlans.Single().ClanShortName, Is.EqualTo(player.Clan.ShortName));
        }

        [Test]
        public void Planner_AppliesOptionalPersonalityWeightsDeterministically()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            var player = Game.Current.GetCurrentPlayer();
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), player.Capitol.Tile);

            var balanced = new ClassicStrategicPlanner().Reconcile(World.Current);
            var balancedExpansion = balanced.Objectives.First(objective => objective.Kind == "Expand");

            player.Clan.Info.Personality = new ClanPersonalityInfo
            {
                Profile = "opportunist-test",
                Opportunist = 2.0,
                Explorer = 2.0,
                Aggressive = 1.0,
                Raider = 1.0,
                Defender = 1.0,
                Economy = 1.0
            };

            var opportunist = new ClassicStrategicPlanner().Reconcile(World.Current);
            var opportunistExpansion = opportunist.Objectives.First(objective => objective.Kind == "Expand");

            Assert.That(opportunist.PersonalityProfile, Is.EqualTo("opportunist-test"));
            Assert.That(opportunistExpansion.Priority, Is.GreaterThan(balancedExpansion.Priority));
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
            Assert.That(metadata.Reason, Does.Contain(targetObjective.TargetCityShortName));
            Assert.That(accepted.Utility, Is.GreaterThan(unrelated.Utility));
        }

        [Test]
        public void AiController_RecordsCompactDecisionTraceForAcceptedBid()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            var player = Game.Current.GetCurrentPlayer();
            var army = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), player.Capitol.Tile);
            var tactical = new TraceTacticalModule("Expand", "TestCity");
            var controller = new AiController(
                new SimpleStrategicModule(),
                new List<ITacticalModule> { tactical });

            controller.ExecuteTurnAndReturnCommands(World.Current);

            var trace = controller.LastDecisionTraces.Single();
            Assert.That(trace.ObjectiveKind, Is.EqualTo("Expand"));
            Assert.That(trace.ModuleName, Is.EqualTo(nameof(TraceTacticalModule)));
            Assert.That(trace.Target, Is.EqualTo("city:TestCity"));
            Assert.That(trace.Reason, Does.Contain("TestCity"));
            Assert.That(trace.ArmyIds, Does.Contain(army.Id));
            Assert.That(trace.CommandNames, Is.Empty);
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

        private sealed class TraceTacticalModule : ITacticalModule
        {
            private readonly string objectiveKind;
            private readonly string targetCityShortName;

            public TraceTacticalModule(string objectiveKind, string targetCityShortName)
            {
                this.objectiveKind = objectiveKind;
                this.targetCityShortName = targetCityShortName;
            }

            public IEnumerable<IBid> GenerateBids(World world)
            {
                var army = Game.Current.GetCurrentPlayer().GetArmies().Single();
                return new IBid[]
                {
                    new StrategicBid(
                        new List<Army> { army },
                        this,
                        10.0,
                        objectiveKind,
                        targetCityShortName)
                };
            }

            public IEnumerable<ICommandAction> GenerateCommands(List<Army> armies, World world)
            {
                return Enumerable.Empty<ICommandAction>();
            }
        }
    }
}
