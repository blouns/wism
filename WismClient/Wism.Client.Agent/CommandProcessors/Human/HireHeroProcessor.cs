using System;
using Wism.Client.Agent.UI;
using Wism.Client.Api.CommandPublisher;
using Wism.Client.Commands;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Wism.Client.Agent.CommandProcessors.Human;

public class HireHeroProcessor : InstrumentedProcessor
{
    private string heroName;
    private IWismLogger logger;
    private AsciiGame game;

    public HireHeroProcessor(IWismLoggerFactory loggerFactory, CommandIpcPublisher publisher, AsciiGame game)
        :base(publisher)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        this.logger = loggerFactory.CreateLogger();
        this.game = game ?? throw new ArgumentNullException(nameof(game));
    }

    public override bool CanExecute(ICommandAction command)
    {
        return command is HireHeroCommand;
    }

    public override ActionState ExecuteInternal(ICommandAction command)
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
            // Wait for user to name the hero
            heroName = GetHeroName(hireCommand);
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
            if (IsHuman)
            {
                Notify.DisplayAndWait($"And the hero brings {allies.Count} allies!");
            }
            
            game.CommandController.AddCommand(
                new ConscriptArmiesCommand(game.PlayerController,
                    command.Player, command.HeroTile, command.HeroAllies));
        }
    }

    private string GetHeroName(HireHeroCommand command)
    {
        var heroName = command.HeroDisplayName;

        if (IsHuman)
        {
            Notify.Information($"Enter a name [Default: {heroName}]:");
            var newName = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newName))
            {
                heroName = newName;
            }
        }
        return heroName;
    }
}