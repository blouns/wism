using System;
using Wism.Client.Model;
using Wism.Client.Model.Commands;

namespace Wism.Client.Api.Commands
{
    public class MoveCommand : Command
    {
        private readonly ArmyDto army;

        private readonly ArmyMoveCommandDto moveModel;

        public MoveCommand(int armyId, int x, int y)
        {
            throw new NotImplementedException();   
        }

        public override CommandResult Execute()
        {
            throw new NotImplementedException();
            //// Preserve previous state
            //CommandResult result = new CommandResult();            

            //// Execute action
            //if (!this.army.TryMove(x, y))
            //    throw new InvalidOperationException();

            //result.ExecutedTime = DateTime.UtcNow;

            //return result;
        }

        public override void Undo()
        {
            throw new NotImplementedException();
            //if (this.army == null)
            //    throw new InvalidOperationException("Cannot undo as this never executed.");

            //// Execute action
            //if (!this.army.TryMove(xBefore.Value, yBefore.Value))
            //    throw new InvalidOperationException();
        }
    }
}
