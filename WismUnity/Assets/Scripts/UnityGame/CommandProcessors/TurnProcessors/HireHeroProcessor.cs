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
        private readonly IWismLogger logger;
        private readonly UnityManager unityGame;

        private readonly string enterHeroNameMsg = "Name of Hero:";
        private readonly string heroBringsAlliesMsg = "{0} {1} offer to join {2}";

        private SolicitInput input;
        private string heroName;

        public HireHeroProcessor(IWismLoggerFactory loggerFactory, UnityManager unityGame)
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
            var hire = (HireHeroCommand)command;
            var player = hire.Player;

            // No hero available; fail immediately
            if (hire.RecruitHeroCommand.Result != ActionState.Succeeded ||
                !hire.RecruitHeroCommand.HeroAccepted.HasValue ||
                !hire.RecruitHeroCommand.HeroAccepted.Value)
            {
                return ActionState.Failed;
            }

            // AI and headless paths skip UI entirely.
            if (!player.IsHuman || !this.unityGame.InteractiveUI)
            {                
                var aiState = hire.Execute();
                this.heroName = hire.HeroDisplayName;
                hire.Hero.DisplayName = this.heroName;
                CreateAnyAllies(hire);
                this.heroName = null;

                return aiState;
            }

            // Human path: handle naming dialog then actual hire
            if (this.heroName == null)
            {
                // Tell the UI to pop up your naming dialog
                unityGame.InputManager.SetInputMode(InputMode.UI);
                this.heroName = GetHeroName(hire);
                return ActionState.InProgress;
            }
            else
            {
                // Name’s been entered—actually hire
                var humanState = hire.Execute();
                hire.Hero.DisplayName = this.heroName;
                CreateAnyAllies(hire);

                // Back to normal gameplay input
                unityGame.InputManager.SetInputMode(InputMode.Game);
                Reset();
                return humanState;
            }
        }

        private void Reset()
        {
            this.input?.Clear();
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
