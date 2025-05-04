using Assets.Scripts.Managers;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;
using IWismLogger = Wism.Client.Common.IWismLogger;

namespace Assets.Scripts.CommandProcessors
{
    public class RecruitHeroProcessor : ICommandProcessor
    {
        private readonly string freeHeroMessage = "In {0}, a hero emerges!";

        private readonly IWismLogger logger;
        private readonly UnityManager unityGame;

        private YesNoBox yesNoBox;

        public RecruitHeroProcessor(IWismLoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is RecruitHeroCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            ActionState state = ActionState.Failed;

            var recruitCommand = (RecruitHeroCommand)command;
            var player = recruitCommand.Player;

            if (player.IsDead)
            {
                return ActionState.Failed;
            }

            if (recruitCommand.Result == ActionState.NotStarted)
            {
                // Find's a hero if one is available
                state = command.Execute();
                if (state == ActionState.Failed)
                {
                    return state;
                }
            }

            // Here is available; offer to player if enough money
            if (recruitCommand.HeroPrice == 0)
            {
                // Always accept a free hero!
                state = AcceptFreeHero(recruitCommand);
            }
            else if (player.Gold >= recruitCommand.HeroPrice)
            {
                state = OfferHeroToPlayer(recruitCommand);
            }
            else
            {
                // Not enough money
                state = ActionState.Failed;
            }

            return state;
        }

        private ActionState AcceptFreeHero(RecruitHeroCommand recruitCommand)
        {
            this.unityGame.NotifyUser(this.freeHeroMessage, recruitCommand.HeroTile.City.DisplayName);
            recruitCommand.HeroAccepted = true;

            return ActionState.Succeeded;
        }

        private ActionState OfferHeroToPlayer(RecruitHeroCommand recruitCommand)
        {
            ActionState state;

            if (this.yesNoBox == null)
            {
                this.yesNoBox = UnityUtilities.GameObjectHardFind("AcceptRejectPanel")
                    .GetComponent<YesNoBox>();
            }

            // Wait for user to accept or reject            
            if (!this.yesNoBox.Answer.HasValue)
            {
                if (!this.yesNoBox.IsActive())
                {
                    var destinationCity = recruitCommand.HeroTile.City;
                    var player = recruitCommand.Player;

                    this.yesNoBox.Ask(
                        $"A hero in {destinationCity} offers to join you for {recruitCommand.HeroPrice} gp.\n" +
                        $"You have {player.Gold} gp");

                    this.unityGame.InputManager.SetInputMode(InputMode.UI);
                }

                state = ActionState.InProgress;
            }
            // You're hired!
            else if (this.yesNoBox.Answer.Value)
            {
                this.yesNoBox.Clear();
                recruitCommand.HeroAccepted = true;
                this.unityGame.InputManager.SetInputMode(InputMode.Game);
                state = ActionState.Succeeded;
            }
            // Rejected
            else
            {
                this.yesNoBox.Clear();
                this.unityGame.InputManager.SetInputMode(InputMode.Game);
                recruitCommand.HeroAccepted = false;
                state = ActionState.Failed;
            }

            return state;
        }
    }
}