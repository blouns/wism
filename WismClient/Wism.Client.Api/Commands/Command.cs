using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Api.Commands
{
    public abstract class Command : ICommandAction
    {
        public int Id { get; set; }

        public Player Player { get; set; }

        public ActionState Result { get; private set; }

        public Command()
        {
        }

        public Command(Player player)
        {
            Player = player ?? throw new System.ArgumentNullException(nameof(player));
        }

        public ActionState Execute()
        {
            this.Result = ExecuteInternal();

            return this.Result;
        }

        protected abstract ActionState ExecuteInternal();

        protected bool PlayerIsAlive()
        {
            return (Player == null || Player.IsDead);
        }
    }
}
