using System;
using System.Collections.Generic;
using System.Threading;
using Wism.Client.Agent.UI;
using Wism.Client.Api.CommandPublisher;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Common;
using Wism.Client.Comparers;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.CommandProcessors.Human;

public class PrepareForBattleProcessor : InstrumentedProcessor
{
    private IWismLogger logger;
    private AsciiGame game;

    public PrepareForBattleProcessor(IWismLoggerFactory loggerFactory, CommandIpcPublisher publisher, AsciiGame game)
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
        return command is PrepareForBattleCommand;
    }

    public override ActionState ExecuteInternal(ICommandAction command)
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

        game.GameSpeed = GameBase.DefaultAttackSpeed;

        return command.Execute();
    }

    private static void DrawBattleSetupSequence(Player attacker, Player defender)
    {
        Console.Clear();
        Notify.Information("War... in a senseless mind.");
        Notify.Display($"{attacker.Clan.DisplayName} is attacking {defender.Clan.DisplayName}!");
        for (var i = 0; i < 3; i++)
        {
            Console.Beep();
            Thread.Sleep(750);
        }
    }
}