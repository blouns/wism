using Wism.Client.Controllers;

namespace Wism.Client.Commands
{
    public interface ICommandAction
    {
        ActionState Execute();
    }
}