﻿using Assets.Scripts.CommandProcessors.Cutscenes;
using Assets.Scripts.Managers;
using System;
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

            if (this.stager == null)
            {
                this.stager = new CutsceneStagerFactory(this.unityGame)
                    .CreateTempleStager(searchCommand);
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
