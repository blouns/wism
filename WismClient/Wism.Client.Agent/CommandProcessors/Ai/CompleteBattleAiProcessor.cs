﻿using System;
using System.Collections.Generic;
using Wism.Client.Agent.UI;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Common;
using Wism.Client.Comparers;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.CommandProcessors.Ai;

public class CompleteBattleAiProcessor : ICommandProcessor
{
    private readonly AsciiGame asciiGame;
    private IWismLogger logger;

    public CompleteBattleAiProcessor(IWismLoggerFactory loggerFactory, AsciiGame asciiGame)
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
        return command is CompleteBattleCommand;
    }

    public ActionState Execute(ICommandAction command)
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
            Notify.Display($"{name} {presentVerb} victorious!");
        }
        else if (battleResult == ActionState.Failed)
        {
            Notify.Display($"{name} {pastVerb} been defeated!");
        }
        else
        {
            Notify.Alert("Error: Unexpected game state" + battleResult);
        }

        asciiGame.GameSpeed = GameBase.DefaultGameSpeed;

        return command.Execute();
    }
}