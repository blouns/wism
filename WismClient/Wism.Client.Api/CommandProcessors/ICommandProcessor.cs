using Wism.Client.Api.Commands;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Api.CommandProcessors
{
    public interface ICommandProcessor
    {
        bool CanExecute(ICommandAction command);

        ActionState Execute(ICommandAction command);
    }
}
