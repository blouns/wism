using Assets.Scripts.Managers;
using Assets.Scripts.UI;
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
        private readonly InputManager inputManager;

        public CompleteBattleProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
        {
            if (loggerFactory is null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.unityGame = unityGame ?? throw new ArgumentNullException(nameof(unityGame));
            this.inputManager = this.unityGame.GetComponent<InputManager>();
        }

        public bool CanExecute(ICommandAction command)
        {
            return command is CompleteBattleCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            unityGame.SetTime(GameManager.StandardTime);
            this.unityGame.InputManager.SetInputMode(InputMode.Game);

            var attackCommand = ((CompleteBattleCommand)command).AttackCommand;
            var result = attackCommand.Result;
            switch (result)
            {
                case ActionState.Succeeded:
                    unityGame.WarPanel.Teardown();
                    unityGame.SetTime(GameManager.StandardTime);
                    this.unityGame.InputManager.SetInputMode(InputMode.Game);
                    OpenProductionPanelIfClaimingCity(attackCommand);
                    break;

                case ActionState.Failed:
                    inputManager.InputHandler.DeselectObject();
                    unityGame.WarPanel.Teardown();
                    
                    break;
                default:
                    throw new InvalidOperationException("Unexpected ActionState: " + result);
            }

            HideWarScene();

            return command.Execute();
        }

        private void OpenProductionPanelIfClaimingCity(AttackOnceCommand attackCommand)
        {            
            var defender = attackCommand.OriginalDefendingArmies[0];
            var tile = defender.Tile;
            if (tile.HasCity())
            {
                // Transition state to production
                unityGame.InputManager.InputHandler.DeselectObject();
                unityGame.SetProductionMode(ProductionMode.SelectCity);
                unityGame.ShowProductionPanel(tile.City);
                this.unityGame.InputManager.SetInputMode(InputMode.UI);
            }
        }

        private void HideWarScene()
        {
            var warGO = UnityUtilities.GameObjectHardFind("War!");
            warGO.SetActive(false);            
        }
    }
}
