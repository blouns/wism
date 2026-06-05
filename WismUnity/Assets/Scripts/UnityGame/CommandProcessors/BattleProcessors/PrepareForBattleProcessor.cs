using Assets.Scripts.Common;
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
            var prep = (PrepareForBattleCommand)command;
            var isHuman = prep.Player.IsHuman;
            var tile = World.Current.Map[prep.X, prep.Y];

            // 1) Build & sort attacker list
            var attackers = new List<Army>(prep.Armies);
            attackers.Sort(new ByArmyBattleOrder(tile));
            unityGame.CurrentAttackers = attackers;

            // 2) Build & sort defender list (or empty if none)
            List<Army> defenders;
            if (prep.Defenders.Count > 0)
            {
                defenders = tile.MusterArmy();
                defenders.Sort(new ByArmyBattleOrder(tile));
            }
            else
            {
                defenders = new List<Army>();
            }
            unityGame.CurrentDefenders = defenders;

            var defendingPlayer = defenders.Count > 0
                ? defenders[0].Player
                : tile.City?.Player
                  ?? throw new InvalidOperationException($"Expected a city at {prep.X},{prep.Y} for battle");


            // 3) Determine modes
            var sceneMode = InputMode.UI;       // always UI for the war scene
            var nextMode = isHuman ? InputMode.Game : InputMode.AITurn;

            // 4) First pass: show notification, draw scene, wait
            if (!timerElapsed)
            {
                unityGame.InputManager.SetInputMode(sceneMode);
                UnityUtilities.GameObjectHardFind("SelectedBox").SetActive(false);
                StartTimerOnFirstTime();
                ShowBattleNotification(defenders.Count > 0
                    ? defendingPlayer
                    : prep.Player);
                DrawWarScene(tile);
                return ActionState.InProgress;
            }

            // 5) Timer done: show panel, reset mode, then execute
            ShowWarPanel(prep.Player, attackers, defendingPlayer, defenders, tile);
            timerElapsed = false;
            timer = null;
            unityGame.InputManager.SetInputMode(nextMode);
            return command.Execute();
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
