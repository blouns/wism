﻿using Assets.Scripts.Common;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Controllers;
using Wism.Client.MapObjects;
using IWismLogger = Wism.Client.Common.IWismLogger;
using Wism.Client.Comparers;
using Wism.Client.Commands.Armies;

namespace Assets.Scripts.CommandProcessors
{
    public class PrepareForBattleProcessor : ICommandProcessor
    {
        private readonly IWismLogger logger;
        private readonly UnityManager unityGame;

        private const double DefaultInterval = 3000d;
        private Timer timer;
        private bool timerElapsed;

        public PrepareForBattleProcessor(IWismLoggerFactory loggerFactory, UnityManager unityGame)
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
            this.unityGame.CurrentAttackers = new List<Army>(prepForBattleCommand.Armies);
            this.unityGame.CurrentAttackers.Sort(new ByArmyBattleOrder(targetTile));

            Player defendingPlayer;
            if (prepForBattleCommand.Defenders.Count > 0)
            {
                defendingPlayer = prepForBattleCommand.Defenders[0].Player;
                this.unityGame.CurrentDefenders = targetTile.MusterArmy();
                this.unityGame.CurrentDefenders.Sort(new ByArmyBattleOrder(targetTile));
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
                this.unityGame.CurrentDefenders = new List<Army>();
            }

            if (!this.timerElapsed)
            {
                this.unityGame.InputManager.SetInputMode(InputMode.UI);
                UnityUtilities.GameObjectHardFind("SelectedBox").SetActive(false);
                StartTimerOnFirstTime();
                ShowBattleNotification(defendingPlayer);
                DrawWarScene(targetTile);

                return ActionState.InProgress;
            }
            else
            {
                ShowWarPanel(attackingPlayer, this.unityGame.CurrentAttackers, defendingPlayer, this.unityGame.CurrentDefenders, targetTile);
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
            var worldTilemap = this.unityGame.WorldTilemap;
            var warGO = UnityUtilities.GameObjectHardFind("War!");
            warGO.transform.position = worldTilemap.ConvertGameToUnityVector(targetTile.X, targetTile.Y);
            warGO.SetActive(true);
        }

        private static void ShowBattleNotification(Player defendingPlayer)
        {
            if (defendingPlayer.Clan.ShortName == "Neutral")
            {
                return;
            }

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
            this.unityGame.WarPanel.Initialize(attackingArmies, defendingArmies, targetTile);
            this.unityGame.SetTime(this.unityGame.GameManager.WarTime);
        }
    }
}
