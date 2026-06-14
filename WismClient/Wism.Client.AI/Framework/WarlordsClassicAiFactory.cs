using System.Collections.Generic;
using Wism.Client.AI.CommandProviders;
using Wism.Client.AI.Services;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Pathing;

namespace Wism.Client.AI.Framework
{
    public static class WarlordsClassicAiFactory
    {
        public static AiController CreateController(
            ControllerProvider controllerProvider,
            IWismLogger logger,
            IPathingStrategy pathingStrategy = null,
            string aiProfile = "tactical")
        {
            pathingStrategy = pathingStrategy ?? new AStarPathingStrategy();
            var pathfinder = new PathfindingService(pathingStrategy);
            var garrisonPolicy = new GarrisonPolicy();
            var allowTempleSearch = !UsesClassicStrategicProfile(aiProfile);

            return new AiController(
                CreateStrategicModule(aiProfile),
                new List<ITacticalModule>
                {
                    new CityDefenseModule(controllerProvider.ArmyController, logger),
                    new CaptureModule(controllerProvider.ArmyController, controllerProvider.CityController, garrisonPolicy, logger),
                    new ExterminationModule(pathfinder, pathingStrategy, controllerProvider.ArmyController, new CombatEstimator(), garrisonPolicy, logger),
                    new SearchModule(controllerProvider.ArmyController, controllerProvider.LocationController, pathingStrategy, garrisonPolicy, logger, allowTempleSearch),
                    new RallyModule(controllerProvider.ArmyController, pathingStrategy, garrisonPolicy, logger)
                },
                new List<ITurnModule>
                {
                    new ProductionModule(controllerProvider.CityController, logger)
                },
                logger);
        }

        public static AdaptaCommandProvider CreateCommandProvider(
            ControllerProvider controllerProvider,
            IWismLogger logger,
            IPathingStrategy pathingStrategy = null,
            string aiProfile = "tactical")
        {
            return new AdaptaCommandProvider(
                logger,
                CreateController(controllerProvider, logger, pathingStrategy, aiProfile),
                controllerProvider);
        }

        private static IStrategicModule CreateStrategicModule(string aiProfile)
        {
            return !UsesClassicStrategicProfile(aiProfile)
                ? (IStrategicModule)new SimpleStrategicModule()
                : new ClassicStrategicModule();
        }

        private static bool UsesClassicStrategicProfile(string aiProfile)
        {
            return !string.Equals(aiProfile, "tactical", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
