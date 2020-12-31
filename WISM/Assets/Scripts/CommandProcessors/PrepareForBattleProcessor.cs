using Assets.Scripts.Wism;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;
using ILogger = Wism.Client.Common.ILogger;

namespace Assets.Scripts.CommandProcessors
{
    public class PrepareForBattleProcessor : ICommandProcessor
    {
        private readonly ILogger logger;
        private readonly UnityGame unityGame;

        public PrepareForBattleProcessor(ILoggerFactory loggerFactory, UnityGame unityGame)
        {
            if (loggerFactory is null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.unityGame = unityGame ?? throw new System.ArgumentNullException(nameof(unityGame));
        }

        public bool CanExecute(ICommandAction command)
        {
            return command is PrepareForBattleCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var prepForBattleCommand = (PrepareForBattleCommand)command;
            var targetTile = World.Current.Map[prepForBattleCommand.X, prepForBattleCommand.Y];
            var attackingPlayer = prepForBattleCommand.Armies[0].Player;
            unityGame.CurrentAttackers = new List<Army>(prepForBattleCommand.Armies);
            unityGame.CurrentAttackers.Sort(new ByArmyBattleOrder(targetTile));

            var defendingPlayer = prepForBattleCommand.Defenders[0].Player;
            unityGame.CurrentDefenders = targetTile.MusterArmy();
            unityGame.CurrentDefenders.Sort(new ByArmyBattleOrder(targetTile));

            ShowWarPanel(attackingPlayer, unityGame.CurrentAttackers, defendingPlayer, unityGame.CurrentDefenders, targetTile);

            return command.Execute();
        }

        public void ShowWarPanel(Player attackingPlayer, List<Army> attackingArmies, Player defendingPlayer, List<Army> defenderingArmies, Tile targetTile)
        {
            if (attackingPlayer == defendingPlayer)
            {
                return;
            }

            Debug.Log($"{attackingPlayer.Clan.DisplayName} " +
                $"{GetPresentVerb(attackingPlayer.Clan.DisplayName)} " +
                $"attacking {defendingPlayer.Clan.DisplayName}!");

            // Set up war UI
            unityGame.WarPanel.Initialize(attackingArmies, defenderingArmies, unityGame.ArmyKinds);
            unityGame.SetTime(GameManager.WarTime);
        }

        private static string GetPresentVerb(string name)
        {
            return name.EndsWith("s") ? "are" : "is";
        }

        private static string GetPastVerb(string name)
        {
            return name.EndsWith("s") ? "have" : "has";
        }
    }
}
