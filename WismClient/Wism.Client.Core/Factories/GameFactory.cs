using System;
using System.Collections.Generic;
using System.Reflection;
using Wism.Client.Core;
using Wism.Client.Entities;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.War;

namespace Wism.Client.Factories
{
    public static class GameFactory
    {
        public static Game Load(GameEntity snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            Game.CreateEmpty();
            var current = Game.Current;

            // Game settings
            current.Random = new Random(snapshot.RandomSeed);            
            var warEntity = snapshot.WarStrategy;
            var warAssembly = Assembly.Load(warEntity.AssemblyName);
            current.WarStrategy = (IWarStrategy)warAssembly.CreateInstance(warEntity.TypeName);
            current.Transition(snapshot.GameState);

            // Players
            LoadPlayers(snapshot, current,
                out Dictionary<string, Player> cityToPlayer,
                out Dictionary<string, Player> capitolToPlayer);

            // Load world
            current.World = WorldFactory.Load(snapshot.World,
                out Dictionary<string, Tile> cityToTile,
                out Dictionary<string, Tile> armiesToTile,
                out Dictionary<string, Tile> visitingToTile);

            // Load late-bound cities
            foreach (var cityName in cityToTile.Keys)
            {
                MapBuilder.AddCity(current.World, cityToTile[cityName].X, cityToTile[cityName].Y, cityName);
                var city = current.World.Map[cityToTile[cityName].X, cityToTile[cityName].Y].City;

                if (cityToPlayer.ContainsKey(cityName))
                {
                    cityToPlayer[cityName].AddCity(city);
                }

                if (capitolToPlayer.ContainsKey(cityName))
                {
                    capitolToPlayer[cityName].Capitol = city;
                }
            }

            // Load late-bound army properties
            var selectedArmies = new List<Army>();
            var allArmies = Game.Current.GetAllArmies();
            foreach (var army in allArmies)
            {
                // Tiles for armies and visiting armies
                if (armiesToTile.ContainsKey(army.ShortName))
                {
                    var tile = armiesToTile[army.ShortName];
                    tile.AddArmies(new List<Army>() { army });
                }
                else if (visitingToTile.ContainsKey(army.ShortName))
                {
                    var tile = visitingToTile[army.ShortName];
                    tile.AddVisitingArmies(new List<Army>() { army });
                }

                // Selected armies
                if (snapshot.SelectedArmyIds != null)
                {
                    for (int i = 0; i < snapshot.SelectedArmyIds.Length; i++)
                    {
                        if (army.Id == snapshot.SelectedArmyIds[i])
                        {
                            selectedArmies.Add(army);
                            break;
                        }
                    }
                }
            }

            return current;
        }

        private static void LoadPlayers(GameEntity snapshot, Game current,
            out Dictionary<string, Player> cityToPlayer,
            out Dictionary<string, Player> capitolToPlayer)
        {
            cityToPlayer = new Dictionary<string, Player>();
            capitolToPlayer = new Dictionary<string, Player>();

            var players = snapshot.Players;
            current.Players = new List<Player>();            
            for (int i = 0; i < players.Length; i++)
            {
                current.Players.Add(PlayerFactory.Load(players[i], 
                    out cityToPlayer,
                    out capitolToPlayer));
            }
        }
    }
}
