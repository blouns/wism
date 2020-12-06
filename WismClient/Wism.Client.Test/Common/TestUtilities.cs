using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    }
}
