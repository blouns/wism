namespace Wism.Companion.Shared.Models
{
    public class ArmyDto
    {
        public string Name { get; set; }
        public string Owner { get; set; }
        public int Health { get; set; }
        public PositionDto Position { get; set; }
        public bool IsHero { get; set; }  // Optional flag to distinguish
    }
}
