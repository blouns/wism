using Assets.Scripts.Editors;
using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.UnityGame.Factories;
using Assets.Scripts.UnityGame.Persistance.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Wism.Client.Core;
using Wism.Client.Entities;
using Wism.Client.Modules;
using Wism.Client.War;
using LocationInfo = Wism.Client.Modules.LocationInfo;

namespace Assets.Scripts
{
    public class GameFactory : MonoBehaviour
    {
        private UnityManager unityManager;
        private WorldTilemap worldTilemap;
        private DebugManager debugManager;
        private bool isInitialized;

        public string WorldName { get; set; }

        public void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (!isInitialized)
            {
                this.worldTilemap = UnityUtilities.GameObjectHardFind("WorldTilemap")
                    .GetComponent<WorldTilemap>();
                this.unityManager = UnityUtilities.GameObjectHardFind("UnityManager")
                    .GetComponent<UnityManager>();
                this.debugManager = this.unityManager
                    .GetComponent<DebugManager>();

                this.isInitialized = true;
            }
        }

        [Obsolete]
        public void CreateGame(string worldName, int randomSeed = GameManager.DefaultRandom)
        {
            if (string.IsNullOrWhiteSpace(worldName))
            {
                throw new ArgumentException($"'{nameof(worldName)}' cannot be null or whitespace", nameof(worldName));
            }            

            Initialize();
            this.debugManager = this.debugManager = UnityUtilities.GameObjectHardFind("UnityManager")
                    .GetComponent<DebugManager>();            
            this.debugManager.LogInformation("World: " + worldName);
            this.debugManager.LogInformation("Mod Path: " + GameManager.DefaultModPath);
            this.WorldName = worldName;

            // Set up the Game
            MapBuilder.Initialize(GameManager.DefaultModPath, worldName);
            this.debugManager.LogInformation("Initialized MapBuilder");
            Game.CreateEmpty();
            Game.Current.Random = new System.Random(randomSeed);
            Game.Current.WarStrategy = new DefaultWarStrategy();
            this.debugManager.LogInformation("Initialized Game");

            ReadyPlayers();
            this.debugManager.LogInformation("Initialized Players");

            this.debugManager.LogInformation("Creating World: " + GameManager.CurrentWorldName);
            World.CreateWorld(
                worldTilemap.CreateWorldFromScene(GameManager.CurrentWorldName).Map);
            debugManager.LogInformation("Created world from scene");
            CreateDefaultCitiesFromScene();
            debugManager.LogInformation("Created cities from scene");
            CreateDefaultLocationsFromScene();
            debugManager.LogInformation("Created locations from scene");
            CreateDefaultArmies();
            this.debugManager.LogInformation("Initialized Game");
        }

        public void CreateDefaultGame()
        {
#if UNITY_EDITOR
            CreateGame(GameManager.CurrentWorldName);
#else
            CreateGame(GameManager.DefaultWorld, (int)DateTime.Now.Ticks);
#endif
        }

        public void CreateGame(UnityNewGameEntity newGameEntity)
        {
            if (newGameEntity is null)
            {
                throw new ArgumentNullException(nameof(newGameEntity));
            }

            ValidateNewGameSettings(newGameEntity);

            Initialize();
            this.debugManager = this.debugManager = UnityUtilities.GameObjectHardFind("UnityManager")
                    .GetComponent<DebugManager>();

            // Set up the Game
            this.WorldName = newGameEntity.WorldName;
            this.debugManager.LogInformation($"Creating new game in { this.WorldName }...");
            MapBuilder.Initialize(GameManager.DefaultModPath, this.WorldName);

            // Game settings
            this.debugManager.LogInformation("Creating new game settings...");
            GameEntity settings = new GameEntity();
            settings.Random = new RandomEntity() { Seed = newGameEntity.RandomSeed };
            settings.WarStrategy = CreateDefaultWarStrategy();
            // TODO: Interactive UI setting
            // TODO: Random start location setting

            // Ready players
            this.debugManager.LogInformation($"Creating { newGameEntity.Players.Length } players...");
            var playerFactory = new UnityPlayerFactory();
            settings.Players = playerFactory.CreatePlayers(newGameEntity.Players);

            this.debugManager.LogInformation("Creating world from scene " + this.WorldName + "...");
            GameManager.CurrentWorldName = this.WorldName;  // TODO: Remove this once migration is complete
            UnityWorldFactory worldFactory = new UnityWorldFactory(this.debugManager);
            settings.World = worldFactory.CreateWorld(this.WorldName, this.worldTilemap);

            this.debugManager.LogInformation("Initializing game...");
            this.unityManager.GameManager.NewGame(settings);
        }

        public void LoadNewGame()
        {
            Initialize();
            this.debugManager = this.debugManager = UnityUtilities.GameObjectHardFind("UnityManager")
                    .GetComponent<DebugManager>();

            // Create temporary Game to bootstrap loader
            Game.CreateDefaultGame();
            World.CreateDefaultWorld();

            // Load game picker
            this.unityManager.InputManager.HandleSaveLoadPicker(false);
        }

        private static void ValidateNewGameSettings(UnityNewGameEntity newGameEntity)
        {
            if (!newGameEntity.IsNewGame)
            {
                throw new ArgumentException("Settings are not valid for a new game", nameof(newGameEntity));
            }
        }

        private static WarStrategyEntity CreateDefaultWarStrategy()
        {
            var entity = new WarStrategyEntity()
            {
                AssemblyName = Assembly.GetAssembly(typeof(DefaultWarStrategy)).FullName,
                TypeName = (typeof(DefaultWarStrategy)).FullName
            };

            return entity;
        }

        public static UnityNewGameEntity CreateDefaultGameSettings()
        {
            var settings = new UnityNewGameEntity();

            settings.WorldName = GameManager.DefaultWorld;
            settings.InteractiveUI = true;
            settings.RandomStartLocations = false;
            settings.RandomSeed = GameManager.DefaultRandom;
            settings.IsNewGame = true;
            CreateDefaultPlayers(settings);

            return settings;
        }

        private static void CreateDefaultPlayers(UnityNewGameEntity settings)
        {
            settings.Players = new UnityPlayerEntity[]
            {
                new UnityPlayerEntity()
                {
                    ClanName = "Sirians",
                    IsHuman = true
                },
                new UnityPlayerEntity()
                {
                    ClanName = "StormGiants",
                    IsHuman = true
                },
                new UnityPlayerEntity()
                {
                    ClanName = "Elvallie",
                    IsHuman = true
                },
                new UnityPlayerEntity()
                {
                    ClanName = "OrcsOfKor",
                    IsHuman = true
                },
                new UnityPlayerEntity()
                {
                    ClanName = "Selentines",
                    IsHuman = true
                },
                new UnityPlayerEntity()
                {
                    ClanName = "HorseLords",
                    IsHuman = true
                },
                new UnityPlayerEntity()
                {
                    ClanName = "GreyDwarves",
                    IsHuman = true
                },
                new UnityPlayerEntity()
                {
                    ClanName = "LordBane",
                    IsHuman = true
                }
            };
        }

        private static List<Player> ReadyPlayers()
        {
            Game.Current.Players = new List<Player>();

            // HACK: TODO: Fix with proper scriptable game starts. Temp for now to provide test env.
            if (GameManager.CurrentWorldName == "TestWorld")
            {
                ReadyPlayer("Sirians");
                ReadyPlayer("LordBane");
            }
            else
            {
                ReadyPlayer("Sirians");
                ReadyPlayer("StormGiants");
                ReadyPlayer("Elvallie");
                ReadyPlayer("OrcsOfKor");
                ReadyPlayer("Selentines");
                ReadyPlayer("HorseLords");
                ReadyPlayer("LordBane");
                ReadyPlayer("GreyDwarves");
            }

            return Game.Current.Players;
        }

        private static void ReadyPlayer(string clanShortName)
        {
            ClanInfo clanInfo = ClanInfo.GetClanInfo(clanShortName);
            Clan clan = Clan.Create(clanInfo);
            Player player = Player.Create(clan);
            player.Gold = 2000; // FOR TESTING ONLY
            Game.Current.Players.Add(player);
        }

        /// <summary>
        /// FOR TESTING ONLY: Create default armies from scene.
        /// </summary>        
        private static void CreateDefaultArmies()
        {
            // No default armies!

            // HACK: TODO: Remove once game start is scriptable
            if (GameManager.CurrentWorldName == "TestWorld")
            {
                Player sirians = Game.Current.Players[0];
                var capitolPosition = UnityUtilities.GameObjectHardFind("Marthos")
                    .GetComponent<CityEntry>()
                    .GetGameCoordinates();
                sirians.HireHero(World.Current.Map[capitolPosition.x, capitolPosition.y]);

                Player lordBane = Game.Current.Players[1];
                capitolPosition = UnityUtilities.GameObjectHardFind("BanesCitadel")
                    .GetComponent<CityEntry>()
                    .GetGameCoordinates();
                lordBane.HireHero(World.Current.Map[capitolPosition.x, capitolPosition.y]);
            }
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
            this.debugManager.LogInformation("Initialized cities GameObjects");

            // Set the coordinates for the cities
            var cityInfos = new List<CityInfo>(
                ModFactory.LoadCityInfos(GameManager.DefaultWorldModPath));

            this.debugManager.LogInformation("Loaded city infos");
            var cities = new List<CityInfo>();
            foreach (CityInfo ci in cityInfos)
            {
                if (citiesNames.ContainsKey(ci.ShortName))
                {
                    var go = citiesNames[ci.ShortName];
                    var coords = worldTilemap.ConvertUnityToGameVector(go.transform.position);
                    ci.X = coords.x;
                    ci.Y = coords.y + 1;    // +1 Adjustment for city object overlay alignment (anchor)
                    cities.Add(ci);
                }
            }
            this.debugManager.LogInformation("Updated city infos with locations on map");
            MapBuilder.AddCitiesFromInfos(World.Current, cities);
            this.debugManager.LogInformation("Added cities from infos");
        }

        /// <summary>
        /// FOR TESTING ONLY: Create default locations from scene.
        /// </summary>
        private void CreateDefaultLocationsFromScene()
        {
            Dictionary<string, GameObject> locationsNames = new Dictionary<string, GameObject>();

            // Extract the X,Y coords from Location GameObjects from the scene 
            var locationContainerGO = UnityUtilities.GameObjectHardFind("Locations");
            int locationCount = locationContainerGO.transform.childCount;
            for (int i = 0; i < locationCount; i++)
            {
                var locationGO = locationContainerGO.transform.GetChild(i).gameObject;
                var locationEntry = locationGO.GetComponent<LocationEntry>();

                if (locationsNames.ContainsKey(locationEntry.locationShortName))
                {
                    continue;
                }

                locationsNames.Add(locationEntry.locationShortName, locationGO);
                locationGO.name = locationEntry.locationShortName;
            }

            // Set the coordinates for the locations
            var locationInfos = new List<LocationInfo>(
                ModFactory.LoadLocationInfos(GameManager.DefaultWorldModPath));
            var locations = new List<LocationInfo>();
            foreach (LocationInfo ci in locationInfos)
            {
                if (locationsNames.ContainsKey(ci.ShortName))
                {
                    var go = locationsNames[ci.ShortName];
                    var coords = worldTilemap.ConvertUnityToGameVector(go.transform.position);
                    ci.X = coords.x;
                    ci.Y = coords.y;
                    locations.Add(ci);
                }
            }
            MapBuilder.AddLocationsFromInfos(World.Current, locations);
            MapBuilder.AllocateBoons(World.Current.GetLocations());
        }
    }
}
