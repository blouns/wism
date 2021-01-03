using AutoMapper;
using BranallyGames.Wism.API.Model;
using BranallyGames.Wism.Repository.Entities;

namespace BranallyGames.Wism.API.Profiles
{
    public class PlayersProfile : Profile
    {
        public PlayersProfile()
        {
            CreateMap<Player, PlayerModel>();
            CreateMap<PlayerForCreationModel, Player>();
            CreateMap<PlayerForUpdateModel, Player>();
            CreateMap<Player, PlayerForUpdateModel>();
        }        
    }
}
