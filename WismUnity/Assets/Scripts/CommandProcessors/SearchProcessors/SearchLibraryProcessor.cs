using Assets.Scripts.Managers;
using UnityEngine;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;
using ILogger = Wism.Client.Common.ILogger;

namespace Assets.Scripts.CommandProcessors
{
    public class SearchLibraryProcessor : ICommandProcessor
    {
        private readonly ILogger logger;
        private readonly UnityManager unityGame;
        private CutsceneStager stager;

        public SearchLibraryProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is SearchLibraryCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var searchCommand = (SearchLibraryCommand)command;

            if (stager == null)
            {
                stager = CutsceneStager.CreateDefault(searchCommand, unityGame.InputManager);
                unityGame.InputManager.SetInputMode(InputMode.WaitForKey);
                unityGame.HideSelectedBox();
            }

            var result = stager.Action();

            if (result == ActionState.Failed ||
                result == ActionState.Succeeded)
            {
                unityGame.InputManager.SetInputMode(InputMode.Game);
                unityGame.GameManager.DeselectArmies();
                stager = null;
            }

            return result;
        }
    }
}
