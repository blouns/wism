using System;
using Wism.Client.Controllers;

namespace Wism.Client.Commands
{
    public abstract class Command : ICommandAction
    {
        private readonly IEventBus eventBus;

        protected Command(IEventBus eventBus) => this.eventBus = eventBus;

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
    }
}