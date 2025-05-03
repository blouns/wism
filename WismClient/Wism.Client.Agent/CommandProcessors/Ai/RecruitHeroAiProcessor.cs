using System;
using Wism.Client.Agent.UI;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Wism.Client.Agent.CommandProcessors.Ai;

public class RecruitHeroAiProcessor : ICommandProcessor
{
    private readonly AsciiGame asciiGame;
    private IWismLogger logger;

    public RecruitHeroAiProcessor(IWismLoggerFactory loggerFactory, AsciiGame asciiGame)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        logger = loggerFactory.CreateLogger();
        this.asciiGame = asciiGame ?? throw new ArgumentNullException(nameof(asciiGame));
    }

    public bool CanExecute(ICommandAction command)
    {
        return command is RecruitHeroCommand;
    }

    public ActionState Execute(ICommandAction command)
    {
        var state = ActionState.Failed;

        var recruitCommand = (RecruitHeroCommand)command;
        var player = recruitCommand.Player;
        if (player.IsDead)
        {
            return ActionState.Failed;
        }

        if (recruitCommand.Result == ActionState.NotStarted)
        {
            // Find's a hero if one is available
            state = command.Execute();
        }

        if (state == ActionState.Succeeded)
        {
            // Here is available; offer to player if enough money
            if (player.Gold >= recruitCommand.HeroPrice)
            {
                // Auto-accept the hero
                recruitCommand.HeroAccepted = true;
                state = ActionState.Succeeded;
            }
            else
            {
                // Not enough money
                state = ActionState.Failed;
            }
        }

        return state;
    }
}