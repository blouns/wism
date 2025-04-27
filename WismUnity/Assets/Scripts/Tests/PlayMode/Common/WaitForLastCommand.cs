using System;
using UnityEngine;
using Wism.Client.Commands;
using Wism.Client.Controllers;

namespace Assets.Tests.PlayMode
{
    public class WaitForLastCommand : CustomYieldInstruction
    {
        private Command command;

        public WaitForLastCommand(ControllerProvider provider)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            this.command = provider.CommandController.GetLastCommand();
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
