using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Api;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.War;
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
        public const int MaxArmysPerArmy = Army.MaxArmies;
        public static readonly string DefaultModPath = @"Assets\Scripts\Core\netstandard2.0\mod";        
        public static readonly string DefaultWorld = @"Illuria";
        public static readonly string DefaultCityModPath = @$"{DefaultModPath}\{ModFactory.WorldsPath}\{DefaultWorld}";

        // Controllers for the WISM Client API
        private ControllerProvider provider;
        private CommandController commandController;

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
                HeroController = new HeroController(LoggerFactory)
            };
            commandController = provider.CommandController;            
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

        public void StartTurn(Player player)
        {
            commandController.AddCommand(
                new StartTurnCommand(provider.GameController, player));
        }

        public void EndTurn()
        {
            commandController.AddCommand(
                new EndTurnCommand(provider.GameController, Game.Current.GetCurrentPlayer()));
            StartTurn();
        }
    }
}