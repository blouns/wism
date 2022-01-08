using Wism.Client.Entities;

namespace Wism.Client.Api.Data.Entities
{
    public class NewGameCommandEntity : CommandEntity
    {
        public GameEntity Settings { get; set; }
    }
}