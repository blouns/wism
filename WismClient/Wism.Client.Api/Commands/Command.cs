using System;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Api.Commands
{
    public abstract class Command : ICommandAction
    {
        public Command()
        {
        }

        public Command(Player player)
        {
            this.Player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public int Id { get; set; }

        public Player Player { get; set; }

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