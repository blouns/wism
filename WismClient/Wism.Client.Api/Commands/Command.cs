using System;
using Wism.Client.Controllers;
using Wism.Companion.Shared.Events;

namespace Wism.Client.Commands
{
    public abstract class Command : ICommandAction, IReplayableCommand
    {   
        public Command()
        {
        }

        public Command(Core.Player player)
        {
            this.Player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public int Id { get; set; }

        public Core.Player Player { get; set; }

        public ActionState Result { get; private set; }

        public ActionState Execute()
        {
            this.Result = this.ExecuteInternal();            

            return this.Result;
        }

        protected abstract ActionState ExecuteInternal();

        protected bool PlayerIsAlive()
        {
            return this.Player == null || this.Player.IsDead;
        }

        public virtual CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            return new CommandExecutedEvent
            {
                CommandType = GetType().Name,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow
            };
        }

    }
}