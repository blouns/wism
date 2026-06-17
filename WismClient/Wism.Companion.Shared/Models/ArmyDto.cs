namespace Wism.Companion.Shared.Models
{
    public class ArmyDto
    {
        public string Name { get; set; }
        public string? UnitType { get; set; }
        public string Owner { get; set; }
        public int Health { get; set; }
        public int Strength { get; set; }
        public int Moves { get; set; }
        public bool IsHero { get; set; }
        public bool IsSpecial { get; set; }
        public bool CanFly { get; set; }
        public PositionDto Position { get; set; }
    }
}
