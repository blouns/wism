using Assets.Scripts.UnityGame.Mapping;
using Assets.Scripts.Managers;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Locations;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class RazeTowerProcessor : ICommandProcessor
    {
        private readonly UnityManager unityGame;
        private RazeTowerCommand pending;
        private YesNoBox prompt;

        public RazeTowerProcessor(IWismLoggerFactory loggerFactory, UnityManager unityGame)
        {
            if (loggerFactory is null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            this.unityGame = unityGame ?? throw new System.ArgumentNullException(nameof(unityGame));
        }

        public bool CanExecute(ICommandAction command)
        {
            return command is RazeTowerCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var towerCommand = (RazeTowerCommand)command;
            if (towerCommand.Player.IsHuman)
            {
                if (this.pending != towerCommand)
                {
                    if (!towerCommand.CanRaze())
                    {
                        this.unityGame.NotifyUser(towerCommand.FailureReason);
                        return ActionState.Failed;
                    }
                    this.pending = towerCommand;
                    this.prompt = UnityEngine.GameObject.FindGameObjectWithTag("YesNoBox").GetComponent<YesNoBox>();
                    this.unityGame.InputManager.SetInputMode(InputMode.UI);
                    this.prompt.Ask("Raze this tower?\nIt can never be rebuilt.");
                    return ActionState.InProgress;
                }
                if (!this.prompt.Answer.HasValue && !this.prompt.Cancelled) return ActionState.InProgress;
                this.pending = null;
                this.unityGame.InputManager.SetInputMode(InputMode.Game);
                this.unityGame.InputManager.SkipInput();
                if (this.prompt.Answer != true || this.prompt.Cancelled) return ActionState.Failed;
            }
            var state = towerCommand.Execute();
            if (state == ActionState.Succeeded)
            {
                MarkTowerRazed(towerCommand);
                if (this.unityGame.ShouldPresentFor(towerCommand.Player))
                {
                    this.unityGame.NotifyUser("Tower razed.");
                }
            }
            else if (this.unityGame.ShouldPresentFor(towerCommand.Player) &&
                !string.IsNullOrWhiteSpace(towerCommand.FailureReason))
            {
                this.unityGame.NotifyUser(towerCommand.FailureReason);
            }

            return state;
        }

        private void MarkTowerRazed(RazeTowerCommand towerCommand)
        {
            var towerVisuals = UnityEngine.Object.FindObjectsOfType<TowerOwnershipVisual>(true);
            foreach (var towerVisual in towerVisuals)
            {
                var coords = this.unityGame.WorldTilemap.ConvertUnityToGameVector(towerVisual.transform.position);
                if (coords.x == towerCommand.TowerTile.X &&
                    coords.y == towerCommand.TowerTile.Y)
                {
                    towerVisual.SetRazed();
                    return;
                }
            }
        }
    }
}
