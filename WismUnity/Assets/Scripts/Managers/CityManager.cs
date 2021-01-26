using Assets.Scripts.Editors;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.Tiles;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using Tile = Wism.Client.Core.Tile;

namespace Assets.Scripts.Managers
{
    public class CityManager : MonoBehaviour
    {
        [SerializeField]
        private CityTile[] cityTiles;

        [SerializeField]
        private CityTile neutralCityTile;

        private WorldTilemap worldTilemap;

        public void DrawCities()
        {
            worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();

            // TODO: Add neutral cities

            // Draw player-owned cities
            foreach (var player in Game.Current.Players)
            {
                foreach (var city in player.GetCities())
                {
                    // Draw city from top-left
                    SetCityTile(city.Tile.X, city.Tile.Y, player.Clan.ShortName);
                    SetCityTile(city.Tile.X+1, city.Tile.Y, player.Clan.ShortName);
                    SetCityTile(city.Tile.X, city.Tile.Y-1, player.Clan.ShortName);
                    SetCityTile(city.Tile.X+1, city.Tile.Y-1, player.Clan.ShortName);
                }                
            }
        }

        internal void SetCityTile(int x, int y, string clanName)
        {
            var worldVector = worldTilemap.ConvertGameToUnityVector(x, y);
            var cityTile = FindCityTile(clanName);
            worldTilemap.SetTile(worldVector, cityTile);
            worldTilemap.RefreshTile(worldVector);            
        }        

        /// <summary>
        /// Assign city by clan
        /// </summary>
        /// <param name="clanName">Clan to assign to the city or "neutral"</param>
        /// <returns>City matching the clan if found; otherwise Neutral city</returns>
        private CityTile FindCityTile(string clanName)
        {
            var returnTile = neutralCityTile;
            
            for (int i = 0; i < cityTiles.Length; i++)
            {
                string tileShortName = cityTiles[i].name.ToLowerInvariant();
                if (tileShortName.Contains(clanName.ToLowerInvariant()))
                {
                    returnTile = cityTiles[i];
                    break;
                }
            }

            return returnTile;
        }
    }
}
