using System;
using System.Collections.Generic;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Api.Commands
{
    public class RecruitHeroCommand : Command
    {
        private readonly PlayerController playerController;

        public Tile HeroTile { get; set; }

        public string HeroDisplayName { get; set; }

        public int HeroPrice { get; set; }

        public bool? HeroAccepted { get; set; }

        public List<ArmyInfo> HeroAllies { get; set; }

        public RecruitHeroCommand(PlayerController playerController, Player player)
            : base(player)
        {
            this.playerController = playerController ?? throw new ArgumentNullException(nameof(playerController));
        }

        protected override ActionState ExecuteInternal()
        {
            ActionState state;

            if (Player.IsDead)
            {
                return ActionState.Failed;
            }

            if (this.playerController.RecruitHero(Player, 
                out string name, out City city, out int price, out List<ArmyInfo> allies))
            {
                HeroTile = city.Tile;
                HeroDisplayName = name;
                HeroPrice = price;
                HeroAllies = allies;
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
            return $"Command: {Player.Clan} recruiting a hero";
        }        
    }
}
