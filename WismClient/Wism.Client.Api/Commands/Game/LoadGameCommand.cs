﻿using System;
using Wism.Client.Core.Controllers;
using Wism.Client.Entities;

namespace Wism.Client.Api.Commands
{
    public class LoadGameCommand : Command
    {
        public GameController GameController { get; }

        public GameEntity Snapshot { get; }

        public LoadGameCommand(GameController gameController, GameEntity snapshot)
        {
            if (gameController is null)
            {
                throw new ArgumentNullException(nameof(gameController));
            }

            GameController = gameController;
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        protected override ActionState ExecuteInternal()
        {
            return GameController.LoadSnapshot(Snapshot);
        }
    }
}