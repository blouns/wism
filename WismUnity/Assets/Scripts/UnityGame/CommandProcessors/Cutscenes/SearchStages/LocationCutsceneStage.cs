using System.Collections.Generic;
using Wism.Client.Commands;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.CommandProcessors
{

    public abstract class LocationCutsceneStage : CutsceneStage
    {
        public Tile TargetTile { get; private set; }
        public Player Player { get; private set; }
        public Location Location { get; private set; }
        public List<Army> Armies { get; private set; }
        public Army Hero { get; private set; }

        public LocationCutsceneStage(SearchLocationCommand command)
            : base(command)
        {
            UnpackCommand(command);
        }

        private void UnpackCommand(SearchLocationCommand command)
        {
            this.TargetTile = World.Current.Map[command.Location.X, command.Location.Y];
            this.Player = command.Armies[0].Player;
            this.Location = this.TargetTile.Location;
            this.Armies = command.Armies;
            this.Hero = command.Armies.Find(a =>
                a is Hero &&
                a.Tile == this.TargetTile &&
                a.MovesRemaining > 0);
        }
    }
}
