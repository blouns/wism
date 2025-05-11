using System;
using System.Collections.Generic;
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

public class BattleProcessor : InstrumentedProcessor
{
    private readonly IWismLogger logger;

    public BattleProcessor(IWismLoggerFactory loggerFactory, CommandIpcPublisher publisher)
        : base(publisher)
    {
        this.logger = loggerFactory.CreateLogger();
    }

    public override bool CanExecute(ICommandAction command)
    {
        return command is AttackOnceCommand;
    }

    public override ActionState ExecuteInternal(ICommandAction command)
    {
        var battleCommand = (AttackOnceCommand)command;

        logger.LogInformation("Executing BattleProcessor logic for AttackOnceCommand");

        var result = battleCommand.Execute();

        logger.LogInformation($"Battle result: {result}");

        var targetTile = World.Current.Map[battleCommand.X, battleCommand.Y];
        var attackingPlayer = battleCommand.OriginalAttackingArmies[0].Player;
        var attackingArmies = battleCommand.OriginalAttackingArmies;
        attackingArmies.Sort(new ByArmyBattleOrder(targetTile));

        if (battleCommand.OriginalDefendingArmies == null || battleCommand.OriginalDefendingArmies.Count == 0)
        {
            logger.LogInformation("No defenders found on target tile.");
            Notify.DisplayAndWait("Press any key to continue...");
        }
        else
        {
            var defendingPlayer = battleCommand.OriginalDefendingArmies[0].Player;
            var defendingArmies = battleCommand.OriginalDefendingArmies;
            defendingArmies.Sort(new ByArmyBattleOrder(targetTile));

            logger.LogInformation($"Battle between {attackingPlayer.Clan} and {defendingPlayer.Clan}");
            DrawBattleUpdate(attackingPlayer.Clan, attackingArmies, defendingPlayer.Clan, defendingArmies);
        }

        return result;
    }

    internal static void DrawBattleUpdate(Clan attackingClan, List<Army> attackingArmies, Clan defendingClan,
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
