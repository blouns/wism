using System;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Api;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Command = Wism.Client.Api.Commands.Command;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// The Game Manager is the bridge betweeen Wism.Client and Unity. 
    /// It contains the controller interface for manipulating model.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // Game setup defaults
        public const int DefaultRandom = 1990;
        public const float StandardTime = 0.25f;
        public const float WarTime = 1.0f;
        public const int MaxArmies = Army.MaxArmies;
        public static readonly string DefaultWorld = @"Illuria";
        public static string DefaultModPath = @"Assets\Mod";        
        public static string DefaultWorldModPath;

        // Controllers for the WISM Client API
        private ControllerProvider provider;
        private CommandController commandController;
        private UnityManager unityManager;

        public ControllerProvider ControllerProvider { get => provider; set => provider = value; }
        public ILoggerFactory LoggerFactory { get; set; }

        public void Initialize()
        {
            // WISM API Command Controller setup
            LoggerFactory = new WismLoggerFactory();
            var repo = new WismClientInMemoryRepository(new SortedList<int, Command>());
            provider = new ControllerProvider()
            {
                ArmyController = new ArmyController(LoggerFactory),
                CityController = new CityController(LoggerFactory),
                CommandController = new CommandController(LoggerFactory, repo),
                GameController = new GameController(LoggerFactory),
                LocationController = new LocationController(LoggerFactory),
                HeroController = new HeroController(LoggerFactory),
                PlayerController = new PlayerController(LoggerFactory)
            };
            commandController = provider.CommandController;

            // Modules path
            DefaultModPath = @$"{Application.dataPath}\{ModFactory.ModPath}";            
            DefaultWorldModPath = @$"{DefaultModPath}\{ModFactory.WorldsPath}\{DefaultWorld}";
        }

        public void SelectArmies(List<Army> armies)
        {
            if (armies is null)
            {
                throw new System.ArgumentNullException(nameof(armies));
            }

            commandController.AddCommand(
                new SelectArmyCommand(provider.ArmyController, armies));
        }

        public void DeselectArmies()
        {
            commandController.AddCommand(
                new DeselectArmyCommand(provider.ArmyController, Game.Current.GetSelectedArmies()));
        }

        public void ConscriptArmies(Player player, Tile tile, List<ArmyInfo> armyKinds)
        {
            commandController.AddCommand(
                new ConscriptArmiesCommand(provider.PlayerController, player, tile, armyKinds));
        }

        public void PrepareForBattle(int x, int y)
        {
            commandController.AddCommand(
                new PrepareForBattleCommand(provider.ArmyController, Game.Current.GetSelectedArmies(), x, y));
        }

        /// <summary>
        /// Attack the target with currently selected armies.
        /// </summary>
        /// <param name="x">X coordinate to attack</param>
        /// <param name="y">Y coordinate to attack</param>
        /// <remarks>
        /// Attacking is comprised of three commands:
        ///   1. Prepare
        ///   2. Attack
        ///   3. Complete
        /// </remarks>
        public void AttackWithSelectedArmies(int x, int y)
        {
            PrepareForBattle(x, y);

            var attackCommand = new AttackOnceCommand(provider.ArmyController, Game.Current.GetSelectedArmies(), x, y);
            commandController.AddCommand(attackCommand);

            commandController.AddCommand(
                new CompleteBattleCommand(provider.ArmyController, attackCommand));
        }

        public void MoveSelectedArmies(int x, int y)
        {
            commandController.AddCommand(
                new MoveOnceCommand(provider.ArmyController, Game.Current.GetSelectedArmies(), x, y));
        }

        public void DefendSelectedArmies()
        {
            commandController.AddCommand(
                new DefendCommand(provider.ArmyController, Game.Current.GetSelectedArmies()));
        }

        public void QuitSelectedArmies()
        {
            commandController.AddCommand(
                new QuitArmyCommand(provider.ArmyController, Game.Current.GetSelectedArmies()));
        }

        public void SelectNextArmy()
        {
            commandController.AddCommand(
                new SelectNextArmyCommand(provider.ArmyController));
        }

        public void StartProduction(City productionCity, ArmyInfo armyKind, City destinationCity = null)
        {
            commandController.AddCommand(
                new StartProductionCommand(provider.CityController, productionCity, armyKind, destinationCity));
        }

        public void StopProduction(City productionCity)
        {
            commandController.AddCommand(
                new StopProductionCommand(provider.CityController, productionCity));
        }

        public void StartTurn()
        {
            StartTurn(Game.Current.GetNextPlayer());
        }

        /// <summary>
        /// Start the given player's turn
        /// </summary>
        /// <param name="player">Player whose turn it is</param>
        /// <remarks>
        /// Starting a turn is comprised of:
        ///  1. Start turn
        ///  2. Recruit new heros
        ///  3. Hire new heros
        ///  4. Renew production
        /// </remarks>
        public void StartTurn(Player player)
        {
            commandController.AddCommand(
                new StartTurnCommand(provider.GameController, player));

            // Check for and hire any new heros
            var recruitHeroCommand = new RecruitHeroCommand(provider.PlayerController, player);
            commandController.AddCommand(
                recruitHeroCommand);
            commandController.AddCommand(
                new HireHeroCommand(provider.PlayerController, recruitHeroCommand));

            // Renew production
            var reviewProductionCommand = new ReviewProductionCommand(provider.CityController, player);
            commandController.AddCommand(
                reviewProductionCommand);
            commandController.AddCommand(
                new RenewProductionCommand(provider.CityController, player, reviewProductionCommand));
        }

        public void EndTurn()
        {
            commandController.AddCommand(
                new EndTurnCommand(provider.GameController, Game.Current.GetCurrentPlayer()));
            
            // Start the next turn
            StartTurn();
        }

        public void SearchLocation()
        {
            if (!Game.Current.ArmiesSelected())
            {
                return;
            }

            var armies = Game.Current.GetSelectedArmies();
            var location = armies[0].Tile.Location;
            if (location == null)
            {
                // TODO: Perhaps say "you found nothing"?
                Debug.Log("No location on this tile.");
            }

            Command command;
            switch (location.Kind)
            {
                case "Library":
                    command = new SearchLibraryCommand(provider.LocationController, armies, location);
                    break;
                case "Ruins":
                case "Tomb":
                    command = new SearchRuinsCommand(provider.LocationController, armies, location);
                    break;
                case "Sage":
                    command = new SearchSageCommand(provider.LocationController, armies, location);
                    break;
                case "Temple":
                    command = new SearchTempleCommand(provider.LocationController, armies, location);
                    break;
                default:
                    throw new InvalidOperationException("No location to search.");
            }
            commandController.AddCommand(command);
        }

        internal void Build()
        {
            if (!Game.Current.ArmiesSelected())
            {
                NotifyUser("You must have an army selected to build.");
                return;
            }

            var armies = Game.Current.GetSelectedArmies();
            var city = armies[0].Tile.City;
            if (city == null)
            {
                NotifyUser("Tower building is not yet supported.");
                return;
            }

            commandController.AddCommand(
                new BuildCityCommand(provider.CityController, city));
        }

        internal void RazeCity()
        {
            if (!Game.Current.ArmiesSelected())
            {
                return;
            }

            var armies = Game.Current.GetSelectedArmies();
            var city = armies[0].Tile.City;
            if (city == null)
            {
                NotifyUser("Only cities can only be razed.");
                return;
            }            

            commandController.AddCommand(
                new RazeCityCommand(provider.CityController, city));
        }

        public void TakeItems(Hero hero, List<Artifact> items)
        {
            commandController.AddCommand(
                new TakeItemsCommand(provider.HeroController, hero, items));
        }

        public void DropItems(Hero hero, List<Artifact> items)
        {
            commandController.AddCommand(
                new DropItemsCommand(provider.HeroController, hero, items));
        }

        internal void LoadGame(string filename)
        {
            var unityGame = UnityUtilities.GameObjectHardFind("UnityManager")
                .GetComponent<UnityManager>();
            var snapshot = PersistanceManager.LoadEntities(filename, unityGame);

            commandController.AddCommand(
                new LoadGameCommand(provider.GameController, snapshot.WismGameEntity));

            // TODO: Ensure the Unity and Wism snapshots do not get out of sync
            PersistanceManager.SetLastSnapshot(snapshot);
        }

        internal void SaveGame(string filename, string saveGameName)
        {
            var unityGame = UnityUtilities.GameObjectHardFind("UnityManager")
                .GetComponent<UnityManager>();
            PersistanceManager.Save(filename, saveGameName, unityGame);
        }

        private void NotifyUser(string message, params object[] args)
        {
            GetUnityManager().NotifyUser(message, args);
        }

        private UnityManager GetUnityManager()
        {
            if (this.unityManager == null)
            {
                this.unityManager = UnityUtilities.GameObjectHardFind("UnityManager")
                    .GetComponent<UnityManager>();
            }

            return this.unityManager;
        }
    }
}