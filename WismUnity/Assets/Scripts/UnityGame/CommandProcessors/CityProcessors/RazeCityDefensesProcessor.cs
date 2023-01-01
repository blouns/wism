using Assets.Scripts.CommandProcessors.Cutscenes;
using Assets.Scripts.Managers;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;
using ILogger = Wism.Client.Common.ILogger;

namespace Assets.Scripts.CommandProcessors
{
    public class RazeCityDefensesProcessor : ICommandProcessor
    {
        private readonly ILogger logger;
        private readonly UnityManager unityGame;
        private CutsceneStager stager;

        public RazeCityDefensesProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is RazeCityCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var cityCommand = (RazeCityCommand)command;

            if (this.stager == null)
            {
                this.stager = new CutsceneStagerFactory(this.unityGame)
                    .CreateRazeCityStager(cityCommand);
            }

            var result = this.stager.Action();

            if (result == ActionState.Failed ||
                result == ActionState.Succeeded)
            {
                this.unityGame.InputManager.SetInputMode(InputMode.Game);
                this.stager = null;
            }

            return result;
        }
    }
}
