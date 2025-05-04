using System;

public interface IEventBus
{
    void Publish(CommandExecutedEvent @event);
}

public class EventBus : IEventBus
{
    public event EventHandler<CommandExecutedEvent> CommandExecuted;
    public void Publish(CommandExecutedEvent @event) =>
        CommandExecuted?.Invoke(this, @event);
}