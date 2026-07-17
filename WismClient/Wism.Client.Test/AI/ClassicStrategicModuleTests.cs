using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.AI.Framework;
using Wism.Client.AI.InfluenceMaps;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;
using Wism.Client.Pathing;
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
        public void Planner_PopulatesStableGoalLifecycleMetadata()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            var player = Game.Current.GetCurrentPlayer();
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), player.Capitol.Tile);

            var first = new ClassicStrategicPlanner().Reconcile(World.Current);
            var firstExpansion = first.Objectives.First(objective => objective.Kind == "Expand");
            var second = new ClassicStrategicPlanner().Reconcile(World.Current);
            var secondExpansion = second.Objectives.First(objective => objective.Id == firstExpansion.Id);

            Assert.That(firstExpansion.GoalId, Is.EqualTo(firstExpansion.Id));
            Assert.That(firstExpansion.CreatedTurn, Is.EqualTo(player.Turn));
            Assert.That(firstExpansion.UpdatedTurn, Is.EqualTo(player.Turn));
            Assert.That(firstExpansion.State, Is.EqualTo("Assigned"));
            Assert.That(firstExpansion.Reason, Does.Contain(firstExpansion.TargetCityShortName));
            Assert.That(secondExpansion.GoalId, Is.EqualTo(firstExpansion.GoalId));
            Assert.That(secondExpansion.CreatedTurn, Is.EqualTo(firstExpansion.CreatedTurn));
        }

        [Test]
        public void Planner_MarksSameOwnerSiegeObjectiveSatisfiedWhenTargetWasCaptured()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            var player = Game.Current.GetCurrentPlayer();
            var targetCity = World.Current.Map[7, 4].City;
            var army = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), player.Capitol.Tile);
            Game.Current.StrategicPlans = new[]
            {
                new StrategicPlanEntity
                {
                    SchemaVersion = 1,
                    ClanShortName = player.Clan.ShortName,
                    Turn = player.Turn,
                    Revision = 1,
                    Posture = "BalancedPressure",
                    PersonalityProfile = "balanced",
                    Objectives = new[]
                    {
                        new StrategicObjectiveEntity
                        {
                            Id = $"Siege:{targetCity.ShortName}",
                            GoalId = $"Siege:{targetCity.ShortName}",
                            Kind = "Siege",
                            TargetCityShortName = targetCity.ShortName,
                            AssignedArmyIds = new[] { army.Id },
                            AssignedCityShortNames = new string[0],
                            Priority = 50,
                            Status = "Active",
                            State = "Assigned",
                            CreatedTurn = player.Turn,
                            UpdatedTurn = player.Turn,
                            Reason = $"Siege goal for {targetCity.ShortName}."
                        }
                    }
                }
            };
            controllerProvider.CityController.ClaimCity(targetCity, player);

            var plan = new ClassicStrategicPlanner().Reconcile(World.Current);

            var satisfied = plan.Objectives.Single(objective =>
                objective.Kind == "Siege" &&
                objective.TargetCityShortName == targetCity.ShortName);
            Assert.That(satisfied.Status, Is.EqualTo("Stale"));
            Assert.That(satisfied.State, Is.EqualTo("Satisfied"));
            Assert.That(satisfied.BlockingReason, Is.EqualTo("target-city-already-owned"));
            Assert.That(satisfied.Priority, Is.EqualTo(5));
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
        public void Planner_UsesInfluenceTensionForDefensiveRecovery()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            var player = Game.Current.GetCurrentPlayer();
            player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), player.Capitol.Tile);
            var advisor = new ScriptedSpatialAdvisor();
            advisor.Set(player.Capitol.Tile, enemy: 0.50, tension: -0.25);

            var plan = new ClassicStrategicPlanner(advisor).Reconcile(World.Current);

            Assert.That(plan.Posture, Is.EqualTo("DefensiveRecovery"));
            var defense = plan.Objectives.FirstOrDefault(objective => objective.Kind == "Defend");
            Assert.That(defense, Is.Not.Null);
            Assert.That(defense.TargetCityShortName, Is.EqualTo(player.Capitol.ShortName));
        }

        [Test]
        public void Planner_AssignsArmiesByRouteDistanceBeforeManhattanDistance()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            var player = Game.Current.GetCurrentPlayer();
            var manhattanNear = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[4, 4]);
            var routeNear = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[1, 1]);
            var pathing = new ScriptedPathingStrategy();
            pathing.SetDistance(manhattanNear.Tile, 30);
            pathing.SetDistance(routeNear.Tile, 3);

            var plan = new ClassicStrategicPlanner(null, pathing).Reconcile(World.Current);

            var expansion = plan.Objectives.First(objective => objective.Kind == "Expand");
            Assert.That(expansion.AssignedArmyIds.First(), Is.EqualTo(routeNear.Id));
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
            Assert.That(loaded.Objectives.Select(objective => objective.GoalId), Is.EquivalentTo(original.Objectives.Select(objective => objective.GoalId)));
            Assert.That(loaded.Objectives.All(objective => !string.IsNullOrWhiteSpace(objective.State)), Is.True);
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

        [Test]
        public void AiController_SuppressesRepeatedIdenticalCommandBatchWithinTurn()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            var player = Game.Current.GetCurrentPlayer();
            var army = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[4, 4]);
            var target = World.Current.Map[5, 4];
            var tactical = new RepeatingMoveTacticalModule(controllerProvider.ArmyController, army, target);
            var controller = new AiController(
                new SimpleStrategicModule(),
                new List<ITacticalModule> { tactical });

            var first = controller.ExecuteTurnAndReturnCommands(World.Current);
            var second = controller.ExecuteTurnAndReturnCommands(World.Current);

            Assert.That(
                first.OfType<MoveOnceCommand>().Any(command => command.X == target.X && command.Y == target.Y),
                Is.True);
            Assert.That(second, Is.Empty);
            Assert.That(controller.LastDecisionTraces.Single().Outcome, Is.EqualTo("blocked"));
            Assert.That(controller.LastDecisionTraces.Single().BlockingReason, Is.EqualTo("repeated-command-batch"));
        }

        [Test]
        public void AiController_FallsBackWhenWinningBidProducesNoCommands()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            var player = Game.Current.GetCurrentPlayer();
            var army = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[4, 4]);
            var target = World.Current.Map[5, 4];
            var blockedWinner = new NoCommandBidTacticalModule(army);
            var fallback = new FallbackMoveTacticalModule(controllerProvider.ArmyController, army, target);
            var controller = new AiController(
                new SimpleStrategicModule(),
                new List<ITacticalModule> { blockedWinner, fallback });

            var commands = controller.ExecuteTurnAndReturnCommands(World.Current);

            Assert.That(
                commands.OfType<MoveOnceCommand>().Any(command => command.X == target.X && command.Y == target.Y),
                Is.True);
            Assert.That(controller.LastDecisionTraces.Select(trace => trace.ModuleName), Does.Contain(nameof(NoCommandBidTacticalModule)));
            Assert.That(controller.LastDecisionTraces.Select(trace => trace.ModuleName), Does.Contain(nameof(FallbackMoveTacticalModule)));
            Assert.That(controller.LastDecisionTraces.First(trace => trace.ModuleName == nameof(NoCommandBidTacticalModule)).Outcome, Is.EqualTo("blocked"));
            Assert.That(controller.LastDecisionTraces.First(trace => trace.ModuleName == nameof(NoCommandBidTacticalModule)).BlockingReason, Is.EqualTo("no-executable-command"));
            Assert.That(controller.LastDecisionTraces.First(trace => trace.ModuleName == nameof(FallbackMoveTacticalModule)).Outcome, Is.EqualTo("executed"));
        }

        [Test]
        public void AiController_PrioritizesExecutableFallbackAfterBidBlocks()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            TestUtilities.NewGame(controllerProvider, TestUtilities.DefaultTestWorld);
            var player = Game.Current.GetCurrentPlayer();
            var army = player.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), World.Current.Map[4, 4]);
            var target = World.Current.Map[5, 4];
            var blockedWinner = new NoCommandBidTacticalModule(army, army.Tile.X, army.Tile.Y);
            var fallback = new FallbackMoveTacticalModule(controllerProvider.ArmyController, army, target);
            var controller = new AiController(
                new SimpleStrategicModule(),
                new List<ITacticalModule> { blockedWinner, fallback });

            ExecuteCommandsUntilDone(controller.ExecuteTurnAndReturnCommands(World.Current));
            var second = controller.ExecuteTurnAndReturnCommands(World.Current);

            Assert.That(
                second.OfType<MoveOnceCommand>().Any(command => command.X == target.X && command.Y == target.Y),
                Is.True);
            Assert.That(controller.LastDecisionTraces.Select(trace => trace.ModuleName), Is.EquivalentTo(new[] { nameof(FallbackMoveTacticalModule) }));
            Assert.That(controller.LastDecisionTraces.Single().Outcome, Is.EqualTo("executed"));
        }

        private static void ExecuteCommandsUntilDone(IEnumerable<ICommandAction> commands)
        {
            foreach (var command in commands)
            {
                var result = command.Execute();
                while (result == ActionState.InProgress)
                {
                    result = command.Execute();
                }
            }
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

        private sealed class RepeatingMoveTacticalModule : ITacticalModule
        {
            private readonly ArmyController armyController;
            private readonly Army army;
            private readonly Tile target;

            public RepeatingMoveTacticalModule(ArmyController armyController, Army army, Tile target)
            {
                this.armyController = armyController;
                this.army = army;
                this.target = target;
            }

            public IEnumerable<IBid> GenerateBids(World world)
            {
                return new IBid[]
                {
                    new StrategicBid(
                        new List<Army> { army },
                        this,
                        10.0,
                        "Expand",
                        targetX: target.X,
                        targetY: target.Y,
                        reason: "Repeatable move for loop detector.")
                };
            }

            public IEnumerable<ICommandAction> GenerateCommands(List<Army> armies, World world)
            {
                return new ICommandAction[]
                {
                    new SelectArmyCommand(armyController, armies),
                    new MoveOnceCommand(armyController, armies, target.X, target.Y),
                    new DeselectArmyCommand(armyController, armies)
                };
            }
        }

        private sealed class NoCommandBidTacticalModule : ITacticalModule
        {
            private readonly Army army;
            private readonly int? targetX;
            private readonly int? targetY;

            public NoCommandBidTacticalModule(Army army, int? targetX = null, int? targetY = null)
            {
                this.army = army;
                this.targetX = targetX;
                this.targetY = targetY;
            }

            public IEnumerable<IBid> GenerateBids(World world)
            {
                return new IBid[]
                {
                    new StrategicBid(
                        new List<Army> { army },
                        this,
                        100.0,
                        "Search",
                        targetX: targetX ?? army.Tile.X,
                        targetY: targetY ?? army.Tile.Y,
                        reason: "Impossible high-priority objective.")
                };
            }

            public IEnumerable<ICommandAction> GenerateCommands(List<Army> armies, World world)
            {
                return Enumerable.Empty<ICommandAction>();
            }
        }

        private sealed class FallbackMoveTacticalModule : ITacticalModule
        {
            private readonly ArmyController armyController;
            private readonly Army army;
            private readonly Tile target;

            public FallbackMoveTacticalModule(ArmyController armyController, Army army, Tile target)
            {
                this.armyController = armyController;
                this.army = army;
                this.target = target;
            }

            public IEnumerable<IBid> GenerateBids(World world)
            {
                return new IBid[]
                {
                    new StrategicBid(
                        new List<Army> { army },
                        this,
                        10.0,
                        "Fallback",
                        targetX: target.X,
                        targetY: target.Y,
                        reason: "Lower-priority objective remains executable.")
                };
            }

            public IEnumerable<ICommandAction> GenerateCommands(List<Army> armies, World world)
            {
                return new ICommandAction[]
                {
                    new SelectArmyCommand(armyController, armies),
                    new MoveOnceCommand(armyController, armies, target.X, target.Y),
                    new DeselectArmyCommand(armyController, armies)
                };
            }
        }

        private sealed class ScriptedSpatialAdvisor : ISpatialAdvisor
        {
            private readonly Dictionary<(int X, int Y), (double Friendly, double Enemy, double Tension)> values =
                new Dictionary<(int X, int Y), (double Friendly, double Enemy, double Tension)>();

            public void Set(Tile tile, double friendly = 0.0, double enemy = 0.0, double tension = 0.0)
            {
                values[(tile.X, tile.Y)] = (friendly, enemy, tension);
            }

            public void Update()
            {
            }

            public double GetInfluence(Tile tile) => GetFriendly(tile);

            public double GetFriendly(Tile tile) => tile == null ? 0.0 : GetFriendly(tile.X, tile.Y);

            public double GetEnemy(Tile tile) => tile == null ? 0.0 : GetEnemy(tile.X, tile.Y);

            public double GetTension(Tile tile) => tile == null ? 0.0 : GetTension(tile.X, tile.Y);

            public double GetRawFriendly(Tile tile) => GetFriendly(tile);

            public double GetRawEnemy(Tile tile) => GetEnemy(tile);

            public bool IsFrontLine(Tile tile) => tile != null && GetEnemy(tile) > 0.0 && System.Math.Abs(GetTension(tile)) < 0.05;

            public Tile GetGradientStep(Tile from, bool ascendFriendly) => from;

            public double GetFriendly(int x, int y) => values.TryGetValue((x, y), out var value) ? value.Friendly : 0.0;

            public double GetEnemy(int x, int y) => values.TryGetValue((x, y), out var value) ? value.Enemy : 0.0;

            public double GetTension(int x, int y) => values.TryGetValue((x, y), out var value) ? value.Tension : 0.0;
        }

        private sealed class ScriptedPathingStrategy : IPathingStrategy
        {
            private readonly Dictionary<(int X, int Y), float> distances = new Dictionary<(int X, int Y), float>();

            public void SetDistance(Tile start, float distance)
            {
                distances[(start.X, start.Y)] = distance;
            }

            public void FindShortestRoute(
                Tile[,] map,
                List<Army> armies,
                Tile target,
                out IList<Tile> fastestRoute,
                out float distance,
                bool ignoreClan = false)
            {
                var start = armies.First().Tile;
                distance = distances.TryGetValue((start.X, start.Y), out var scripted)
                    ? scripted
                    : 999;
                fastestRoute = new List<Tile> { start, target };
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
