using Wism.Client.MapObjects;

namespace Wism.Client.Agent.Commands
{
    public abstract class Command
    {
        public int Id { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public Army Army { get; set; }
    }
}
