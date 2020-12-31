using System;
using Wism.Client.Data.Entities;
using Wism.Client.Model.Commands;

namespace Wism.Client.Agent.Profiles
{
    public static class ArmyCommandFactory
    {
        public static ArmyCommand CreateFrom(CommandDto commandDto)
        {
            ArmyCommand armyCommand = null;

            if (commandDto.GetType().IsAssignableFrom(typeof(HireHeroCommandDto)))
            {
                armyCommand = new ArmyCommand()
                {
                    ArmyId = ((HireHeroCommandDto)commandDto).Army.Guid,
                    CommandId = commandDto.Id
                };
            }
            else if (commandDto.GetType().IsAssignableFrom(typeof(ConscriptArmyCommandDto)))
            {
                armyCommand = new ArmyCommand()
                {
                    ArmyId = ((ConscriptArmyCommandDto)commandDto).Army.Guid,
                    CommandId = commandDto.Id
                };
            }
            else if (commandDto.GetType().IsAssignableFrom(typeof(MoveCommandDto)))
            {
                armyCommand = new ArmyCommand()
                {
                    ArmyId = ((MoveCommandDto)commandDto).Army.Guid,
                    CommandId = commandDto.Id
                };
            }
            else if (commandDto.GetType().IsAssignableFrom(typeof(AttackCommandDto)))
            {
                armyCommand = new ArmyCommand()
                {
                    ArmyId = ((AttackCommandDto)commandDto).Army.Guid,
                    CommandId = commandDto.Id
                };
            }

            if (armyCommand == null)
            {
                throw new ArgumentException("Command type not found.", nameof(commandDto));
            }

            return armyCommand;
        }
    }
}
