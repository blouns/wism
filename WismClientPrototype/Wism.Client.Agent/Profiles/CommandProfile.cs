using AutoMapper;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wism.Client.Data.Entities;
using Wism.Client.Model.Commands;

namespace Wism.Client.Agent.Profiles
{
    public class CommandProfile : Profile
    {
        public CommandProfile()
        {
            CreateMap<Command, CommandDto>()
                .Include<HireHeroCommand, HireHeroCommandDto>()
                .Include<ConscriptArmyCommand, ConscriptArmyCommandDto>()
                .Include<AttackCommand, AttackCommandDto>()
                .Include<MoveCommand, MoveCommandDto>();

            CreateMap<AttackCommand, AttackCommandDto>();
            CreateMap<MoveCommand, MoveCommandDto>();
            CreateMap<HireHeroCommand, HireHeroCommandDto>();
            CreateMap<ConscriptArmyCommand, ConscriptArmyCommandDto>();

            CreateMap<CommandDto, Command>()
                .ForMember(
                    entity => entity.ArmyCommands,
                    act => act.MapFrom(
                        dto => new List<ArmyCommand>()
                        {
                            ArmyCommandFactory.CreateFrom(dto)
                        }))
                .Include<HireHeroCommandDto, HireHeroCommand>()
                .Include<ConscriptArmyCommandDto, ConscriptArmyCommand>()
                .Include<AttackCommandDto, AttackCommand>()
                .Include<MoveCommandDto, MoveCommand>();           

            CreateMap<AttackCommandDto, AttackCommand>();
            CreateMap<MoveCommandDto, MoveCommand>();
            CreateMap<HireHeroCommandDto, HireHeroCommand>();
            CreateMap<ConscriptArmyCommandDto, ConscriptArmyCommand>();
        }
    }
}
