﻿using System;
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
            if (gameController is null)
            {
                throw new ArgumentNullException(nameof(gameController));
            }

            GameController = gameController;
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        protected override ActionState ExecuteInternal()
        {
            return GameController.NewGame(Settings);
        }
    }
}