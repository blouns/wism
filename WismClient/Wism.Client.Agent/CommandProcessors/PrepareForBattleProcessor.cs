using System;
using System.Collections.Generic;
using System.Threading;
using Wism.Client.Agent.UI;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Common;
using Wism.Client.Comparers;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.CommandProcessors;

public class PrepareForBattleProcessor : ICommandProcessor
{
    private readonly AsciiGame asciiGame;
    private IWismLogger logger;

    public PrepareForBattleProcessor(IWismLoggerFactory loggerFactory, AsciiGame asciiGame)
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
        return command is PrepareForBattleCommand;
    }

    public ActionState Execute(ICommandAction command)
    {
        var battleCommand = (PrepareForBattleCommand)command;
        var targetTile = World.Current.Map[battleCommand.X, battleCommand.Y];
        var attackingPlayer = battleCommand.Armies[0].Player;
        var attackingArmies = new List<Army>(battleCommand.Armies);
        attackingArmies.Sort(new ByArmyBattleOrder(targetTile));

        Player defendingPlayer;
        List<Army> defendingArmies;
        if (battleCommand.Defenders != null && battleCommand.Defenders.Count > 0)
        {
            defendingPlayer = battleCommand.Defenders[0].Player;
            defendingArmies = targetTile.MusterArmy();
            defendingArmies.Sort(new ByArmyBattleOrder(targetTile));
        }
        else
        {
            // Attacking an empty city
            defendingPlayer = targetTile.City.Player;
            defendingArmies = new List<Army>();
        }

        DrawBattleSetupSequence(attackingPlayer, defendingPlayer);
        BattleProcessor.DrawBattleUpdate(attackingPlayer.Clan, attackingArmies, defendingPlayer.Clan, defendingArmies);

        this.asciiGame.GameSpeed = GameBase.DefaultAttackSpeed;

        return command.Execute();
    }

    private static void DrawBattleSetupSequence(Player attacker, Player defender)
    {
        Console.Clear();
        Notify.Information("War! ...in a senseless mind.");
        Notify.Display($"{attacker.Clan.DisplayName} is attacking {defender.Clan.DisplayName}!");
        for (var i = 0; i < 3; i++)
        {
            Console.Beep();
            Thread.Sleep(750);
        }
    }
}