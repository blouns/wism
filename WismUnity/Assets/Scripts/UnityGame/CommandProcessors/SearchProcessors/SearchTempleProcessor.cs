using Assets.Scripts.CommandProcessors.Cutscenes;
using Assets.Scripts.Managers;
using System;
using UnityEngine;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;
using ILogger = Wism.Client.Common.ILogger;

namespace Assets.Scripts.CommandProcessors
{
    public class SearchTempleProcessor : ICommandProcessor
    {
        private ILogger logger;
        private readonly UnityManager unityGame;
        private CutsceneStager stager;

        public SearchTempleProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is SearchTempleCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var searchCommand = (SearchTempleCommand)command;

            if (stager == null)
            {
                stager = new CutsceneStagerFactory(unityGame)
                    .CreateTempleStager(searchCommand);
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
