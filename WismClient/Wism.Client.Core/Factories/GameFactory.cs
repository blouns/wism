using System;
using System.Collections.Generic;
using System.Reflection;
using Wism.Client.Core;
using Wism.Client.Entities;
using Wism.Client.MapObjects;
using Wism.Client.War;

namespace Wism.Client.Factories
{
    public static class GameFactory
    {
        public static Game Create(GameEntity settings)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Game.CreateEmpty();

            // Game settings
            Game.Current.Random = new Random(settings.Random.Seed);
            var warEntity = settings.WarStrategy;
            var warAssembly = Assembly.Load(warEntity.AssemblyName);
            Game.Current.WarStrategy = (IWarStrategy)warAssembly.CreateInstance(warEntity.TypeName);
            Game.Current.Transition(settings.GameState);

            CreatePlayers(settings, Game.Current);

            var world = WorldFactory.Create(settings.World);

            return Game.Current;
        }

        public static Game Load(GameEntity snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            Game.CreateEmpty();

            // Game settings
            Game.Current.Random = LoadRandom(snapshot.Random);
            var warEntity = snapshot.WarStrategy;
            var warAssembly = Assembly.Load(warEntity.AssemblyName);
            Game.Current.WarStrategy = (IWarStrategy)warAssembly.CreateInstance(warEntity.TypeName);
            Game.Current.Transition(snapshot.GameState);

            LoadPlayers(snapshot, Game.Current,
                out Dictionary<string, Player> cityToPlayer,
                out Dictionary<string, Player> capitolToPlayer);

            var world = WorldFactory.Load(snapshot.World,
                out Dictionary<int, Tile> armiesToTile,
                out Dictionary<int, Tile> visitingToTile);

            LoadCities(snapshot, world, cityToPlayer, capitolToPlayer);
            LoadArmies(snapshot, armiesToTile, visitingToTile);

            // Factory state
            ArmyFactory.LastId = snapshot.LastArmyId;

            return Game.Current;
        }

        private static void CreatePlayers(GameEntity settings, Game current)
        {
            var players = settings.Players;
            current.Players = new List<Player>();
            for (int i = 0; i < players.Length; i++)
            {
                current.Players.Add(PlayerFactory.Create(players[i]));
            }
        }

        private static void LoadArmies(GameEntity snapshot, Dictionary<int, Tile> armiesToTile, Dictionary<int, Tile> visitingToTile)
        {
            var selectedArmies = new List<Army>();
            var allArmies = Game.Current.GetAllArmies();
            foreach (var army in allArmies)
            {
                // Tiles for armies and visiting armies
                if (armiesToTile.ContainsKey(army.Id))
                {
                    var tile = armiesToTile[army.Id];
                    tile.AddArmies(new List<Army>() { army });
                }
                else if (visitingToTile.ContainsKey(army.Id))
                {
                    var tile = visitingToTile[army.Id];
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

                    // Select armies: need to side-load since they were saved as Visiting Armies
                    Game.Current.SelectArmiesInternal(selectedArmies);
                }
            }
        }

        private static void LoadCities(GameEntity snapshot, World world,
            Dictionary<string, Player> cityToPlayer, 
            Dictionary<string, Player> capitolToPlayer)
        {
            if (snapshot.World.Cities != null)
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
            Game.Current.RandomSeed = snapshot.Seed;
            return snapshot.Random;
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
