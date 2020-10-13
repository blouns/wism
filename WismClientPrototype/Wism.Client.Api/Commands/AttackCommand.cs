using System;
using Wism.Client.Model;

namespace Wism.Client.Api.Commands
{
    public class AttackCommand : Command
    {
        private readonly ArmyModel army;
        private int? hitPointsBefore;
        private int x;
        private int y;

        public AttackCommand(ArmyModel army, int x, int y)
        {
            this.army = army?? throw new ArgumentNullException(nameof(army));
            this.x = x;
            this.y = y;
        }

        public override CommandResult Execute()
        {
            hitPointsBefore = army.HitPoints;
            army.Attack(x, y);

            throw new NotImplementedException();
        }

        public override void Undo()
        {
            if (hitPointsBefore == null)
                throw new InvalidOperationException("This command has not yet been executed.");

            army.HitPoints = hitPointsBefore.Value;
        }
    }
}
