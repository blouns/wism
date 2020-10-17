using AutoMapper;
using BranallyGames.Wism;
using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Model;

namespace Wism.Client.Api.Profiles
{
    public class ArmyProfile : Profile
    {
        public ArmyProfile()
        {
            CreateMap<Army, ArmyDto>();
        }
    }
}
