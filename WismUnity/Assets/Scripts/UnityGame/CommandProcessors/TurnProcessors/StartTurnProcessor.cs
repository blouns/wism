using Assets.Scripts.Common;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
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

            CenterOnCapitol(startTurn.Player);

            var messageBox = UnityUtilities.GameObjectHardFind("NotificationBox")
                .GetComponent<NotificationBox>();

            string name = TextUtilities.CleanupName(startTurn.Player.Clan.DisplayName);
            if (startTurn.Player.GetCities().Count == 0)
            {
                // Player has died                
                messageBox.Notify($"Wretched {name}, for you the war is over...");
            }
            else
            {
                messageBox.Notify($"{name} your turn is starting!");
            }

            return command.Execute();
        }

        private void CenterOnCapitol(Player player)
        {
            this.unityGame.GoToCapitol(player);
        }
    }
}
