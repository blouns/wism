using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using System.Collections.Generic;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Controllers;
using IWismLogger = Wism.Client.Common.IWismLogger;
using Wism.Client.Core.Armies;
using Wism.Client.Commands.Players;

namespace Assets.Scripts.CommandProcessors
{
    public class RenewProductionProcessor : ICommandProcessor
    {
        private readonly IWismLogger logger;
        private readonly UnityManager unityGame;
        private YesNoBox okCancelBox;
        private YesNoBox yesNoCancelBox;
        private bool renewalCleared;

        private enum YesNoCancel
        {
            None,
            Yes,
            No,
            Cancel
        }

        public RenewProductionProcessor(IWismLoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is RenewProductionCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var renewCommand = (RenewProductionCommand)command;
            var player = renewCommand.Player;

            if (player.IsDead)
            {
                return ActionState.Failed;
            }

            // Show production on first city
            if (player.Turn == 1)
            {
                OpenProductionPanel(player);
                return ActionState.Succeeded;
            }

            ActionState result = ActionState.InProgress;
            var reviewCommand = renewCommand.ReviewProductionCommand;
            this.unityGame.InputManager.SetInputMode(InputMode.UI);

            // Clear the renewal queue and ask user for input
            if (!this.renewalCleared)
            {
                renewCommand.ArmiesToRenew = new List<ArmyInTraining>();
                this.renewalCleared = true;
            }

            // Show deliveries
            if (reviewCommand.ArmiesDeliveredResult != null &&
                reviewCommand.ArmiesDeliveredResult.Count > 0)
            {
                HandleDeliveries(reviewCommand);
            }
            // Show production
            else if (reviewCommand.ArmiesProducedResult != null &&
                     reviewCommand.ArmiesProducedResult.Count > 0)
            {
                HandleProduction(reviewCommand, renewCommand);
            }
            // Renew production
            else
            {
                // Renew all armies in ArmiesToRenew
                result = command.Execute();
                this.unityGame.InputManager.SetInputMode(InputMode.Game);
                Reset();
            }

            return result;
        }

        private void HandleProduction(ReviewProductionCommand reviewCommand, RenewProductionCommand renewCommand)
        {
            var renewResult = AskToRenewProduction(reviewCommand.ArmiesProducedResult[0]);
            switch (renewResult)
            {
                case YesNoCancel.Yes:
                    // Renew production
                    renewCommand.ArmiesToRenew.Add(reviewCommand.ArmiesProducedResult[0]);
                    reviewCommand.ArmiesProducedResult.RemoveAt(0);
                    break;
                case YesNoCancel.No:
                    // Stop production
                    reviewCommand.ArmiesProducedResult.RemoveAt(0);
                    break;
                case YesNoCancel.Cancel:
                    // Renew all remaining production
                    renewCommand.ArmiesToRenew.AddRange(reviewCommand.ArmiesProducedResult);
                    reviewCommand.ArmiesProducedResult.Clear();
                    break;
                default:
                    // Wait for user
                    break;
            }
        }

        private void HandleDeliveries(ReviewProductionCommand reviewCommand)
        {
            var deliveryResult = ShowDelivery(reviewCommand.ArmiesDeliveredResult[0]);
            switch (deliveryResult)
            {
                case OkCancel.Ok:
                    // Show next report
                    reviewCommand.ArmiesDeliveredResult.RemoveAt(0);
                    break;
                case OkCancel.Cancel:
                    // Cancelled the report
                    reviewCommand.ArmiesDeliveredResult.Clear();
                    break;
                default:
                    // Wait for user
                    break;
            }
        }

        private YesNoCancel AskToRenewProduction(ArmyInTraining ait)
        {
            YesNoCancel state = YesNoCancel.None;

            // Ask the user if they'd like to renew production
            if (this.yesNoCancelBox == null)
            {
                this.yesNoCancelBox = UnityUtilities.GameObjectHardFind("RenewProductionPanel")
                    .GetComponent<YesNoBox>();
            }

            // If new request prompt the user
            if (!this.yesNoCancelBox.Cancelled &&
                !this.yesNoCancelBox.Answer.HasValue)
            {
                this.yesNoCancelBox.Ask($"{ait.DisplayName} - Produced!");
            }
            // If User skipped the reports
            else if (this.yesNoCancelBox.Cancelled)
            {
                this.yesNoCancelBox.Clear();
                state = YesNoCancel.Cancel;
            }
            // If User has selected "Yes"
            else if (this.yesNoCancelBox.Answer.HasValue &&
                     this.yesNoCancelBox.Answer.Value)
            {
                this.yesNoCancelBox.Clear();
                state = YesNoCancel.Yes;
            }
            // If User selected "No"
            else if (this.yesNoCancelBox.Answer.HasValue &&
                     !this.yesNoCancelBox.Answer.Value)
            {
                this.yesNoCancelBox.Clear();
                state = YesNoCancel.No;
            }
            // Else wait for user input
            else
            {
                // Do nothing
            }

            return state;
        }

        private OkCancel ShowDelivery(ArmyInTraining ait)
        {
            OkCancel state = OkCancel.None;

            if (this.okCancelBox == null)
            {
                this.okCancelBox = UnityUtilities.GameObjectHardFind("ReviewDeliveriesPanel")
                    .GetComponent<YesNoBox>();
            }

            // If new request prompt the user
            if (!this.yesNoCancelBox.Cancelled &&
                !this.yesNoCancelBox.Answer.HasValue)
            {
                this.yesNoCancelBox.Ask($"{ait.DisplayName} reaches {ait.DestinationCity.DisplayName}");
            }
            else if (this.okCancelBox.Cancelled)
            {
                // User skipped the reports
                this.okCancelBox.Clear();
                state = OkCancel.Cancel;
            }
            else if (this.okCancelBox.Answer.HasValue)
            {
                // User has selected "Ok"
                this.okCancelBox.Clear();
                state = OkCancel.Ok;
            }
            else
            {
                // Wait for user input
                state = OkCancel.Picking;
            }

            return state;
        }

        private void OpenProductionPanel(Player player)
        {
            if (player.Capitol != null)
            {
                // Transition state to production
                this.unityGame.InputManager.InputHandler.DeselectObject();
                this.unityGame.SetProductionMode(ProductionMode.SelectCity);
                this.unityGame.ShowProductionPanel(player.Capitol);
            }
        }

        private void Reset()
        {
            if (this.yesNoCancelBox != null)
            {
                this.yesNoCancelBox.Clear();
                this.yesNoCancelBox = null;
            }

            if (this.okCancelBox != null)
            {
                this.okCancelBox.Clear();
                this.okCancelBox = null;
            }

            this.renewalCleared = false;
        }
    }
}