using System;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Agent.CommandProcessors;

public class HireHeroProcessor : ICommandProcessor
{
    private readonly AsciiGame asciiGame;
    private string heroName;
    private ILogger logger;

    public HireHeroProcessor(ILoggerFactory loggerFactory, AsciiGame asciiGame)
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
            this.heroName == null)
        {
            // Wait for user to name the hero
            this.heroName = this.GetHeroName(hireCommand);
            state = ActionState.InProgress;
        }
        else if (hireCommand.HeroAccepted)
        {
            // Hire the hero
            state = hireCommand.Execute();
            hireCommand.Hero.DisplayName = this.heroName;

            // Create any allies that will join the hero
            this.CreateAnyAllies(hireCommand);
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
            Notify.DisplayAndWait($"And the hero brings {allies.Count} allies!");
            this.asciiGame.CommandController.AddCommand(
                new ConscriptArmiesCommand(this.asciiGame.PlayerController,
                    command.Player, command.HeroTile, command.HeroAllies));
        }
    }

    private string GetHeroName(HireHeroCommand command)
    {
        var heroName = command.HeroDisplayName;

        Notify.Information($"Enter a name [Default: {heroName}]:");
        var newName = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(newName))
        {
            heroName = newName;
        }

        return heroName;
    }
}