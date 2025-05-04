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
            ActionState actionState = command.Execute();

            if (actionState == ActionState.Succeeded)
            {
                // Load Unity game state
                // TODO: Ensure this cannot be out of sync w/ game state
                //       Currently rapid saves would get out of sync.
                PersistanceManager.LoadLastSnapshot(this.unityGame);

                // Reset Unity managers
                this.unityGame.Reset();

                var loadGameCommand = (LoadGameCommand)command;
                Debug.Log($"Game loaded successfully '{loadGameCommand.Snapshot.World.Name}'.");
            }

            return actionState;
        }
    }
}