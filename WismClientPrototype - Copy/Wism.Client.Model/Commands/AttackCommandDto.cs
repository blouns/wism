namespace Wism.Client.Model.Commands
{
    public class AttackCommandDto : CommandDto
    {
        public int X { get; set; }

        public int Y { get; set; }

        public ArmyDto Army { get; set; }
    }
}
