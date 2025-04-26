using System;
using Wism.Client.Agent.UI;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Wism.Client.Agent.CommandProcessors;

public class StartTurnProcessor : ICommandProcessor
{
    private readonly AsciiGame asciiGame;
    private ILogger logger;

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