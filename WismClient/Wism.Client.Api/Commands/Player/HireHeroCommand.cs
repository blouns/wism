using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Api.Commands
{
    public class HireHeroCommand : Command
    {
        private readonly PlayerController playerController;

        public Hero Hero { get; set; }

        public RecruitHeroCommand RecruitHeroCommand { get; set; }

        public bool HeroAccepted
        {
            get => this.RecruitHeroCommand.HeroAccepted.Value;
        }

        public Tile HeroTile
        {
            get => this.RecruitHeroCommand.HeroTile;
        }

        public string HeroDisplayName
        {
            get => this.RecruitHeroCommand.HeroDisplayName;
        }

        public int HeroPrice
        {
            get => this.RecruitHeroCommand.HeroPrice;
        }

        public List<ArmyInfo> HeroAllies
        {
            get => this.RecruitHeroCommand.HeroAllies;
        }

        public HireHeroCommand(PlayerController playerController, RecruitHeroCommand recruitHeroCommand)
            : base(recruitHeroCommand.Player)
        {
            this.playerController = playerController ?? throw new ArgumentNullException(nameof(playerController));
            this.RecruitHeroCommand = recruitHeroCommand ?? throw new ArgumentNullException(nameof(recruitHeroCommand));
        }

        protected override ActionState ExecuteInternal()
        {
            ActionState state = ActionState.Failed;

            if (!this.RecruitHeroCommand.HeroAccepted.HasValue)
            {
                throw new InvalidOperationException("Hero has not been accepted or rejected.");
            }

            // If hero accepted, hire; otherwise skip
            if (this.HeroAccepted)
            {
                bool success = this.playerController.TryHireHero(this.Player,
                    this.HeroTile,
                    this.HeroDisplayName,
                    this.HeroPrice,
                    out Hero hero);

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
