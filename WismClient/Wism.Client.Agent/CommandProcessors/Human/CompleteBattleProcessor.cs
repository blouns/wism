using System;
using System.Collections.Generic;
using Wism.Client.Api.CommandPublisher;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Common;
using Wism.Client.Comparers;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.CommandProcessors.Human;

public class CompleteBattleProcessor : InstrumentedProcessor
{
    private IWismLogger logger;

    public CompleteBattleProcessor(IWismLoggerFactory loggerFactory, CommandIpcPublisher publisher)
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
        return command is CompleteBattleCommand;
    }

    public override ActionState ExecuteInternal(ICommandAction command)
    {
        var battleCompleteCommand = (CompleteBattleCommand)command;
        var targetTile = World.Current.Map[battleCompleteCommand.X, battleCompleteCommand.Y];
        var attackingPlayer = battleCompleteCommand.Player;
        var attackingArmies = new List<Army>(battleCompleteCommand.Armies);
        attackingArmies.Sort(new ByArmyBattleOrder(targetTile));

        var defendingArmies = targetTile.MusterArmy();
        defendingArmies.Sort(new ByArmyBattleOrder(targetTile));

        var name = attackingPlayer.Clan.DisplayName;
        var presentVerb = name.EndsWith('s') ? "are" : "is";
        var pastVerb = name.EndsWith('s') ? "have" : "has";

        // Check battle result
        var battleResult = battleCompleteCommand.AttackCommand.Result;
        if (battleResult == ActionState.Succeeded)
        {
            if (IsHuman)
            {
                Notify.DisplayAndWait($"{name} {presentVerb} victorious!");
            }
            else
            {
                Notify.Display($"{name} {pastVerb} victorious!");
            }
        }
        else if (battleResult == ActionState.Failed)
        {
            if (IsHuman)
            {
                Notify.DisplayAndWait($"{name} {pastVerb} been defeated!");
            }
            else
            {
                Notify.Display($"{name} {pastVerb} been defeated!");
            }
        }
        else
        {
            Notify.Alert("Error: Unexpected game state" + battleResult);
        }        

        return command.Execute();
    }
}