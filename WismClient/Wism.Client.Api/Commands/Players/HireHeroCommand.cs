using System;
using System.Collections.Generic;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Modules.Infos;

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
    }
}