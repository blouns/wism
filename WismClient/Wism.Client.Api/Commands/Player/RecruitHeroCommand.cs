using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.Modules;

namespace Wism.Client.Api.Commands
{
    public class RecruitHeroCommand : Command
    {
        private readonly PlayerController playerController;

        public RecruitHeroCommand(PlayerController playerController, Player player)
            : base(player)
        {
            this.playerController = playerController ?? throw new ArgumentNullException(nameof(playerController));
        }

        public Tile HeroTile { get; set; }

        public string HeroDisplayName { get; set; }

        public int HeroPrice { get; set; }

        public bool? HeroAccepted { get; set; }

        public List<ArmyInfo> HeroAllies { get; set; }

        protected override ActionState ExecuteInternal()
        {
            ActionState state;

            if (this.Player.IsDead)
            {
                return ActionState.Failed;
            }

            if (this.playerController.RecruitHero(this.Player,
                    out var name, out var city, out var price, out var allies))
            {
                this.HeroTile = city.Tile;
                this.HeroDisplayName = name;
                this.HeroPrice = price;
                this.HeroAllies = allies;
                state = ActionState.Succeeded;
            }
            else
            {
                state = ActionState.Failed;
            }

            return state;
        }

        public override string ToString()
        {
            return $"Command: {this.Player.Clan} recruiting a hero";
        }
    }
}