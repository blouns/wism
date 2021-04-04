using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Api.Commands;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.CommandProcessors
{

    public abstract class LocationCutsceneStage : CutsceneStage
    {
        public Tile TargetTile { get; private set; }
        public Player Player { get; private set; }
        public Location Location { get; private set; }
        public List<Army> Armies { get; private set;  }
        public Army Hero { get; private set; }

        public LocationCutsceneStage(SearchLocationCommand command)
            : base(command)
        {
            UnpackCommand(command);
        }

        private void UnpackCommand(SearchLocationCommand command)
        {
            TargetTile = World.Current.Map[command.Location.X, command.Location.Y];
            Player = command.Armies[0].Player;
            Location = TargetTile.Location;
            Armies = command.Armies;
            Hero = command.Armies.Find(a =>
                a is Hero &&
                a.Tile == TargetTile &&
                a.MovesRemaining > 0);
        }
    }
}
