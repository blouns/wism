namespace Wism.Client.Data.Entities.Game
{
    public class NewGameCommandEntity : CommandEntity
    {
        public GameEntity Settings { get; set; }
    }
}