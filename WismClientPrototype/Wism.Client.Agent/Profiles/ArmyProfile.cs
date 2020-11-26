using AutoMapper;
using BranallyGames.Wism;
using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Model;

namespace Wism.Client.Agent.Profiles
{
    public class ArmyProfile : Profile
    {
        public ArmyProfile()
        {
            CreateMap<BranallyGames.Wism.Army, ArmyDto>()
                .ForMember(dest => dest.X, act => act.MapFrom(src => src.GetCoordinates().X))
                .ForMember(dest => dest.Y, act => act.MapFrom(src => src.GetCoordinates().Y));

            CreateMap<ArmyDto, BranallyGames.Wism.Army>()
                .ForMember(dest => dest.Tile, act => act.MapFrom(src => World.Current.Map[src.X, src.Y]));

            CreateMap<ArmyDto, Wism.Client.Data.Entities.Army>()
                .ForMember(dest => dest.Name, act => act.MapFrom(src => src.ShortName));
            CreateMap<Wism.Client.Data.Entities.Army, ArmyDto>()
                .ForMember(dest => dest.ShortName, act => act.MapFrom(src => src.Name));
        }
    }
}
