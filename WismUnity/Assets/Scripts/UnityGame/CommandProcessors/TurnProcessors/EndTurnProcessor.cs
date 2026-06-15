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
        private YesNoBox offerOfPeaceBox;
        private VictoryOutcomeSnapshot pendingOfferOfPeace;

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

            if (this.pendingOfferOfPeace == null)
            {
                this.pendingOfferOfPeace = VictoryEvaluator.EvaluateClassicSurrender(
                    World.Current,
                    Game.Current.Players,
                    command.Player.Turn);

                if (!this.pendingOfferOfPeace.SurrenderEligible)
                {
                    this.pendingOfferOfPeace = null;
                    return ActionState.Succeeded;
                }

                Game.Current.SetVictoryOutcome(this.pendingOfferOfPeace);
            }

            if (this.offerOfPeaceBox == null)
            {
                this.offerOfPeaceBox = UnityUtilities.GameObjectHardFind("AcceptRejectPanel")
                    .GetComponent<YesNoBox>();
            }

            if (!this.offerOfPeaceBox.Answer.HasValue)
            {
                if (!this.offerOfPeaceBox.IsActive())
                {
                    this.offerOfPeaceBox.Ask(
                        "Mighty Warlord!\n" +
                        "The remaining computer lords offer peace.\n" +
                        "Accept their surrender?");

                    this.unityGame.InputManager.SetInputMode(InputMode.UI);
                }

                return ActionState.InProgress;
            }

            var accepted = this.offerOfPeaceBox.Answer.Value;
            this.offerOfPeaceBox.Clear();
            this.unityGame.InputManager.SetInputMode(InputMode.Game);

            if (accepted)
            {
                VictoryEvaluator.AcceptSurrender(Game.Current, World.Current, this.pendingOfferOfPeace);
                this.unityGame.NotifyUser(
                    "{0}, you have won. You may now inspect your domain.",
                    this.pendingOfferOfPeace.WinnerClanDisplayName);
            }
            else
            {
                VictoryEvaluator.RejectSurrender(Game.Current, this.pendingOfferOfPeace);
                this.unityGame.NotifyUser("Peace is not an option. The war continues.");
            }

            this.pendingOfferOfPeace = null;
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
