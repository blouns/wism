using System;
using Wism.Client.Core.Controllers;
using Wism.Client.Entities;

namespace Wism.Client.Api.Commands
{
    public class NewGameCommand : Command
    {
        public GameController GameController { get; }

        public GameEntity Settings { get; }

        public NewGameCommand(GameController gameController, GameEntity settings)
        {
            this.GameController = gameController ?? throw new ArgumentNullException(nameof(gameController));
            this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        protected override ActionState ExecuteInternal()
        {
            return this.GameController.NewGame(this.Settings);
        }
    }
}