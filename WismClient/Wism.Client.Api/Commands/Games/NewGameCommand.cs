using System;
using Wism.Client.Controllers;
using Wism.Client.Data.Entities;

namespace Wism.Client.Commands.Games
{
    public class NewGameCommand : Command
    {
        public NewGameCommand(GameController gameController, GameEntity settings)
        {
            this.GameController = gameController ?? throw new ArgumentNullException(nameof(gameController));
            this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public GameController GameController { get; }

        public GameEntity Settings { get; }

        protected override ActionState ExecuteInternal()
        {
            return this.GameController.NewGame(this.Settings);
        }
    }
}