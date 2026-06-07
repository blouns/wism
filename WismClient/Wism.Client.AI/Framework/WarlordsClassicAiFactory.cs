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
            IPathingStrategy pathingStrategy = null)
        {
            pathingStrategy = pathingStrategy ?? new AStarPathingStrategy();
            var pathfinder = new PathfindingService(pathingStrategy);
            var garrisonPolicy = new GarrisonPolicy();

            return new AiController(
                new SimpleStrategicModule(),
                new List<ITacticalModule>
                {
                    new CityDefenseModule(controllerProvider.ArmyController, logger),
                    new CaptureModule(controllerProvider.ArmyController, controllerProvider.CityController, garrisonPolicy, logger),
                    new ExterminationModule(pathfinder, pathingStrategy, controllerProvider.ArmyController, new CombatEstimator(), garrisonPolicy, logger),
                    new SearchModule(controllerProvider.ArmyController, controllerProvider.LocationController, pathingStrategy, garrisonPolicy, logger),
                    new RallyModule(controllerProvider.ArmyController, pathingStrategy, garrisonPolicy, logger)
                },
                new List<ITurnModule>
                {
                    new ProductionModule(controllerProvider.CityController, logger)
                });
        }

        public static AdaptaCommandProvider CreateCommandProvider(
            ControllerProvider controllerProvider,
            IWismLogger logger,
            IPathingStrategy pathingStrategy = null)
        {
            return new AdaptaCommandProvider(
                logger,
                CreateController(controllerProvider, logger, pathingStrategy),
                controllerProvider);
        }
    }
}
