using Assets.Scripts.Managers;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class BattleProcessor : ICommandProcessor
    {
        private readonly ILogger logger;
        private readonly UnityManager unityGame;

        public BattleProcessor(ILoggerFactory loggerFactory, UnityManager unityGame)
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
            return command is AttackOnceCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            ActionState result = command.Execute();

            unityGame.WarPanel.UpdateBattle(unityGame.CurrentAttackers, unityGame.CurrentDefenders);

            return result;
        }
    }
}
