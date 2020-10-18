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
            CreateMap<Army, ArmyDto>()
                .ForMember(dest => dest.X, act => act.MapFrom(src => src.GetCoordinates().X))
                .ForMember(dest => dest.Y, act => act.MapFrom(src => src.GetCoordinates().Y));
        }
    }
}
