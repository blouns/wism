using Assets.Scripts.CommandProcessors.Cutscenes;
using Assets.Scripts.Managers;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
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

            if (this.stager == null)
            {
                this.stager = new CutsceneStagerFactory(this.unityGame)
                    .CreateLibraryStager(searchCommand);
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
