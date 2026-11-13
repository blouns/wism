using Assets.Scripts.Managers;
using UnityEngine;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Games;
using Wism.Client.Common;
using Wism.Client.Controllers;
using IWismLogger = Wism.Client.Common.IWismLogger;

namespace Assets.Scripts.CommandProcessors
{
    public class LoadGameProcessor : ICommandProcessor
    {
        private readonly IWismLogger logger;
        private readonly UnityManager unityGame;

        public LoadGameProcessor(IWismLoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is LoadGameCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var loadGameCommand = (LoadGameCommand)command;
            ActionState actionState = command.Execute();
            this.unityGame.GameManager.CompletePendingLoad(loadGameCommand, actionState);

            if (actionState == ActionState.Failed)
            {
                Debug.LogWarning("Load failed: " + loadGameCommand.FailureReason);
                if (this.unityGame.AwaitingInitialLoad)
                    this.unityGame.InputManager.HandleSaveLoadPicker(false);
                this.unityGame.NotifyUser("Could not load the game. Check the save file and required world, then try again.");
                return actionState;
            }

            if (actionState == ActionState.Succeeded)
            {
                // Load Unity game state
                PersistanceManager.LoadLastSnapshot(this.unityGame);

                // Reset Unity managers
                this.unityGame.Reset();

                Debug.Log($"Game loaded successfully '{loadGameCommand.Snapshot.World.Name}'.");
            }

            return actionState;
        }
    }
}
