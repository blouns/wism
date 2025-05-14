using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Commands.Armies
{
    public class MoveOnceCommand : ArmyCommand
    {
        private IList<Tile> path;

        public MoveOnceCommand(ArmyController armyController, List<MapObjects.Army> armies, int x, int y)
            : base(armyController, armies)
        {
            this.X = x;
            this.Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }

        public IList<Tile> Path
        {
            get => this.path;
            set => this.path = value;
        }

        protected override ActionState ExecuteInternal()
        {
            return this.ArmyController.MoveOneStep(this.Armies, World.Current.Map[this.X, this.Y], ref this.path,
                out _);
        }

        public override string ToString()
        {
            return $"Command: {ArmyUtilities.ArmiesToString(this.Armies)} move to ({World.Current.Map[this.X, this.Y]}";
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            var mover = Armies.FirstOrDefault();
            var current = mover?.Tile;
            var destination = World.Current?.Map[this.X, this.Y];

            return new CommandExecutedEvent
            {
                CommandType = nameof(MoveOnceCommand),
                ActorId = mover?.ShortName ?? "Unknown",
                TargetId = destination?.ToString(),
                TargetPosition = destination != null
                    ? new PositionDto { X = destination.X, Y = destination.Y }
                    : null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "FromX", current?.X ?? -1 },
                    { "FromY", current?.Y ?? -1 },
                    { "Terrain", destination?.Terrain?.ToString() ?? "Unknown" },
                    { "ArmySize", Armies.Count }
                }
            };
        }
    }
}