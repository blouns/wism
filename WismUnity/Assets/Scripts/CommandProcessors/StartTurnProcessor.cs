using Assets.Scripts.Common;
using Assets.Scripts.Managers;
using System;
using UnityEngine;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using ILogger = Wism.Client.Common.ILogger;

namespace Assets.Scripts.CommandProcessors
{
    public class StartTurnProcessor : ICommandProcessor
    {
        private readonly ILogger logger;
        private readonly UnityManager unityGame;

        public StartTurnProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
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
            var messageBox = UnityUtilities.GameObjectHardFind("NotificationBox")
                .GetComponent<NotificationBox>();

            string name = TextUtilities.CleanupName(startTurn.Player.Clan.DisplayName);
            messageBox.Notify($"{name} your turn is starting!");            

            var actionState = command.Execute();

            CenterOnCapitol();

            return actionState;
        }

        private void CenterOnCapitol()
        {
            unityGame.GoToCapitol();
        }
    }
}
