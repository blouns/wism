using Assets.Scripts.Managers;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class SelectArmyProcessor : ICommandProcessor
    {
        private readonly ILogger logger;
        private readonly UnityManager unityGame;

        public SelectArmyProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
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