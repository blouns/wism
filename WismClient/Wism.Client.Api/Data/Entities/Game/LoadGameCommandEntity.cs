namespace Wism.Client.Data.Entities.Game
{
    public class LoadGameCommandEntity : CommandEntity
    {
        public GameEntity Snapshot { get; set; }
    }
}