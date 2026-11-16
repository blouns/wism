using Assets.Scripts.Common;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using UnityEngine;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Controllers;
using IWismLogger = Wism.Client.Common.IWismLogger;
using Wism.Client.Commands.Players;

namespace Assets.Scripts.CommandProcessors
{
    public class StartTurnProcessor : ICommandProcessor
    {
        private readonly IWismLogger logger;
        private readonly UnityManager unityGame;
        private StartTurnCommand announcedTurn;
        private NotificationBox announcement;
        private bool waitForAcknowledgment;
        private bool acknowledged;
        private float dismissAt;

        public StartTurnProcessor(IWismLoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is StartTurnCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var startTurn = (StartTurnCommand)command;
            var player = startTurn.Player;
            var input = this.unityGame.InputManager;

            if (this.announcedTurn != startTurn)
            {
                this.unityGame.ApplyPlayerControlAtTurnStart(player);
                if (!this.unityGame.InteractiveUI || player.IsDead)
                {
                    return startTurn.Execute();
                }

                this.announcedTurn = startTurn;
                this.announcement = UnityUtilities.GameObjectHardFind("NotificationBox").GetComponent<NotificationBox>();
                var isEliminated = player.GetCities().Count == 0 && !Game.Current.IgnoreGameOver;
                var name = TextUtilities.CleanupName(player.Clan.DisplayName);
                this.waitForAcknowledgment = player.IsHuman && !isEliminated;
                this.acknowledged = false;
                this.dismissAt = Time.realtimeSinceStartup + NotificationBox.DefaultInterval;

                if (this.waitForAcknowledgment)
                {
                    this.unityGame.GoToCapitol(player);
                    input.SetInputMode(InputMode.WaitForKey);
                    input.KeyPressed += Acknowledge;
                }

                if (isEliminated)
                {
                    startTurn.Execute();
                }
                this.announcement.Notify(
                    isEliminated ? $"Wretched {name}, for you the war is over..." : $"{name}, your turn is starting!",
                    double.PositiveInfinity);
                return ActionState.InProgress;
            }

            if (this.waitForAcknowledgment ? !this.acknowledged : Time.realtimeSinceStartup < this.dismissAt)
            {
                return ActionState.InProgress;
            }

            if (this.waitForAcknowledgment)
            {
                input.KeyPressed -= Acknowledge;
                input.SkipInput();
            }
            input.SetInputMode(player.IsHuman ? InputMode.Game : InputMode.AITurn);
            this.announcement.ClearNotification();
            this.announcement = null;
            this.announcedTurn = null;
            return startTurn.Result == ActionState.NotStarted ? startTurn.Execute() : startTurn.Result;
        }

        private void Acknowledge()
        {
            this.acknowledged = true;
        }
    }
}
