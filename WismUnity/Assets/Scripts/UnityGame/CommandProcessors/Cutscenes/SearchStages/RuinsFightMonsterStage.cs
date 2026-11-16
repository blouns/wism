using Wism.Client.Commands.Locations;

using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;
using Wism.Client.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class RuinsFightMonsterStage : LocationCutsceneStage
    {
        public RuinsFightMonsterStage(SearchRuinsCommand command)
            : base(command)
        {
        }

        protected override SceneResult ActionInternal()
        {
            if (this.Command.Result == ActionState.NotStarted)
            {
                var result = this.Command.Execute();
                var heroDied = this.Hero?.IsDead == true;
                if (heroDied)
                {
                    var unity = this.InputManager.UnityManager;
                    var casualtyIcon = unity.WarPanel?.KilledPrefab?.GetComponent<Image>();
                    unity.WorldTilemap.ShowSearchDeathIcon(casualtyIcon?.sprite);
                }

                var message = result == ActionState.Succeeded
                    ? "...and is victorious!"
                    : heroDied ? "...and is slain!" : "The search could not be completed.";
                this.InputManager.SetInputMode(InputMode.WaitForKey);
                GameObject.FindGameObjectWithTag("NotificationBox").GetComponent<NotificationBox>()
                    .Notify(message, double.PositiveInfinity);
            }

            if (!KeyPressed())
            {
                return SceneResult.Wait;
            }

            return this.Command.Result == ActionState.Succeeded ? SceneResult.Continue : SceneResult.Failure;
        }
    }
}
