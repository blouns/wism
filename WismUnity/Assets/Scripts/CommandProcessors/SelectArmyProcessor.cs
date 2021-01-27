using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class SelectArmyProcessor : ICommandProcessor
    {
        private readonly ILogger logger;
        private readonly UnityManager unityGame;
        private readonly InputHandler inputHandler;

        public SelectArmyProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
        {
            if (loggerFactory is null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.unityGame = unityGame ?? throw new System.ArgumentNullException(nameof(unityGame));
            this.inputHandler = this.unityGame.GetComponent<InputManager>().InputHandler;
        }

        public bool CanExecute(ICommandAction command)
        {
            return command is SelectNextArmyCommand || 
                   command is SelectArmyCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            ActionState result = command.Execute();

            //if (result == ActionState.Succeeded)
            //{
            //    inputHandler.CenterOnTile(Game.Current.GetSelectedArmies()[0].Tile);
            //}

            return result;
        }
    }
}