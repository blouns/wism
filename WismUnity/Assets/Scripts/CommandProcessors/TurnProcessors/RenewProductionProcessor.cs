using Assets.Scripts.Managers;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class RenewProductionProcessor : ICommandProcessor
    {
        private readonly ILogger logger;
        private readonly UnityManager unityGame;

        public RenewProductionProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
        {
            if (loggerFactory is null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.unityGame = unityGame ?? throw new System.ArgumentNullException(nameof(unityGame));
        }

        public bool CanExecute(ICommandAction command)
        {
            return command is ReviewProductionCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            // TODO: Show option to renew production
            //       For now just automatically renew
            return command.Execute();
        }
    }
}