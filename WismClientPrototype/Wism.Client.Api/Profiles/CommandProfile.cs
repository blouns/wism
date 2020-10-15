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
            CreateMap<Command, CommandModel>()
                .Include<ArmyAttackCommand, ArmyAttackCommandModel>()
                .Include<ArmyMoveCommand, ArmyMoveCommandModel>();

            CreateMap<ArmyAttackCommand, ArmyAttackCommandModel>();
            CreateMap<ArmyMoveCommand, ArmyMoveCommandModel>();

            CreateMap<CommandModel, Command>()
                .Include<ArmyAttackCommandModel, ArmyAttackCommand>()
                .Include<ArmyMoveCommandModel, ArmyMoveCommand>();

            CreateMap<ArmyAttackCommandModel, ArmyAttackCommand>();
            CreateMap<ArmyMoveCommandModel, ArmyMoveCommand>();            
        }
    }
}
