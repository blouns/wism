using System;
using Wism.Client.Api.CommandPublisher;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Controllers;

public abstract class InstrumentedProcessor : ICommandProcessor
{
    private readonly CommandIpcPublisher publisher;
    private bool isHuman;

    public bool IsHuman { get => isHuman; set => isHuman = value; }

    protected InstrumentedProcessor(CommandIpcPublisher publisher)
    {
        this.publisher = publisher;
    }

    public abstract bool CanExecute(ICommandAction command);

    public abstract ActionState ExecuteInternal(ICommandAction command);

    public ActionState Execute(ICommandAction command)
    {
        this.IsHuman = IsPlayerHuman(command as Command);

        var result = ExecuteInternal(command);

        if (command is IReplayableCommand replayable)
        {
            try
            {
                var evt = replayable.ToExecutedEvent(result);
                publisher.Publish(evt);
            }
            catch (Exception ex)
            {
                // Log or swallow gracefully
            }
        }

        return result;
    }

    private static bool IsPlayerHuman(Command command)
    {
        if (command == null)
        {
            return true;
        }

        return command.Player.IsHuman;
    }
}
