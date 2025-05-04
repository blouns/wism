using System;
using Wism.Client.Commands;
using Wism.Client.Controllers;

public class CommandExecutedEvent : EventArgs
{
    public Command Command { get; }
    public ActionState Result { get; }
    public DateTime Timestamp { get; }

    public CommandExecutedEvent(Command command, ActionState result)
    {
        this.Command = command ?? throw new ArgumentNullException(nameof(command));
        this.Timestamp = DateTime.Now;
    }
}