using UnityEngine;
using Wism.Client.Commands;
using Wism.Client.Core.Controllers;

namespace Assets.Tests.PlayMode
{
    public class WaitForCommand : CustomYieldInstruction
    {
        private Command command;

        public WaitForCommand(Command command)
        {
            this.command = command ?? throw new System.ArgumentNullException(nameof(command));
        }

        public override bool keepWaiting
        {
            get
            {
                return (this.command.Result == ActionState.NotStarted) ||
                       (this.command.Result == ActionState.InProgress);

            }
        }
    }
}
