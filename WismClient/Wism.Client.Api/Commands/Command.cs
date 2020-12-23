using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Api.Commands
{
    public abstract class Command : ICommandAction
    {
        public int Id { get; set; }

        public Player Player { get; set; }

        public Command()
        {
        }

        public Command(Player player)
        {
            Player = player ?? throw new System.ArgumentNullException(nameof(player));
        }

        public abstract ActionState Execute();
    }
}
