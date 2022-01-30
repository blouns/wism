using Assets.Scripts.Tilemaps;
using Assets.Scripts.Tiles;
using UnityEngine;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Assets.Scripts.Managers
{
    public class CityManager : MonoBehaviour
    {
        [SerializeField]
        private CityTile[] cityTiles;

        [SerializeField]
        private CityTile neutralCityTile;

        [SerializeField]
        private RuinsTile ruinsTile;

        private WorldTilemap worldTilemap;
        private bool isInitialized;

        public void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            this.isInitialized = true;
        }

        public void Reset()
        {
            this.worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();

            // Reset all cities to neutral
            foreach (var city in World.Current.GetCities())
            {
                SetNeutralCityTile(city.Tile.X, city.Tile.Y);
                SetNeutralCityTile(city.Tile.X + 1, city.Tile.Y);
                SetNeutralCityTile(city.Tile.X, city.Tile.Y - 1);
                SetNeutralCityTile(city.Tile.X + 1, city.Tile.Y - 1);
            }

            // Set ownership for Player-owned cities
            foreach (var player in Game.Current.Players)
            {
                foreach (var city in player.GetCities())
                {
                    // Draw city from top-left
                    SetCityTile(city.Tile.X, city.Tile.Y, player.Clan.ShortName);
                    SetCityTile(city.Tile.X + 1, city.Tile.Y, player.Clan.ShortName);
                    SetCityTile(city.Tile.X, city.Tile.Y - 1, player.Clan.ShortName);
                    SetCityTile(city.Tile.X + 1, city.Tile.Y - 1, player.Clan.ShortName);
                }
            }
        }

        public void Raze(City city)
        {
            SetRuinsTile(city.Tile.X, city.Tile.Y);
            SetRuinsTile(city.Tile.X + 1, city.Tile.Y);
            SetRuinsTile(city.Tile.X, city.Tile.Y - 1);
            SetRuinsTile(city.Tile.X + 1, city.Tile.Y - 1);
        }

        public void FixedUpdate()
        {
            if (!IsInitialized())
            {
                return;
            }

            DrawCities();
        }

        private bool IsInitialized()
        {
            return this.isInitialized && Game.IsInitialized();
        }

        public void DrawCities()
        {
            this.worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();

            // Draw player-owned cities
            foreach (var player in Game.Current.Players)
            {
                foreach (var city in player.GetCities())
                {
                    // Draw city from top-left
                    SetCityTile(city.Tile.X, city.Tile.Y, player.Clan.ShortName);
                    SetCityTile(city.Tile.X + 1, city.Tile.Y, player.Clan.ShortName);
                    SetCityTile(city.Tile.X, city.Tile.Y - 1, player.Clan.ShortName);
                    SetCityTile(city.Tile.X + 1, city.Tile.Y - 1, player.Clan.ShortName);
                }
            }
        }

        internal void SetCityTile(int x, int y, string clanName)
        {
            var worldVector = this.worldTilemap.ConvertGameToUnityVector(x, y);
            var cityTile = FindCityTile(clanName);
            this.worldTilemap.SetTile(worldVector, cityTile);
            this.worldTilemap.RefreshTile(worldVector);
        }

        private void SetNeutralCityTile(int x, int y)
        {
            var worldVector = this.worldTilemap.ConvertGameToUnityVector(x, y);
            this.worldTilemap.SetTile(worldVector, this.neutralCityTile);
            this.worldTilemap.RefreshTile(worldVector);
        }

        /// <summary>
        /// Find a city by clan
        /// </summary>
        /// <param name="clanName">Clan to assign to the city or "neutral"</param>
        /// <returns>City matching the clan if found; otherwise Neutral city</returns>
        private CityTile FindCityTile(string clanName)
        {
            var returnTile = this.neutralCityTile;

            for (int i = 0; i < this.cityTiles.Length; i++)
            {
                string tileShortName = this.cityTiles[i].name.ToLowerInvariant();
                if (tileShortName.Contains(clanName.ToLowerInvariant()))
                {
                    returnTile = this.cityTiles[i];
                    break;
                }
            }

            return returnTile;
        }

        private void SetRuinsTile(int x, int y)
        {
            var worldVector = this.worldTilemap.ConvertGameToUnityVector(x, y);
            // BUGBUG: This does not work. Image is reset to the original neutral castle instead.
            this.worldTilemap.SetTile(worldVector, ScriptableObject.CreateInstance<RuinsTile>());
            this.worldTilemap.RefreshTile(worldVector);
        }
    }
}
