using Assets.Scripts.CommandProcessors.Cutscenes;
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
        private readonly UnityManager unityManager;
        private CutsceneStager stager;

        public SearchRuinsProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.unityManager = unityGame ?? throw new ArgumentNullException(nameof(unityGame));            
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
                stager = new CutsceneStagerFactory(unityManager)
                    .CreateRuinsStager(ruinsCommand);
                unityManager.InputManager.SetInputMode(InputMode.WaitForKey);
                unityManager.HideSelectedBox();
            }
            
            var result = stager.Action();

            if (result == ActionState.Failed ||
                result == ActionState.Succeeded)
            {
                unityManager.InputManager.SetInputMode(InputMode.Game);
                unityManager.GameManager.DeselectArmies();
                stager = null;
            }

            return result;
        }
    }
}
