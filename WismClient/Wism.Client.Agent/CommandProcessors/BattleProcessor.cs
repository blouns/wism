using System;
using System.Collections.Generic;
using Wism.Client.Agent.UI;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.CommandProcessors;

public class BattleProcessor : ICommandProcessor
{
    private ILogger logger;

    public BattleProcessor(ILoggerFactory loggerFactory)
    {
        this.logger = loggerFactory.CreateLogger();
    }

    public bool CanExecute(ICommandAction command)
    {
        return command is AttackOnceCommand;
    }

    public ActionState Execute(ICommandAction command)
    {
        var battleCommand = (AttackOnceCommand)command;
        var result = battleCommand.Execute();

        var targetTile = World.Current.Map[battleCommand.X, battleCommand.Y];
        var attackingPlayer = battleCommand.OriginalAttackingArmies[0].Player;
        var attackingArmies = battleCommand.OriginalAttackingArmies;
        attackingArmies.Sort(new ByArmyBattleOrder(targetTile));

        Player defendingPlayer;
        List<Army> defendingArmies;
        if (battleCommand.OriginalDefendingArmies == null ||
            battleCommand.OriginalDefendingArmies.Count == 0)
        {
            // Attacking empty city
            Notify.DisplayAndWait("Press any key to continue...");
            return ActionState.Succeeded;
        }

        defendingPlayer = battleCommand.OriginalDefendingArmies[0].Player;
        defendingArmies = battleCommand.OriginalDefendingArmies;
        defendingArmies.Sort(new ByArmyBattleOrder(targetTile));

        DrawBattleUpdate(attackingPlayer.Clan, attackingArmies, defendingPlayer.Clan, defendingArmies);

        return result;
    }

    public static void DrawBattleUpdate(Clan attackingClan, List<Army> attackingArmies, Clan defendingClan,
        List<Army> defendingArmies)
    {
        var color = Console.ForegroundColor;
        Console.Clear();

        Console.ForegroundColor = AsciiMapper.GetColorForClan(defendingClan);
        Console.WriteLine($"{defendingClan.DisplayName}:");
        DrawArmies(defendingArmies);

        Console.WriteLine();

        Console.ForegroundColor = AsciiMapper.GetColorForClan(attackingClan);
        Console.WriteLine($"{attackingClan.DisplayName}:");
        DrawArmies(attackingArmies);

        Console.ForegroundColor = color;
        Console.Beep();
    }

    private static void DrawArmies(List<Army> armies)
    {
        var originalColor = Console.ForegroundColor;

        if (armies == null || armies.Count == 0)
        {
            Console.WriteLine("The garrison has fled before you!");
        }
        else
        {
            foreach (var army in armies)
            {
                Console.ForegroundColor = AsciiMapper.GetColorForClan(army.Clan);

                Console.Write(army.DisplayName);
                if (army.IsDead)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(" [X]");
                }

                Console.WriteLine();
            }
        }

        Console.ForegroundColor = originalColor;
    }
}