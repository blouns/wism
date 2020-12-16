using Wism.Client.Core.Controllers;

namespace Wism.Client.Agent.Commands
{
    public interface ICommandAction
    {
        ActionState Execute();
    }
}
