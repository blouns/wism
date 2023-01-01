using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using UnityEngine;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;
using ILogger = Wism.Client.Common.ILogger;

namespace Assets.Scripts.CommandProcessors
{
    public class EndTurnProcessor : ICommandProcessor
    {
        private readonly ILogger logger;
        private readonly UnityManager unityGame;

        public EndTurnProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is EndTurnCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var endTurn = (EndTurnCommand)command;

            HandleGameOver(endTurn);
            this.unityGame.ClearInfoPanel();

            return command.Execute();
        }

        private void HandleGameOver(EndTurnCommand command)
        {
            if (command.Player.GetCities().Count == 0)
            {
                var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                    .GetComponent<NotificationBox>();
                messageBox.Notify($"Wretched {command.Player.Clan.DisplayName}! For you, the war is over!");
            }
        }
    }
}
