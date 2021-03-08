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
            current.Random = LoadRandom(snapshot.Random);
            var warEntity = snapshot.WarStrategy;
            var warAssembly = Assembly.Load(warEntity.AssemblyName);
            current.WarStrategy = (IWarStrategy)warAssembly.CreateInstance(warEntity.TypeName);
            current.Transition(snapshot.GameState);

            LoadPlayers(snapshot, current,
                out Dictionary<string, Player> cityToPlayer,
                out Dictionary<string, Player> capitolToPlayer);

            var world = WorldFactory.Load(snapshot.World,
                out Dictionary<string, Tile> cityToTile,
                out Dictionary<string, Tile> armiesToTile,
                out Dictionary<string, Tile> visitingToTile);

            LoadCities(snapshot, world, cityToPlayer, capitolToPlayer, cityToTile);
            LoadArmies(snapshot, armiesToTile, visitingToTile);

            // Factory state
            ArmyFactory.LastId = snapshot.LastArmyId;

            return current;
        }

        private static void LoadArmies(GameEntity snapshot, Dictionary<string, Tile> armiesToTile, Dictionary<string, Tile> visitingToTile)
        {
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
        }

        private static void LoadCities(GameEntity snapshot, World world,
            Dictionary<string, Player> cityToPlayer, 
            Dictionary<string, Player> capitolToPlayer, 
            Dictionary<string, Tile> cityToTile)
        {
            foreach (var citySnapshot in snapshot.World.Cities)
            {
                var city = CityFactory.Load(citySnapshot, world);

                // Add late-bound properties
                if (cityToPlayer.ContainsKey(city.ShortName))
                {
                    cityToPlayer[city.ShortName].AddCity(city);
                }

                if (capitolToPlayer.ContainsKey(city.ShortName))
                {
                    capitolToPlayer[city.ShortName].Capitol = city;
                }
            }
        
            
            
            foreach (var cityName in cityToTile.Keys)
            {
                //MapBuilder.AddCity(game.World, cityToTile[cityName].X, cityToTile[cityName].Y, cityName);
                //var city = current.World.Map[cityToTile[cityName].X, cityToTile[cityName].Y].City;                

                
            }
        }

        /// <summary>
        /// Loads the random seed array into a new Random instance
        /// </summary>
        /// <param name="snapshot">RandomEntity to load</param>
        /// <returns>New Random based on the snapshot</returns>
        /// <remarks>
        /// This method overwrites the private seed array from Random. The seed in this 
        /// case is actually unused, but it is set for consistency.
        /// </remarks>
        private static Random LoadRandom(RandomEntity snapshot)
        {            
            var random = new Random(snapshot.Seed);
            var seedArrayCopy = (int[])snapshot.SeedArray.Clone();

            var seedArrayInfo = typeof(Random).GetField("SeedArray", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            seedArrayInfo.SetValue(random, seedArrayCopy);

            return random;
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
