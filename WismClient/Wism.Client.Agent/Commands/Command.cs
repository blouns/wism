using Wism.Client.Core;

namespace Wism.Client.Agent.Commands
{
    public abstract class Command : IAction
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
