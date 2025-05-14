using System;
using Wism.Client.Api.Telemetry;
using Wism.Client.Commands;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Wism.Client.Agent.CommandProcessors.Human;

public class StartTurnProcessor : InstrumentedProcessor
{
    private IWismLogger logger;

    public StartTurnProcessor(IWismLoggerFactory loggerFactory, CommandIpcPublisher publisher)
        : base(publisher)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        logger = loggerFactory.CreateLogger();
    }

    public override bool CanExecute(ICommandAction command)
    {
        return command is StartTurnCommand;
    }

    public override ActionState ExecuteInternal(ICommandAction command)
    {
        var startTurnCommand = (StartTurnCommand)command;
        var player = startTurnCommand.Player;
        if (startTurnCommand.Player.GetCities().Count == 0)
        {
            // Player has died
            if (IsHuman)
            {
                Notify.DisplayAndWait($"Wretched {player.Clan.DisplayName}, for you the war is over...");
            }
            else
            {
                Notify.Display($"Wretched {player.Clan.DisplayName}, for you the war is over...");
            }
        }
        else
        {
            // Start the turn
            if (IsHuman)
            {
                Notify.DisplayAndWait($"{player.Clan.DisplayName} your turn is starting...");
            }
            else
            // AI
            {
                Notify.Display($"{player.Clan.DisplayName} your turn is starting...");
            }
        }

        var state = command.Execute();

        return state;
    }
}