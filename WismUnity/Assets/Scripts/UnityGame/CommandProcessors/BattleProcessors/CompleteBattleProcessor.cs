using Assets.Scripts.Managers;
using System;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Controllers;
using Wism.Client.Commands.Armies;

namespace Assets.Scripts.CommandProcessors
{
    class CompleteBattleProcessor : ICommandProcessor
    {
        private readonly IWismLogger logger;
        private readonly UnityManager unityGame;
        private readonly InputManager inputManager;

        public CompleteBattleProcessor(IWismLoggerFactory loggerFactory, UnityManager unityGame)
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
            unityGame.SetTime(unityGame.GameManager.StandardTime);

            var completeBattle = (CompleteBattleCommand)command;
            var attackCommand = completeBattle.AttackCommand;
            var isHuman = attackCommand.Player.IsHuman;

            // Tear down any war UI and hide the battlefield
            unityGame.WarPanel.Teardown();
            HideWarScene();

            if (attackCommand.Result == ActionState.Failed)
            {
                inputManager.InputHandler.DeselectObject();
            }
            else if (attackCommand.Result == ActionState.Succeeded && isHuman)
            {
                OpenProductionPanelIfClaimingCity(attackCommand);
            }

            // If we didn’t just switch into UI for production, set the next mode
            if (unityGame.InputManager.GetInputMode() != InputMode.UI)
            {
                unityGame.InputManager.SetInputMode(isHuman
                    ? InputMode.Game
                    : InputMode.AITurn);
            }

            return command.Execute();
        }


        private void OpenProductionPanelIfClaimingCity(AttackOnceCommand attackCommand)
        {
            var tile = World.Current.Map[attackCommand.X, attackCommand.Y];
            if (tile.HasCity())
            {
                // Transition state to production
                this.unityGame.InputManager.InputHandler.DeselectObject();
                this.unityGame.SetProductionMode(ProductionMode.SelectCity);
                this.unityGame.ShowProductionPanel(tile.City);
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
