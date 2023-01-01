using Wism.Client.Core.Controllers;

namespace Wism.Client.Api.Commands
{
    public interface ICommandAction
    {
        ActionState Execute();
    }
}