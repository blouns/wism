using Assets.Scripts.Managers;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Armies;
using Wism.Client.Common;
using Wism.Client.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public class BattleProcessor : ICommandProcessor
    {
        private readonly IWismLogger logger;
        private readonly UnityManager unityGame;

        public BattleProcessor(IWismLoggerFactory loggerFactory, UnityManager unityGame)
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
            var attack = (AttackOnceCommand)command;
            var targetTile = Wism.Client.Core.World.Current.Map[attack.X, attack.Y];
            var defendingPlayer = BattlePresentation.ResolveDefendingPlayer(targetTile, attack.OriginalDefendingArmies);
            var presentedBattle = BattlePresentation.ShouldPresent(attack.Player, defendingPlayer) ||
                                  this.unityGame.WarPanel.gameObject.activeSelf;
            ActionState result = command.Execute();

            if (presentedBattle)
            {
                this.unityGame.WarPanel.UpdateBattle(this.unityGame.CurrentAttackers, this.unityGame.CurrentDefenders);
            }

            return result;
        }
    }
}
