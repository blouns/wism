using Assets.Scripts.Managers;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class HireHeroProcessor : ICommandProcessor
    {
        private readonly ILogger logger;
        private readonly UnityManager unityGame;

        private readonly string enterHeroNameMsg = "Name of Hero:";
        private readonly string heroBringsAlliesMsg = "{0} {1} offer to join {2}";

        private SolicitInput input;
        private string heroName;

        public HireHeroProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is HireHeroCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            ActionState state;

            var hireCommand = (HireHeroCommand)command;

            if (hireCommand.RecruitHeroCommand.Result != ActionState.Succeeded ||
                !hireCommand.RecruitHeroCommand.HeroAccepted.HasValue)
            {
                // No hero was available
                return ActionState.Failed;
            }

            if (hireCommand.HeroAccepted &&
                this.heroName == null)
            {
                // Wait for user to name the hero
                this.unityGame.InputManager.SetInputMode(InputMode.UI);
                this.heroName = GetHeroName(hireCommand);
                state = ActionState.InProgress;
            }
            else if (hireCommand.HeroAccepted)
            {
                // Hire the hero
                state = hireCommand.Execute();
                hireCommand.Hero.DisplayName = this.heroName;

                // Create any allies that will join the hero
                CreateAnyAllies(hireCommand);
                this.unityGame.InputManager.SetInputMode(InputMode.Game);
                Reset();
            }
            else
            {
                // Hero not accepted
                this.unityGame.InputManager.SetInputMode(InputMode.Game);
                state = ActionState.Failed;
                Reset();
            }

            return state;
        }

        private void Reset()
        {
            this.input.Clear();
            this.input = null;
            this.heroName = null;
        }

        private void CreateAnyAllies(HireHeroCommand hireCommand)
        {
            var player = hireCommand.Player;
            var allies = hireCommand.HeroAllies;
            var tile = hireCommand.HeroTile;

            if (allies == null ||
                allies.Count == 0)
            {
                // No allies for you
                return;
            }

            this.unityGame.NotifyUser(this.heroBringsAlliesMsg,
                allies.Count,
                allies[0].DisplayName,
                this.heroName);

            this.unityGame.GameManager.ConscriptArmies(player, tile, allies);
        }


        /// <summary>
        /// Return the hero name from user input.
        /// </summary>
        /// <param name="command">Command object</param>
        /// <returns>Hero's name or null if waiting on user input</returns>
        private string GetHeroName(HireHeroCommand command)
        {
            string name = null;

            if (this.input == null)
            {
                this.input = UnityUtilities.GameObjectHardFind("SolicitInputPanel")
                    .GetComponent<SolicitInput>();
            }

            // Show input box to enter hero's name
            if (!this.input.IsInitialized() &&
                this.input.OkCancelResult == UI.OkCancel.None)
            {
                this.input.Initialize(this.enterHeroNameMsg, command.HeroDisplayName);
                this.input.Show();
            }

            // Wait for user input
            if (this.input.OkCancelResult == UI.OkCancel.Ok)
            {
                name = this.input.GetInputText();
            }

            return name;
        }
    }
}