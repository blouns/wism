using System;

namespace Wism.Client.Api.Commands
{
    public class CommandResultArgs : EventArgs
    {
        public Command Command { get; }

        public CommandResultArgs(Command command)
        {
            Command = command;
        }
    }
}
