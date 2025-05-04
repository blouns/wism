using Assets.Scripts.CommandProcessors.Cutscenes;
using Assets.Scripts.Managers;
using System;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Controllers;
using IWismLogger = Wism.Client.Common.IWismLogger;
using Wism.Client.Commands.Locations;

namespace Assets.Scripts.CommandProcessors
{
    public class SearchSageProcessor : ICommandProcessor
    {
        private IWismLogger logger;
        private readonly UnityManager unityGame;
        private Librarian librarian = new Librarian();
        private CutsceneStager stager;

        public SearchSageProcessor(IWismLoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is SearchSageCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var searchCommand = (SearchSageCommand)command;

            if (this.stager == null)
            {
                this.stager = new CutsceneStagerFactory(this.unityGame)
                    .CreateSageStager(searchCommand);
                this.unityGame.InputManager.SetInputMode(InputMode.WaitForKey);
                this.unityGame.HideSelectedBox();
            }

            var result = this.stager.Action();

            if (result == ActionState.Failed ||
                result == ActionState.Succeeded)
            {
                this.unityGame.InputManager.SetInputMode(InputMode.Game);
                this.unityGame.GameManager.DeselectArmies();
                this.stager = null;
            }

            return result;
        }
    }
}
