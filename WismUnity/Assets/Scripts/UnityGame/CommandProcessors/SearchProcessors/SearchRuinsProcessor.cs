using Assets.Scripts.CommandProcessors.Cutscenes;
using Assets.Scripts.Managers;
using System;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Locations;
using Wism.Client.Common;
using Wism.Client.Controllers;
using IWismLogger = Wism.Client.Common.IWismLogger;

namespace Assets.Scripts.CommandProcessors
{
    public class SearchRuinsProcessor : ICommandProcessor
    {
        private IWismLogger logger;
        private readonly UnityManager unityManager;
        private CutsceneStager stager;

        public SearchRuinsProcessor(IWismLoggerFactory loggerFactory, UnityManager unityGame)
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
            var searchCommand = command as SearchRuinsCommand;
            var player = searchCommand.Player;

            if (!player.IsHuman)
            {
                // AI path: skip UI entirely
                var aiState = searchCommand.Execute();
                this.unityManager.GameManager.DeselectArmies();
                return aiState;
            }

            // Human path: show UI
            if (this.stager == null)
            {
                this.stager = new CutsceneStagerFactory(this.unityManager)
                    .CreateRuinsStager(searchCommand);
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
