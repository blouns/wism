using System;
using Wism.Client.Model;

namespace Wism.Client.Agent.Commands
{
    public class AttackAction : IAction
    {
        private readonly ArmyDto army;
        private int? hitPointsBefore;
        private int x;
        private int y;

        public AttackAction(ArmyDto army, int x, int y)
        {
            this.army = army?? throw new ArgumentNullException(nameof(army));
            this.x = x;
            this.y = y;
        }

        public void Execute()
        {
            hitPointsBefore = army.HitPoints;
            
        }

        public void Undo()
        {
            if (hitPointsBefore == null)
                throw new InvalidOperationException("This command has not yet been executed.");

            army.HitPoints = hitPointsBefore.Value;
        }
    }
}
