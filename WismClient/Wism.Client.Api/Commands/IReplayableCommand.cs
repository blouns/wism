using Wism.Client.Controllers;
using Wism.Companion.Shared.Events;

namespace Wism.Client.Commands
{
    public interface IReplayableCommand
    {
        CommandExecutedEvent ToExecutedEvent(ActionState result);
    }

}