using System;
using System.Collections.Generic;
using Wism.Client.AI.Framework;
using Wism.Client.Commands;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Common;
using Wism.Client.CommandProviders;
using Wism.Client.Commands.Players;

namespace Wism.Client.AI.CommandProviders
{
    public class AdaptaCommandProvider : ICommandProvider
    {
        private readonly AiController aiController;
        private readonly ControllerProvider controllerProvider;
        private readonly IWismLogger logger;
        private readonly CommandController commandController;
        private readonly GameController gameController;
        private readonly List<ICommandAction> bufferedCommands = new List<ICommandAction>();

        public AdaptaCommandProvider(
                        IWismLogger logger,
                        AiController aiController,                        
                        ControllerProvider controllerProvider)
        {
            this.aiController = aiController ?? throw new ArgumentNullException(nameof(aiController));
            this.controllerProvider = controllerProvider ?? throw new ArgumentNullException(nameof(controllerProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.commandController = controllerProvider.CommandController;
            this.gameController = controllerProvider.GameController;
        }

        public void GenerateCommands()
        {
            logger.LogInformation("AdaptaCommandProvider: Generating AI commands.");
            bufferedCommands.Clear();

            var commands = aiController.ExecuteTurnAndReturnCommands(World.Current);
            if (commands != null && commands.Count > 0)
            {
                foreach (var command in commands)
                {
                    bufferedCommands.Add(command);
                    commandController.AddCommand((Command)command);
                }

                logger.LogInformation($"[Adapta] Queued {commands.Count} command(s).");
            }
            else
            {
                var endTurn = new EndTurnCommand(gameController, Game.Current.GetCurrentPlayer());
                bufferedCommands.Add(endTurn);
                commandController.AddCommand(endTurn);
                logger.LogInformation("[Adapta] No tactical commands. Queued EndTurnCommand.");
            }
        }

        public List<ICommandAction> GetBufferedCommands()
        {
            return bufferedCommands;
        }
    }
}
