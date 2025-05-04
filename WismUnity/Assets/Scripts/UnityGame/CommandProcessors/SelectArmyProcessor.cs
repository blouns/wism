using Assets.Scripts.Managers;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Controllers;
using Wism.Client.Commands.Armies;

namespace Assets.Scripts.CommandProcessors
{
    public class SelectArmyProcessor : ICommandProcessor
    {
        private readonly IWismLogger logger;
        private readonly UnityManager unityGame;

        public SelectArmyProcessor(IWismLoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is SelectNextArmyCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var selectNext = (SelectNextArmyCommand)command;

            var state = selectNext.Execute();
            if (state == ActionState.Succeeded)
            {
                var armies = Game.Current.GetSelectedArmies();
                if (armies != null && armies.Count > 0)
                {
                    this.unityGame.SetCameraToSelectedBox();
                }
            }

            return state;
        }
    }
}