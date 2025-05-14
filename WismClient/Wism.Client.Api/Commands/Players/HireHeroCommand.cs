using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Modules.Infos;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.Client.Commands.Players
{
    public class HireHeroCommand : Command
    {
        private readonly PlayerController playerController;

        public HireHeroCommand(PlayerController playerController, RecruitHeroCommand recruitHeroCommand)
            : base(recruitHeroCommand.Player)
        {
            this.playerController = playerController ?? throw new ArgumentNullException(nameof(playerController));
            this.RecruitHeroCommand = recruitHeroCommand ?? throw new ArgumentNullException(nameof(recruitHeroCommand));
        }

        public MapObjects.Hero Hero { get; set; }

        public RecruitHeroCommand RecruitHeroCommand { get; set; }

        public bool HeroAccepted => this.RecruitHeroCommand.HeroAccepted.Value;

        public Tile HeroTile => this.RecruitHeroCommand.HeroTile;

        public string HeroDisplayName => this.RecruitHeroCommand.HeroDisplayName;

        public int HeroPrice => this.RecruitHeroCommand.HeroPrice;

        public List<ArmyInfo> HeroAllies => this.RecruitHeroCommand.HeroAllies;

        protected override ActionState ExecuteInternal()
        {
            var state = ActionState.Failed;

            if (!this.RecruitHeroCommand.HeroAccepted.HasValue)
            {
                throw new InvalidOperationException("Hero has not been accepted or rejected.");
            }

            // If hero accepted, hire; otherwise skip
            if (this.HeroAccepted)
            {
                var success = this.playerController.TryHireHero(this.Player,
                    this.HeroTile,
                    this.HeroDisplayName,
                    this.HeroPrice,
                    out var hero);

                if (success)
                {
                    this.Hero = hero;
                    state = ActionState.Succeeded;
                }
                else
                {
                    state = ActionState.Failed;
                }
            }

            return state;
        }

        public override string ToString()
        {
            return $"Command: {this.Player.Clan} hiring hero";
        }

        public override CommandExecutedEvent ToExecutedEvent(ActionState result)
        {
            return new CommandExecutedEvent
            {
                CommandType = nameof(HireHeroCommand),
                ActorId = Player?.Clan?.ShortName ?? "Unknown",
                TargetId = Hero?.ShortName ?? HeroDisplayName ?? "UnknownHero",
                TargetPosition = HeroTile != null
                    ? new PositionDto { X = HeroTile.X, Y = HeroTile.Y }
                    : null,
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "HeroName", HeroDisplayName ?? "Unknown" },
                    { "HeroAccepted", HeroAccepted },
                    { "HeroPrice", HeroPrice },
                    { "HeroAllies", string.Join(", ", HeroAllies.Select(a => a.ShortName)) },
                    { "Tile", HeroTile?.ToString() ?? "Unknown" }
                }
            };
        }
    }
}