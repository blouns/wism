using System;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Agent.CommandProcessors
{
    public class StartTurnProcessor : ICommandProcessor
    {
        private ILogger logger;
        private readonly AsciiGame asciiGame;

        public StartTurnProcessor(ILoggerFactory loggerFactory, AsciiGame asciiGame)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.asciiGame = asciiGame ?? throw new ArgumentNullException(nameof(asciiGame));
        }

        public bool CanExecute(ICommandAction command)
        {
            return command is StartTurnCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var startTurnCommand = (StartTurnCommand)command;
            var player = startTurnCommand.Player;
            if (startTurnCommand.Player.GetCities().Count == 0)
            {
                // Player has died                
                Notify.DisplayAndWait($"Wretched {player.Clan.DisplayName}, for you the war is over...");
            }
            else
            {
                // Start the turn
                Notify.DisplayAndWait($"{player.Clan.DisplayName} your turn is starting...");
            }

            var state = command.Execute();

            return state;
        }
    }
}
