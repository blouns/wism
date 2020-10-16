using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Data.Entities;
using Wism.Client.Model.Commands;

namespace Wism.Client.Api.Profiles
{
    public class CommandProfile : Profile
    {
        public CommandProfile()
        {
            CreateMap<Command, CommandDto>()
                .Include<ArmyAttackCommand, ArmyAttackCommandDto>()
                .Include<ArmyMoveCommand, ArmyMoveCommandDto>();

            CreateMap<ArmyAttackCommand, ArmyAttackCommandDto>();
            CreateMap<ArmyMoveCommand, ArmyMoveCommandDto>();

            CreateMap<CommandDto, Command>()
                .Include<ArmyAttackCommandDto, ArmyAttackCommand>()
                .Include<ArmyMoveCommandDto, ArmyMoveCommand>();

            CreateMap<ArmyAttackCommandDto, ArmyAttackCommand>();
            CreateMap<ArmyMoveCommandDto, ArmyMoveCommand>();            
        }
    }
}
