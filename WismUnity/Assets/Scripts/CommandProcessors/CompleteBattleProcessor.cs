using Assets.Scripts.Wism;
using System;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    class CompleteBattleProcessor : ICommandProcessor
    {
        private readonly ILogger logger;
        private readonly UnityManager unityGame;

        public CompleteBattleProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is CompleteBattleCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var result = ((CompleteBattleCommand)command).AttackCommand.Result;
            switch (result)
            {
                case ActionState.Succeeded:
                    unityGame.WarPanel.Teardown();
                    unityGame.SetTime(GameManager.StandardTime);
                    break;

                case ActionState.Failed:
                    unityGame.DeselectObject();
                    unityGame.WarPanel.Teardown();
                    unityGame.SetTime(GameManager.StandardTime);
                    break;
                default:
                    throw new InvalidOperationException("Unexpected ActionState: " + result);
            }

            return command.Execute();
        }
    }
}
