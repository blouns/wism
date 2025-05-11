using System;
using Wism.Client.Api.CommandPublisher;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Controllers;

public abstract class InstrumentedProcessor : ICommandProcessor
{
    private readonly CommandIpcPublisher publisher;

    protected InstrumentedProcessor(CommandIpcPublisher publisher)
    {
        this.publisher = publisher;
    }

    public abstract bool CanExecute(ICommandAction command);

    public abstract ActionState ExecuteInternal(ICommandAction command);

    public ActionState Execute(ICommandAction command)
    {
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
}
