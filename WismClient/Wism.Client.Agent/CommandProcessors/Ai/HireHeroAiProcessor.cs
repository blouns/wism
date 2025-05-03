using System;
using Wism.Client.Agent.UI;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Wism.Client.Agent.CommandProcessors.Ai;

public class HireHeroAiProcessor : ICommandProcessor
{
    private readonly AsciiGame asciiGame;
    private string heroName;
    private IWismLogger logger;

    public HireHeroAiProcessor(IWismLoggerFactory loggerFactory, AsciiGame asciiGame)
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
        return command is HireHeroCommand;
    }

    public ActionState Execute(ICommandAction command)
    {
        ActionState state;

        var hireCommand = (HireHeroCommand)command;

        if (hireCommand.RecruitHeroCommand.Result != ActionState.Succeeded)
        {
            return ActionState.Failed;
        }

        if (hireCommand.HeroAccepted &&
            heroName == null)
        {
            heroName = hireCommand.HeroDisplayName;
            state = ActionState.InProgress;
        }
        else if (hireCommand.HeroAccepted)
        {
            // Hire the hero
            state = hireCommand.Execute();
            hireCommand.Hero.DisplayName = heroName;

            // Create any allies that will join the hero
            CreateAnyAllies(hireCommand);
        }
        else
        {
            // Hero not accepted
            state = ActionState.Failed;
        }

        return state;
    }

    private void CreateAnyAllies(HireHeroCommand command)
    {
        // Check for any allies the hero brought with them
        var allies = command.HeroAllies;
        if (allies != null && allies.Count > 0)
        {
            Notify.Display($"And the hero brings {allies.Count} allies!");
            asciiGame.CommandController.AddCommand(
                new ConscriptArmiesCommand(asciiGame.PlayerController,
                    command.Player, command.HeroTile, command.HeroAllies));
        }
    }
}