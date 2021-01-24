using Assets.Scripts.Common;
using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Timers;
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
        private readonly UnityManager unityGame;

        private const double DefaultInterval = 3000d;
        private Timer timer;
        private bool timerElapsed;

        public PrepareForBattleProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
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
            var attackingPlayer = prepForBattleCommand.Player;
            unityGame.CurrentAttackers = new List<Army>(prepForBattleCommand.Armies);
            unityGame.CurrentAttackers.Sort(new ByArmyBattleOrder(targetTile));

            Player defendingPlayer;
            if (prepForBattleCommand.Defenders.Count > 0)
            {
                defendingPlayer = prepForBattleCommand.Defenders[0].Player;
                unityGame.CurrentDefenders = targetTile.MusterArmy();
                unityGame.CurrentDefenders.Sort(new ByArmyBattleOrder(targetTile));
            }
            else
            {
                // Defenseless city
                var tile = World.Current.Map[prepForBattleCommand.X, prepForBattleCommand.Y];
                if (tile.City == null)
                {
                    throw new InvalidOperationException($"Expected a city to be present on {tile}");
                }
                defendingPlayer = tile.City.Player;
                unityGame.CurrentDefenders = new List<Army>();
            }

            if (!timerElapsed)
            {
                this.unityGame.SetAcceptingInput(false);
                UnityUtilities.GameObjectHardFind("Selected").SetActive(false);
                StartTimerOnFirstTime();
                ShowBattleNotification(defendingPlayer);
                DrawWarScene(targetTile);

                return ActionState.InProgress;
            }
            else
            {
                ShowWarPanel(attackingPlayer, unityGame.CurrentAttackers, defendingPlayer, unityGame.CurrentDefenders, targetTile);
                this.timerElapsed = false;
                this.timer = null;

                return command.Execute();
            }
        }

        private void StartTimerOnFirstTime()
        {
            if (this.timer == null)
            {
                this.timer = new Timer(DefaultInterval);
                this.timer.Elapsed += Timer_Elapsed;
                this.timer.Start();
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.timer.Stop();
            this.timerElapsed = true;            
        }

        private void DrawWarScene(Tile targetTile)
        {
            var worldTilemap = unityGame.WorldTilemap;
            var warGO = UnityUtilities.GameObjectHardFind("War!");
            warGO.transform.position = worldTilemap.ConvertGameToUnityCoordinates(targetTile.X, targetTile.Y);
            warGO.SetActive(true);
        }

        private static void ShowBattleNotification(Player defendingPlayer)
        {
            // TODO: need to change Notification API to show and hide the entire panel
            var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                            .GetComponent<NotificationBox>();
            string name = defendingPlayer.Clan.DisplayName;
            messageBox.Notify($"{name} you {TextUtilities.GetPresentVerb(name)} being attacked!");
        }

        public void ShowWarPanel(Player attackingPlayer, List<Army> attackingArmies, Player defendingPlayer, List<Army> defendingArmies, Tile targetTile)
        {
            if (attackingPlayer == defendingPlayer)
            {
                return;
            }

            Debug.Log($"{attackingPlayer.Clan.DisplayName} " +
                $"{TextUtilities.GetPresentVerb(attackingPlayer.Clan.DisplayName)} " +
                $"attacking {defendingPlayer.Clan.DisplayName}!");

            // Set up war UI
            unityGame.WarPanel.Initialize(attackingArmies, defendingArmies, targetTile);
            unityGame.SetTime(GameManager.WarTime);
        }
    }
}
