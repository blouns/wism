namespace Wism.Client.Data.Entities.Hero
{
    public class ItemsCommandEntity : CommandEntity
    {
        public bool Taking { get; set; }

        public int HeroId { get; set; }

        public string[] ItemShortNames { get; set; }
    }
}