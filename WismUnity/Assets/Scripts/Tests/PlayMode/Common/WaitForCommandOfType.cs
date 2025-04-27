using System;
using UnityEngine;
using Wism.Client.Controllers;

namespace Assets.Tests.PlayMode
{
    public class WaitForCommandOfType<T> : CustomYieldInstruction
    {
        private readonly ControllerProvider provider;
        private readonly bool waitForCompletion;
        private int lastCommandIdSeen;

        public WaitForCommandOfType(ControllerProvider provider, bool waitForCompletion = false, int lastId = 0)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            this.provider = provider;
            this.waitForCompletion = waitForCompletion;
            this.lastCommandIdSeen = lastId;
        }

        public override bool keepWaiting
        {
            get
            {
                var newCommands = this.provider.CommandController.GetCommandsAfterId(this.lastCommandIdSeen);
                foreach (var command in newCommands)
                {
                    if (command.GetType() == typeof(T))
                    {
                        if (this.waitForCompletion)
                        {
                            return (command.Result == ActionState.NotStarted) ||
                                   (command.Result == ActionState.InProgress);
                        }
                        else
                        {
                            return false;
                        }
                    }

                    this.lastCommandIdSeen = command.Id;
                }

                return true;
            }
        }
    }
}
