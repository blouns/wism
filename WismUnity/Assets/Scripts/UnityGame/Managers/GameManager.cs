using System;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Controllers;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Command = Wism.Client.Commands.Command;
using Wism.Client.Modules.Infos;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Players;
using Wism.Client.Commands.Cities;
using Wism.Client.Commands.Heros;
using Wism.Client.Data.Entities;
using Wism.Client.Commands.Games;
using Wism.Client.Commands.Locations;
using Wism.Client.Data;

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
        public float StandardTime = 0.25f;
        public float WarTime = 1.0f;
        public const int MaxArmies = Army.MaxArmies;
        public readonly static string DefaultWorld = @"Mini-Illuria";
        public readonly static string DefaultModPath = @"Assets\Mod";
        public readonly static string DefaultWorldModPath = @$"{DefaultModPath}\{ModFactory.WorldsPath}\{DefaultWorld}";

        [SerializeField]
        private string worldName = DefaultWorld;
        [SerializeField]
        private string modPath = DefaultModPath;

        // Controllers for the WISM Client API
        private ControllerProvider provider;
        private CommandController commandController;
        private UnityManager unityManager;

        public ControllerProvider ControllerProvider { get => this.provider; set => this.provider = value; }
        public IWismLoggerFactory LoggerFactory { get; set; }

        public string WorldName { get => this.worldName; set => this.worldName = value; }
        public string ModPath { get => this.modPath; set => this.modPath = value; }

        public void Initialize()
        {
            // WISM API Command Controller setup
            this.LoggerFactory = new WismLoggerFactory();
            var repo = new WismClientInMemoryRepository(new SortedList<int, Command>());
            this.provider = new ControllerProvider()
            {
                ArmyController = new ArmyController(this.LoggerFactory),
                CityController = new CityController(this.LoggerFactory),
                CommandController = new CommandController(this.LoggerFactory, repo),
                GameController = new GameController(this.LoggerFactory),
                LocationController = new LocationController(this.LoggerFactory),
                HeroController = new HeroController(this.LoggerFactory),
                PlayerController = new PlayerController(this.LoggerFactory)
            };
            this.commandController = this.provider.CommandController;
        }

        public void SelectArmies(List<Army> armies)
        {
            if (armies is null)
            {
                throw new System.ArgumentNullException(nameof(armies));
            }

            this.commandController.AddCommand(
                new SelectArmyCommand(this.provider.ArmyController, armies));
        }

        public void DeselectArmies()
        {
            this.commandController.AddCommand(
                new DeselectArmyCommand(this.provider.ArmyController, Game.Current.GetSelectedArmies()));
        }

        public void ConscriptArmies(Player player, Tile tile, List<ArmyInfo> armyKinds)
        {
            this.commandController.AddCommand(
                new ConscriptArmiesCommand(this.provider.PlayerController, player, tile, armyKinds));
        }

        public void PrepareForBattle(int x, int y)
        {
            this.commandController.AddCommand(
                new PrepareForBattleCommand(this.provider.ArmyController, Game.Current.GetSelectedArmies(), x, y));
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

            var attackCommand = new AttackOnceCommand(this.provider.ArmyController, Game.Current.GetSelectedArmies(), x, y);
            this.commandController.AddCommand(attackCommand);

            this.commandController.AddCommand(
                new CompleteBattleCommand(this.provider.ArmyController, attackCommand));
        }

        public void MoveSelectedArmies(int x, int y)
        {
            this.commandController.AddCommand(
                new MoveOnceCommand(this.provider.ArmyController, Game.Current.GetSelectedArmies(), x, y));
        }

        public void DefendSelectedArmies()
        {
            this.commandController.AddCommand(
                new DefendCommand(this.provider.ArmyController, Game.Current.GetSelectedArmies()));
        }

        public void QuitSelectedArmies()
        {
            this.commandController.AddCommand(
                new QuitArmyCommand(this.provider.ArmyController, Game.Current.GetSelectedArmies()));
        }

        public void SelectNextArmy()
        {
            this.commandController.AddCommand(
                new SelectNextArmyCommand(this.provider.ArmyController));
        }

        public void StartProduction(City productionCity, ArmyInfo armyKind, City destinationCity = null)
        {
            this.commandController.AddCommand(
                new StartProductionCommand(this.provider.CityController, productionCity, armyKind, destinationCity));
        }

        public void StopProduction(City productionCity)
        {
            this.commandController.AddCommand(
                new StopProductionCommand(this.provider.CityController, productionCity));
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
            this.commandController.AddCommand(
                new StartTurnCommand(this.provider.GameController, player));

            // Check for and hire any new heros
            var recruitHeroCommand = new RecruitHeroCommand(this.provider.PlayerController, player);
            this.commandController.AddCommand(
                recruitHeroCommand);
            this.commandController.AddCommand(
                new HireHeroCommand(this.provider.PlayerController, recruitHeroCommand));

            // Renew production
            var reviewProductionCommand = new ReviewProductionCommand(this.provider.CityController, player);
            this.commandController.AddCommand(
                reviewProductionCommand);
            this.commandController.AddCommand(
                new RenewProductionCommand(this.provider.CityController, player, reviewProductionCommand));
        }

        public void EndTurn()
        {
            this.commandController.AddCommand(
                new EndTurnCommand(this.provider.GameController, Game.Current.GetCurrentPlayer()));

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
                    command = new SearchLibraryCommand(this.provider.LocationController, armies, location);
                    break;
                case "Ruins":
                case "Tomb":
                    command = new SearchRuinsCommand(this.provider.LocationController, armies, location);
                    break;
                case "Sage":
                    command = new SearchSageCommand(this.provider.LocationController, armies, location);
                    break;
                case "Temple":
                    command = new SearchTempleCommand(this.provider.LocationController, armies, location);
                    break;
                default:
                    throw new InvalidOperationException("No location to search.");
            }
            this.commandController.AddCommand(command);
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

            this.commandController.AddCommand(
                new BuildCityCommand(this.provider.CityController, city));
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

            this.commandController.AddCommand(
                new RazeCityCommand(this.provider.CityController, city));
        }

        public void TakeItems(Hero hero, List<Artifact> items)
        {
            this.commandController.AddCommand(
                new TakeItemsCommand(this.provider.HeroController, hero, items));
        }

        public void DropItems(Hero hero, List<Artifact> items)
        {
            this.commandController.AddCommand(
                new DropItemsCommand(this.provider.HeroController, hero, items));
        }

        public void NewGame(GameEntity settings)
        {
            var unityGame = UnityUtilities.GameObjectHardFind("UnityManager")
                .GetComponent<UnityManager>();

            this.commandController.AddCommand(
                new NewGameCommand(this.provider.GameController, settings));

            // TODO: Clear the persistence snapshots?
        }

        internal void LoadGame(string filename)
        {
            var unityGame = UnityUtilities.GameObjectHardFind("UnityManager")
                .GetComponent<UnityManager>();
            var snapshot = PersistanceManager.LoadEntities(filename, unityGame);

            this.commandController.AddCommand(
                new LoadGameCommand(this.provider.GameController, snapshot.WismGameEntity));

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