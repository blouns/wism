using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Common;
using Wism.Client.Comparers;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Commands.Armies
{
    public class PrepareForBattleCommand : ArmyCommand
    {
        public PrepareForBattleCommand(ArmyController armyController, List<MapObjects.Army> armies, int x, int y)
            : base(armyController, armies)
        {
            this.X = x;
            this.Y = y;
            this.Defenders = World.Current.Map[this.X, this.Y].MusterArmy();
            this.Defenders.Sort(new ByArmyBattleOrder(World.Current.Map[this.X, this.Y]));
            armies.Sort(new ByArmyBattleOrder(World.Current.Map[this.X, this.Y]));
        }

        public int X { get; set; }
        public int Y { get; set; }

        public List<MapObjects.Army> Defenders { get; set; }

        protected override ActionState ExecuteInternal()
        {
            return this.ArmyController.PrepareForBattle();
        }

        public override string ToString()
        {
            return $"Command: Prepare for battle of {ArmyUtilities.ArmiesToString(this.Armies)} against " +
                   $"{World.Current.Map[this.X, this.Y]}";
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            var attacker = Armies.FirstOrDefault();
            var tile = World.Current.Map[this.X, this.Y];

            return new CommandExecutedEvent
            {
                CommandType = nameof(PrepareForBattleCommand),
                ActorId = attacker?.ShortName ?? "Unknown",
                TargetId = Defenders.FirstOrDefault()?.ShortName,
                TargetPosition = new PositionDto { X = this.X, Y = this.Y },
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "Attackers", Armies.Count },
                    { "Defenders", Defenders.Count },
                    { "Terrain", tile?.Terrain?.ToString() ?? "Unknown" },
                    { "Tile", tile?.ToString() ?? $"({this.X},{this.Y})" }
                }
            };
        }
    }
}