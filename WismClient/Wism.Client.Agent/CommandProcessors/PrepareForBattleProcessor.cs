﻿using System;
using System.Collections.Generic;
using System.Threading;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.CommandProcessors
{
    public class PrepareForBattleProcessor : ICommandProcessor
    {
        private ILogger logger;
        private readonly AsciiGame asciiGame;

        public PrepareForBattleProcessor(ILoggerFactory loggerFactory, AsciiGame asciiGame)
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

            var defendingPlayer = battleCommand.Defenders[0].Player;
            var defendingArmies = targetTile.MusterArmy();
            defendingArmies.Sort(new ByArmyBattleOrder(targetTile));

            DrawBattleSetupSequence(attackingPlayer, defendingPlayer);
            BattleProcessor.DrawBattleUpdate(attackingPlayer.Clan, attackingArmies, defendingPlayer.Clan, defendingArmies);

            asciiGame.GameSpeed = AsciiGame.DefaultAttackSpeed;

            return command.Execute();
        }

        private static void DrawBattleSetupSequence(Player attacker, Player defender)
        {
            Console.Clear();
            Console.WriteLine("War! ...in a senseless mind.");
            Console.WriteLine($"{attacker.Clan.DisplayName} is attacking {defender.Clan.DisplayName}!");
            for (int i = 0; i < 3; i++)
            {
                Console.Beep();
                Thread.Sleep(750);
            }
        }
    }
}