using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using UnityEngine;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using IWismLogger = Wism.Client.Common.IWismLogger;

namespace Assets.Scripts.CommandProcessors
{
    public class EndTurnProcessor : ICommandProcessor
    {
        private readonly IWismLogger logger;
        private readonly UnityManager unityGame;

        public EndTurnProcessor(IWismLoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is EndTurnCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var endTurn = (EndTurnCommand)command;

            if (endTurn.Result == ActionState.NotStarted)
            {
                HandleGameOver(endTurn);
                this.unityGame.ClearInfoPanel();

                var result = command.Execute();
                if (result != ActionState.Succeeded)
                {
                    return result;
                }
            }

            return HandleOfferOfPeace(endTurn);
        }

        private void HandleGameOver(EndTurnCommand command)
        {
            if (!this.unityGame.ShouldPresentFor(command.Player))
            {
                return;
            }

            if (command.Player.GetCities().Count == 0)
            {
                var messageBox = GameObject.FindGameObjectWithTag("NotificationBox")
                    .GetComponent<NotificationBox>();
                messageBox.Notify($"Wretched {command.Player.Clan.DisplayName}! For you, the war is over!");
            }
        }

        private ActionState HandleOfferOfPeace(EndTurnCommand command)
        {
            if (!ShouldOfferPeace(command))
            {
                return ActionState.Succeeded;
            }

            var offer = VictoryEvaluator.EvaluateClassicSurrender(
                World.Current, Game.Current.Players, command.Player.Turn);
            if (offer.SurrenderEligible)
                Game.Current.SetVictoryOutcome(offer);
            // Presentation observes the durable outcome, including after a load.
            // Finish this command before pausing the next turn for the decision.
            return ActionState.Succeeded;
        }

        private static bool ShouldOfferPeace(EndTurnCommand command)
        {
            if (command.Player == null ||
                !command.Player.IsHuman ||
                Game.Current.GameState == GameState.GameOver)
            {
                return false;
            }

            var outcome = Game.Current.VictoryOutcome;
            return outcome == null ||
                   outcome.OutcomeKind == VictoryOutcomeKind.None;
        }
    }
}
