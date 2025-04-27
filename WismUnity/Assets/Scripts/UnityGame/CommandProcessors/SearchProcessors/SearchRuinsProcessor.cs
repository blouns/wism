using Assets.Scripts.CommandProcessors.Cutscenes;
using Assets.Scripts.Managers;
using System;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Locations;
using Wism.Client.Common;
using Wism.Client.Controllers;
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
            if (this.stager == null)
            {
                this.stager = new CutsceneStagerFactory(this.unityManager)
                    .CreateRuinsStager(ruinsCommand);
                if (this.unityManager.InteractiveUI)
                {
                    this.unityManager.InputManager.SetInputMode(InputMode.WaitForKey);
                }
                this.unityManager.HideSelectedBox();
            }

            var result = this.stager.Action();

            if (result == ActionState.Failed ||
                result == ActionState.Succeeded)
            {
                this.unityManager.InputManager.SetInputMode(InputMode.Game);
                this.unityManager.GameManager.DeselectArmies();
                this.stager = null;
            }

            return result;
        }
    }
}
