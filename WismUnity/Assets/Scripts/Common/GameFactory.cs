﻿using Assets.Scripts.Editors;
using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Core;
using Wism.Client.Modules;
using Wism.Client.War;

namespace Assets.Scripts
{
    public class GameFactory : MonoBehaviour
    {
        private WorldTilemap worldTilemap;

        public void Start()
        {
            this.worldTilemap = UnityUtilities.GameObjectHardFind("WorldTilemap")
                .GetComponent<WorldTilemap>();
        }

        public void CreateDefaultGame()
        {
            // Set up the Game
            MapBuilder.Initialize(GameManager.DefaultModPath, GameManager.DefaultWorld);
            Game.CreateEmpty();
            Game.Current.Random = new System.Random(GameManager.DefaultRandom);
            Game.Current.WarStrategy = new DefaultWarStrategy();

            ReadyPlayers();
            World.CreateWorld(
                worldTilemap.CreateWorldFromScene(GameManager.DefaultWorld).Map);            
            CreateDefaultCitiesFromScene();
            CreateDefaultArmies();
        }

        private static List<Player> ReadyPlayers()
        {
            Game.Current.Players = new List<Player>();

            // Ready Player One
            ClanInfo clanInfo = ClanInfo.GetClanInfo("Sirians");
            Clan clan = Clan.Create(clanInfo);
            Player player1 = Player.Create(clan);
            Game.Current.Players.Add(player1);
            player1.Clan.IsHuman = true;

            // Ready Player Two
            clanInfo = ClanInfo.GetClanInfo("StormGiants");
            clan = Clan.Create(clanInfo);
            Player player2 = Player.Create(clan);
            Game.Current.Players.Add(player2);
            player2.Clan.IsHuman = false;

            clanInfo = ClanInfo.GetClanInfo("Elvallie");
            clan = Clan.Create(clanInfo);
            Player player3 = Player.Create(clan);
            Game.Current.Players.Add(player3);
            player3.Clan.IsHuman = false;

            return Game.Current.Players;
        }

        /// <summary>
        /// FOR TESTING ONLY: Create default armies from scene.
        /// </summary>        
        private static void CreateDefaultArmies()
        {
            Player sirians = Game.Current.Players[0];
            var capitolPosition = UnityUtilities.GameObjectHardFind("Marthos")
                .GetComponent<CityEntry>()
                .GetGameCoordinates();
            sirians.HireHero(World.Current.Map[capitolPosition.x, capitolPosition.y]);

            Player stormgiants = Game.Current.Players[1];
            capitolPosition = UnityUtilities.GameObjectHardFind("Stormheim")
                .GetComponent<CityEntry>()
                .GetGameCoordinates();
            stormgiants.HireHero(World.Current.Map[capitolPosition.x, capitolPosition.y]);

            Player elvallie = Game.Current.Players[2];
            capitolPosition = UnityUtilities.GameObjectHardFind("Elvallie")
                .GetComponent<CityEntry>()
                .GetGameCoordinates();
            elvallie.HireHero(World.Current.Map[capitolPosition.x, capitolPosition.y]);
        }

        /// <summary>
        /// FOR TESTING ONLY: Create default cities from scene.
        /// </summary>        
        private void CreateDefaultCitiesFromScene()
        {
            Dictionary<string, GameObject> citiesNames = new Dictionary<string, GameObject>();

            // Extract the X,Y coords from City GameObjects from the scene 
            var cityContainerGO = UnityUtilities.GameObjectHardFind("Cities");
            int cityCount = cityContainerGO.transform.childCount;
            for (int i = 0; i < cityCount; i++)
            {
                var cityGO = cityContainerGO.transform.GetChild(i).gameObject;
                var cityEntry = cityGO.GetComponent<CityEntry>();

                if (citiesNames.ContainsKey(cityEntry.cityShortName))
                {
                    continue;
                }

                citiesNames.Add(cityEntry.cityShortName, cityGO);
                cityGO.name = cityEntry.cityShortName;
            }

            // Set the coords for the new city on the CityInfos
            var cityInfos = new List<CityInfo>(
                ModFactory.LoadCityInfos(GameManager.DefaultCityModPath));
            var illuriaCities = new List<CityInfo>();
            foreach (CityInfo ci in cityInfos)
            {
                if (citiesNames.ContainsKey(ci.ShortName))
                {
                    var go = citiesNames[ci.ShortName];
                    var coords = worldTilemap.ConvertUnityToGameVector(go.transform.position);
                    ci.X = coords.x;
                    ci.Y = coords.y + 1;    // +1 Adjustment for city object overlay alignment (anchor)
                    illuriaCities.Add(ci);
                }
            }

            MapBuilder.AddCitiesToMapFromWorld(World.Current.Map, illuriaCities);
        }
    }
}