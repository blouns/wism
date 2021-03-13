using Wism.Client.Entities;

namespace Wism.Client.Api.Data.Entities
{
    public class LoadGameCommandEntity : CommandEntity
    {
        public GameEntity Snapshot { get; set; }
    }
}
