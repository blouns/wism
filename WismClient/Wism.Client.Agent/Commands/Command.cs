using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.Commands
{
    public abstract class Command : IAction
    {
        private Player player;

        public int Id { get; set; }

        public Player Player { get => player; }

        public List<Army> Armies { get; set; }

        public Command(List<Army> armies)
        {
            Armies = armies ?? throw new System.ArgumentNullException(nameof(armies));
            this.player = Armies[0].Player;
        }

        public abstract bool Execute();
    }
}
