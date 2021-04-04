using System;
using System.Collections.Generic;
using System.Text;
using Wism.Client.Core;
using Wism.Client.Entities;
using Wism.Client.MapObjects;
using Wism.Client.Modules;

namespace Wism.Client.Factories
{
    public static class PlayerFactory
    {
        public static Player Load(PlayerEntity playerEntity,
            out Dictionary<string, Player> cityToPlayer,
            out Dictionary<string, Player> capitolToPlayer)
        {
            capitolToPlayer = new Dictionary<string, Player>();
            cityToPlayer = new Dictionary<string, Player>();

            // Player
            Clan clan = Clan.Create(
                ModFactory.FindClanInfo(playerEntity.ClanShortName));
            var player = Player.Create(clan);

            // Capitol (circular reference; add later)
            if (playerEntity.CapitolShortName != null)
            {
                capitolToPlayer.Add(playerEntity.CapitolShortName, player);
            }

            // My armies
            if (playerEntity.Armies != null)
            {
                foreach (var armyEntity in playerEntity.Armies)
                {
                    var army = ArmyFactory.Load(player, armyEntity);
                    if (armyEntity.IsHero)
                    {
                        var hero = (Hero)army;
                        player.AddHero(hero);

                        // Load items
                        if (armyEntity.Artifacts != null)
                        {
                            foreach (var artifactEntity in armyEntity.Artifacts)
                            {
                                hero.Items = new List<Artifact>();
                                hero.Items.Add(ArtifactFactory.Load(artifactEntity));
                            }
                        }
                    }

                    player.AddArmy(army);
                }
            }

            // My cities (circular reference; add later)
            if (playerEntity.MyCitiesShortNames != null)
            {
                foreach (var cityName in playerEntity.MyCitiesShortNames)
                {
                    cityToPlayer.Add(cityName, player);
                }
            }

            // Player Details
            player.Gold = playerEntity.Gold;
            player.IsDead = playerEntity.IsDead;
            player.Turn = playerEntity.Turn;
            player.LastHeroTurn = playerEntity.LastHeroTurn;

            return player;
        }        
    }
}
