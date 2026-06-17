using System.Collections.Generic;
using NUnit.Framework;
using Wism.Client.AI.Framework;
using Wism.Client.AI.InfluenceMaps;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.AI
{
    /// <summary>
    ///     Workstream A2: the controller refreshes the shared spatial advisor exactly once per AI
    ///     turn (no per-bid recompute) and exposes that single cached instance to consumers.
    /// </summary>
    [TestFixture]
    public class AiControllerSpatialAdvisorTests
    {
        [Test]
        public void ExecuteTurn_FloodsTheSpatialAdvisor_ExactlyOnce_WhenNoBids()
        {
            var advisor = new CountingSpatialAdvisor();
            var controller = new AiController(
                new NoOpStrategicModule(),
                new List<ITacticalModule>(),
                new List<ITurnModule>(),
                logger: null,
                spatialAdvisor: advisor);

            controller.ExecuteTurnAndReturnCommands(null);

            Assert.That(advisor.UpdateCount, Is.EqualTo(1), "the field is flooded once at turn start");
        }

        [Test]
        public void ExecuteTurn_FloodsTheSpatialAdvisor_OncePerTurn_NotPerBid()
        {
            var advisor = new CountingSpatialAdvisor();
            var moduleA = new StubTacticalModule();
            var moduleB = new StubTacticalModule();
            var controller = new AiController(
                new NoOpStrategicModule(),
                new List<ITacticalModule> { moduleA, moduleB },
                new List<ITurnModule>(),
                logger: null,
                spatialAdvisor: advisor);

            controller.ExecuteTurnAndReturnCommands(null);

            Assert.That(advisor.UpdateCount, Is.EqualTo(1), "one flood per turn, regardless of bid count");
            Assert.That(
                moduleA.CommandGenerationCount + moduleB.CommandGenerationCount,
                Is.EqualTo(2),
                "both bids were processed, yet the field was not recomputed per bid");
        }

        [Test]
        public void Controller_ExposesTheInjectedAdvisor_AsASingleCachedInstance()
        {
            var advisor = new CountingSpatialAdvisor();
            var controller = new AiController(
                new NoOpStrategicModule(),
                new List<ITacticalModule>(),
                new List<ITurnModule>(),
                logger: null,
                spatialAdvisor: advisor);

            Assert.That(controller.SpatialAdvisor, Is.SameAs(advisor));
        }

        [Test]
        public void Controller_DefaultsToAForwardFeedInfluenceMap_WhenNoneInjected()
        {
            var controller = new AiController(new NoOpStrategicModule(), new List<ITacticalModule>());

            Assert.That(controller.SpatialAdvisor, Is.Not.Null);
            Assert.That(controller.SpatialAdvisor, Is.TypeOf<ForwardFeedInfluenceMap>());
        }

        [Test]
        public void Factory_WiresAForwardFeedInfluenceMapIntoTheController()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            var controller = WarlordsClassicAiFactory.CreateController(controllerProvider, logger);

            Assert.That(controller.SpatialAdvisor, Is.TypeOf<ForwardFeedInfluenceMap>());
        }

        [Test]
        public void Factory_WiresNoOpSpatialAdvisor_ForStrategicProfile()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            var controller = WarlordsClassicAiFactory.CreateController(controllerProvider, logger, aiProfile: "strategic");

            Assert.That(controller.SpatialAdvisor, Is.TypeOf<NoOpSpatialAdvisor>());
        }

        [Test]
        public void Factory_WiresNoOpSpatialAdvisor_ForStrategicBaselineProfile()
        {
            var controllerProvider = TestUtilities.CreateControllerProvider();
            var logger = TestUtilities.CreateLogFactory().CreateLogger();

            var controller = WarlordsClassicAiFactory.CreateController(controllerProvider, logger, aiProfile: "strategic-baseline");

            Assert.That(controller.SpatialAdvisor, Is.TypeOf<NoOpSpatialAdvisor>());
        }

        private sealed class CountingSpatialAdvisor : ISpatialAdvisor
        {
            public int UpdateCount { get; private set; }

            public void Update() => this.UpdateCount++;

            public double GetInfluence(Tile tile) => 0.0;

            public double GetFriendly(Tile tile) => 0.0;

            public double GetEnemy(Tile tile) => 0.0;

            public double GetTension(Tile tile) => 0.0;

            public double GetRawFriendly(Tile tile) => 0.0;

            public double GetRawEnemy(Tile tile) => 0.0;

            public bool IsFrontLine(Tile tile) => false;

            public Tile GetGradientStep(Tile from, bool ascendFriendly) => from;

            public double GetFriendly(int x, int y) => 0.0;

            public double GetEnemy(int x, int y) => 0.0;

            public double GetTension(int x, int y) => 0.0;
        }

        private sealed class NoOpStrategicModule : IStrategicModule
        {
            public void UpdateGoals(World world)
            {
            }

            public void AllocateAssets(IEnumerable<IBid> bids)
            {
            }
        }

        private sealed class StubTacticalModule : ITacticalModule
        {
            public int CommandGenerationCount { get; private set; }

            public IEnumerable<IBid> GenerateBids(World world)
            {
                return new IBid[] { new StubBid(this) };
            }

            public IEnumerable<ICommandAction> GenerateCommands(List<Army> armies, World world)
            {
                this.CommandGenerationCount++;
                return new List<ICommandAction>();
            }
        }

        private sealed class StubBid : IBid
        {
            public StubBid(ITacticalModule module)
            {
                this.Module = module;
            }

            public List<Army> Armies { get; } = new List<Army>();

            public double Utility => 1.0;

            public ITacticalModule Module { get; }
        }
    }
}
