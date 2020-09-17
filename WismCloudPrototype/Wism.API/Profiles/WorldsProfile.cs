using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BranallyGames.Wism.API.Model;
using BranallyGames.Wism.Repository.Entities;

namespace BranallyGames.Wism.API.Profiles
{
    public class WorldsProfile : Profile
    {
        public WorldsProfile()
        {
            CreateMap<World, WorldModel>();
            CreateMap<WorldForCreationModel, World>();
            CreateMap<WorldForUpdateModel, World>();
        }
    }
}
