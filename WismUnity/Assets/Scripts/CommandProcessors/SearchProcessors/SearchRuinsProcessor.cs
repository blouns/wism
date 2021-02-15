using Assets.Scripts.Managers;
using System;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;
using ILogger = Wism.Client.Common.ILogger;

namespace Assets.Scripts.CommandProcessors
{
    public class SearchRuinsProcessor : ICommandProcessor
    {
        private ILogger logger;
        private readonly UnityManager unityGame;
        private CutsceneStager stager;

        public SearchRuinsProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.unityGame = unityGame ?? throw new ArgumentNullException(nameof(unityGame));            
        }

        public bool CanExecute(ICommandAction command)
        {
            // Ruins and tombs are interchangable
            return command is SearchRuinsCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var ruinsCommand = command as SearchRuinsCommand;
            if (stager == null)
            {
                stager = CutsceneStager.CreateDefault(ruinsCommand, unityGame.InputManager);
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
