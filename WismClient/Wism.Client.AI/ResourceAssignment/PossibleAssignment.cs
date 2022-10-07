namespace Wism.Client.AI.ResourceAssignment
{
    public class PossibleAssignment
    {
        public int Distance { get; set; }

        public int Score { get; set; }

        public TaskableObject PossibleTaskDoer { get; set; }
    }
}
