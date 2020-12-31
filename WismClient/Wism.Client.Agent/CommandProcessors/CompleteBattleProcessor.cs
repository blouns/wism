using System;
using System.Collections.Generic;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.CommandProcessors
{
    public class CompleteBattleProcessor : ICommandProcessor
    {
        private readonly AsciiGame asciiGame;
        private ILogger logger;

        public CompleteBattleProcessor(ILoggerFactory loggerFactory, AsciiGame asciiGame)
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
            return command is CompleteBattleCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var battleCompleteCommand = (CompleteBattleCommand)command;
            var targetTile = World.Current.Map[battleCompleteCommand.X, battleCompleteCommand.Y];
            var attackingPlayer = battleCompleteCommand.Armies[0].Player;
            var attackingArmies = new List<Army>(battleCompleteCommand.Armies);
            attackingArmies.Sort(new ByArmyBattleOrder(targetTile));

            var defendingPlayer = battleCompleteCommand.Defenders[0].Player;
            var defendingArmies = targetTile.MusterArmy();
            defendingArmies.Sort(new ByArmyBattleOrder(targetTile));

//            BattleProcessor.DrawBattleUpdate(attackingPlayer.Clan, attackingArmies, defendingPlayer.Clan, defendingArmies);

            var name = attackingPlayer.Clan.DisplayName;
            var presentVerb = name.EndsWith('s') ? "are" : "is";
            var pastVerb = name.EndsWith('s') ? "have" : "has";

            // Check battle result
            var battleResult = battleCompleteCommand.AttackCommand.Result;
            if (battleResult == ActionState.Succeeded)
            {
                Console.WriteLine($"{name} {presentVerb} victorious!");
            }
            else if (battleResult == ActionState.Failed)
            {
                Console.WriteLine($"{name} {pastVerb} been defeated!");
            }
            else
            {
                Console.WriteLine("Error: Unexpected game state" + battleResult);
            }
            Console.ReadKey();

            asciiGame.GameSpeed = AsciiGame.DefaultGameSpeed;

            return command.Execute();
        }
    }
}
