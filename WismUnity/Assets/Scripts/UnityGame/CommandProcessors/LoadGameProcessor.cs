using Assets.Scripts.Managers;
using UnityEngine;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;
using ILogger = Wism.Client.Common.ILogger;

namespace Assets.Scripts.CommandProcessors
{
    public class LoadGameProcessor : ICommandProcessor
    {
        private readonly ILogger logger;
        private readonly UnityManager unityGame;

        public LoadGameProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
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