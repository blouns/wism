﻿using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.UnityGame.Factories;
using Assets.Scripts.UnityGame.Persistance.Entities;
using Newtonsoft.Json;
using System;
using System.Reflection;
using UnityEngine;
using Wism.Client.Core;
using Wism.Client.Entities;
using Wism.Client.Modules;
using Wism.Client.War;

namespace Assets.Scripts
{
    public class GameFactory : MonoBehaviour
    {
        private UnityManager unityManager;
        private DebugManager debugManager;
        private bool isInitialized;

        private string worldName = GameManager.DefaultWorld;
        private string modPath = GameManager.DefaultModPath;
        public string WorldName { get => worldName; set => worldName = value; }
        public string ModPath { get => modPath; set => modPath = value; }

        public void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (!isInitialized)
            {
                this.unityManager = UnityUtilities.GameObjectHardFind("UnityManager")
                    .GetComponent<UnityManager>();
                this.debugManager = this.unityManager
                    .GetComponent<DebugManager>();

                // Factory uses world name to load ModFactory data (mod path)
                if (unityManager == null || !unityManager.IsInitalized())
                {
                    this.WorldName = GameManager.DefaultWorld;
                    this.ModPath = GameManager.DefaultModPath;
                }
                else
                {
                    this.WorldName = unityManager.GameManager.WorldName;
                    this.ModPath = unityManager.GameManager.ModPath;
                }

                this.isInitialized = true;
            }
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
            this.unityManager.InteractiveUI = newGameEntity.InteractiveUI;

            // Set up the Game
            this.WorldName = newGameEntity.WorldName;
            this.debugManager.LogInformation($"Creating new game in { this.WorldName }...");

            // Game settings
            this.debugManager.LogInformation("Creating new game settings...");
            GameEntity settings = new GameEntity();
            settings.Random = new RandomEntity() { Seed = newGameEntity.RandomSeed };
            settings.WarStrategy = CreateDefaultWarStrategy();                       

            // TODO: Random start location setting

            // Ready players
            this.debugManager.LogInformation($"Creating { newGameEntity.Players.Length } players...");
            var playerFactory = new UnityPlayerFactory();
            settings.Players = playerFactory.CreatePlayers(newGameEntity.Players);

            this.debugManager.LogInformation("Creating world from scene " + this.WorldName + "...");
            UnityWorldFactory worldFactory = new UnityWorldFactory(this.debugManager);
            settings.World = worldFactory.CreateWorld(this.WorldName, unityManager);

            var serializedSettings = JsonConvert.SerializeObject(settings, Formatting.Indented);
            

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
    }
}