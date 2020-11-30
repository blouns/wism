using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Agent;
using Wism.Client.Agent.Commands;
using Wism.Client.Agent.Controllers;

namespace Wism.Client.Test.Common
{
    public static class TestUtilities
    {
        public static ILoggerFactory CreateLogFactory()
        {
            var serviceProvider = new ServiceCollection()
                                .AddLogging()
                                .BuildServiceProvider();
            var logFactory = serviceProvider.GetService<ILoggerFactory>();
            return logFactory;
        }

        public static CommandController CreateCommandController(IWismClientRepository repo = null)
        {
            
            if (repo == null)
            {
                var commands = new SortedList<int, Command>();
                repo = new WismClientInMemoryRepository(commands);
            }

            return new CommandController(CreateLogFactory(), repo);
        }

        public static ArmyController CreateArmyController()
        {
            return new ArmyController(CreateLogFactory());
        }

        public static GameController CreateGameController()
        {
            return new GameController(CreateLogFactory());
        }
    }
}
