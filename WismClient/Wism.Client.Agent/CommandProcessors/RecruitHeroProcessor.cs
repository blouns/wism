using System;
using Wism.Client.Api.Telemetry;
using Wism.Client.Commands;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Wism.Client.Agent.CommandProcessors;

public class RecruitHeroProcessor : InstrumentedProcessor
{
    private IWismLogger logger;

    public RecruitHeroProcessor(IWismLoggerFactory loggerFactory, CommandIpcPublisher publisher)
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
        return command is RecruitHeroCommand;
    }

    public override ActionState ExecuteInternal(ICommandAction command)
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
                if (IsHuman)
                {
                    // Offer to player
                    state = OfferHeroToPlayer(recruitCommand);
                }
                else
                {
                    // Auto-accept the hero
                    recruitCommand.HeroAccepted = true;
                    state = ActionState.Succeeded;
                }
            }
            else
            {
                // Not enough money
                state = ActionState.Failed;
            }
        }

        return state;
    }

    private ActionState OfferHeroToPlayer(RecruitHeroCommand command)
    {
        ActionState state;

        var player = command.Player;
        var city = command.HeroTile.City;
        var gold = command.HeroPrice;

        Notify.Information($"A hero in {city.DisplayName} offers to join you for {gold} gp!");
        Notify.Information($"You have {player.Gold} gp.");
        Notify.Information("[A]ccept or [r]eject?");
        var key = Console.ReadKey();
        if (key.Key != ConsoleKey.A)
        {
            command.HeroAccepted = false;
            state = ActionState.Failed;
        }
        else
        {
            command.HeroAccepted = true;
            state = ActionState.Succeeded;
        }

        Console.WriteLine();

        return state;
    }
}