using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Api.Commands;

namespace Wism.Actions
{

    public class ActionWorker
    {
        public Queue<Command> CommandQueue { get; } = new Queue<Command>();

        public event EventHandler<CommandResultArgs> CommandExecuted;

        public void ExecuteCommand()
        {
            Command command = CommandQueue.Dequeue();
            
            if (command == null) 
                return;

            command.Execute();
            //CommandExecuted(this, new CommandResultArgs() { Command = command }); 
        }
    }
}
