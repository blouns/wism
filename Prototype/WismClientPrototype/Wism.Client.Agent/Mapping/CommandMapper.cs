using System;
using System.Collections.Generic;
using System.Linq;
using BranallyGames.Wism;
using Wism.Client.Data.Entities;
using Wism.Client.Model;
using Wism.Client.Model.Commands;
using DataArmy = Wism.Client.Data.Entities.Army;
using GameArmy = BranallyGames.Wism.Army;

namespace Wism.Client.Agent.Mapping
{
    public sealed class CommandMapper
    {
        public Command ToEntity(CommandDto dto)
        {
            if (dto is null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            Command command = dto switch
            {
                MoveCommandDto _ => new MoveCommand(),
                AttackCommandDto _ => new AttackCommand(),
                ConscriptArmyCommandDto _ => new ConscriptArmyCommand(),
                HireHeroCommandDto _ => new HireHeroCommand(),
                _ => throw new ArgumentException($"Unsupported command DTO type {dto.GetType().Name}.", nameof(dto))
            };

            command.Id = dto.Id;
            if (dto is MoveCommandDto move)
            {
                command.X = move.X;
                command.Y = move.Y;
                AddArmyCommand(command, move.Army);
            }
            else if (dto is AttackCommandDto attack)
            {
                command.X = attack.X;
                command.Y = attack.Y;
                AddArmyCommand(command, attack.Army);
            }
            else if (dto is ConscriptArmyCommandDto conscript)
            {
                AddArmyCommand(command, conscript.Army);
            }
            else if (dto is HireHeroCommandDto hire)
            {
                command.X = hire.X;
                command.Y = hire.Y;
            }

            return command;
        }

        public CommandDto ToDto(Command entity)
        {
            if (entity is null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            CommandDto dto = entity switch
            {
                MoveCommand _ => new MoveCommandDto(),
                AttackCommand _ => new AttackCommandDto(),
                ConscriptArmyCommand _ => new ConscriptArmyCommandDto(),
                HireHeroCommand _ => new HireHeroCommandDto(),
                _ => throw new ArgumentException($"Unsupported command entity type {entity.GetType().Name}.", nameof(entity))
            };

            dto.Id = entity.Id;
            if (dto is MoveCommandDto move)
            {
                move.X = entity.X;
                move.Y = entity.Y;
            }
            else if (dto is AttackCommandDto attack)
            {
                attack.X = entity.X;
                attack.Y = entity.Y;
            }
            else if (dto is HireHeroCommandDto hire)
            {
                hire.X = entity.X;
                hire.Y = entity.Y;
            }

            return dto;
        }

        public IEnumerable<CommandDto> ToDtos(IEnumerable<Command> entities)
        {
            return entities.Select(ToDto);
        }

        public ArmyDto ToDto(GameArmy army)
        {
            if (army is null)
            {
                throw new ArgumentNullException(nameof(army));
            }

            var coordinates = army.GetCoordinates();
            return new ArmyDto
            {
                Guid = army.Guid,
                ShortName = army.ID,
                DisplayName = army.DisplayName,
                HitPoints = army.HitPoints,
                Strength = army.Strength,
                X = coordinates.X,
                Y = coordinates.Y
            };
        }

        public DataArmy ToEntity(ArmyDto dto)
        {
            if (dto is null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            return new DataArmy
            {
                Id = dto.Guid,
                Name = dto.ShortName,
                X = dto.X,
                Y = dto.Y,
                HitPoints = dto.HitPoints,
                Strength = dto.Strength
            };
        }

        private static void AddArmyCommand(Command command, ArmyDto army)
        {
            if (army is null || army.Guid == Guid.Empty)
            {
                return;
            }

            command.ArmyCommands.Add(new ArmyCommand
            {
                ArmyId = army.Guid,
                CommandId = command.Id
            });
        }
    }
}
