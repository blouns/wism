using Wism.Client.Commands;
using Wism.Client.Controllers;

namespace Wism.Client.CommandProcessors
{
    public interface ICommandProcessor
    {
        bool CanExecute(ICommandAction command);

        ActionState Execute(ICommandAction command);
    }
}