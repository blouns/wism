using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Agent;
using Wism.Client.Agent.Commands;
using Wism.Client.Agent.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

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

        public static ControllerProvider CreateControllerProvider()
        {
            return new ControllerProvider()
            {
                ArmyController = CreateArmyController(),
                CommandController = CreateCommandController(),
                GameController = CreateGameController()
            };
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

        public static CityController CreateCityController()
        {
            return new CityController(CreateLogFactory());
        }

        public static ActionState Select(CommandController commandController, ArmyController armyController, List<Army> armies)
        {
            return ExecuteCommandUntilDone(commandController,
                new SelectArmyCommand(armyController, armies));
        }

        public static ActionState Deselect(CommandController commandController, ArmyController armyController, List<Army> armies)
        {
            return ExecuteCommandUntilDone(commandController,
                new DeselectArmyCommand(armyController, armies));
        }

        public static ActionState AttackUntilDone(CommandController commandController, ArmyController armyController, List<Army> armies, int x, int y)
        {
            return ExecuteCommandUntilDone(commandController,
                new AttackCommand(armyController, armies, x, y));
        }

        public static ActionState MoveUntilDone(CommandController commandController, ArmyController armyController, List<Army> armies, int x, int y)
        {
            return ExecuteCommandUntilDone(commandController,
                new MoveAlongPathCommand(armyController, armies, x, y));
        }

        public static ActionState EndTurn(CommandController commandController, GameController gameController)
        {
            return ExecuteCommandUntilDone(commandController,
                new EndTurnCommand(gameController, Game.Current.GetCurrentPlayer()));
        }

        public static ActionState StartTurn(CommandController commandController, GameController gameController)
        {
            return ExecuteCommandUntilDone(commandController,
                new StartTurnCommand(gameController, Game.Current.GetCurrentPlayer()));
        }

        public static ActionState ExecuteCommandUntilDone(CommandController commandController, Command command)
        {
            // Simulate two-phase execution            
            commandController.AddCommand(command);

            var commandToExecute = commandController.GetCommand(command.Id);
            var result = commandToExecute.Execute();
            while (result == ActionState.InProgress)
            {
                result = commandToExecute.Execute();
            }

            return result;
        }

        public static void PlotRouteOnMap(Tile[,] map, List<Tile> path)
        {
            for (int y = 0; y <= map.GetUpperBound(0); y++)
            {
                for (int x = 0; x <= map.GetUpperBound(1); x++)
                {
                    var tile = path.Find(t => ((t.X == x) && (t.Y == y)));
                    if (tile != null)
                    {
                        TestContext.Write($"({x},{y}){{{map[x, y]}}}>\t");
                    }
                    else
                    {
                        TestContext.Write($"({x},{y})[{map[x, y]}]\t");
                    }
                }
                TestContext.WriteLine();
            }
        }
    }
}
