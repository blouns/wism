using System;
using Wism.Client.Controllers;
using Wism.Client.Data.Entities;

namespace Wism.Client.Commands.Game
{
    public class LoadGameCommand : Command
    {
        public LoadGameCommand(GameController gameController, GameEntity snapshot)
        {
            if (gameController is null)
            {
                throw new ArgumentNullException(nameof(gameController));
            }

            this.GameController = gameController;
            this.Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public GameController GameController { get; }

        public GameEntity Snapshot { get; }

        protected override ActionState ExecuteInternal()
        {
            return this.GameController.LoadSnapshot(this.Snapshot);
        }
    }
}