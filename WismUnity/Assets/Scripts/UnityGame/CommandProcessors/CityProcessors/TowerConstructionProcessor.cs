using Assets.Scripts.Managers;
using UnityEngine;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Locations;
using Wism.Client.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public sealed class TowerConstructionProcessor : ICommandProcessor
    {
        private readonly UnityManager manager;
        private BuildTowerCommand pending;
        private YesNoBox prompt;

        public TowerConstructionProcessor(UnityManager manager) => this.manager = manager;

        public bool CanExecute(ICommandAction command) => command is BuildTowerCommand;

        public ActionState Execute(ICommandAction command)
        {
            var build = (BuildTowerCommand)command;
            if (pending != build)
            {
                if (!build.CanBuild())
                {
                    if (manager.ShouldPresentFor(build.Player)) manager.NotifyUser(build.FailureReason);
                    return ActionState.Failed;
                }
                if (!build.Player.IsHuman)
                {
                    var aiState = build.Execute();
                    if (aiState == ActionState.Succeeded)
                        manager.GetComponent<ArmyManager>().RefreshTowerOwnershipVisuals(true);
                    return aiState;
                }
                pending = build;
                prompt = GameObject.FindGameObjectWithTag("YesNoBox").GetComponent<YesNoBox>();
                manager.InputManager.SetInputMode(InputMode.UI);
                prompt.Ask($"Build a tower for {BuildTowerCommand.BuildCost} gp?\nYou have {build.Player.Gold} gp.");
                return ActionState.InProgress;
            }

            if (!prompt.Answer.HasValue && !prompt.Cancelled) return ActionState.InProgress;
            bool accepted = prompt.Answer == true && !prompt.Cancelled;
            var result = accepted ? build.Execute() : ActionState.Failed;
            if (result == ActionState.Succeeded)
                manager.GetComponent<ArmyManager>().RefreshTowerOwnershipVisuals(true);
            else if (accepted) manager.NotifyUser(build.FailureReason);
            pending = null;
            manager.InputManager.SetInputMode(InputMode.Game);
            manager.InputManager.SkipInput();
            return result;
        }
    }
}
