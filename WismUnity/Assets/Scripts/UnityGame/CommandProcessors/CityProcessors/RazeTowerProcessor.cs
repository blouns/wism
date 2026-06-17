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
