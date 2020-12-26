using Assets.Scripts.Wism;
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

/// <summary>
/// The Game Manager is the bridge betweeen Wism.Client and Unity. 
/// It contains the controller interface for manipulating model.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Game setup defaults
    private const int DefaultRandom = 1990;
    public const float StandardTime = 0.25f;
    public const float WarTime = 1.0f;    
    public const int MaxArmysPerArmy = Army.MaxArmies;
    public static readonly string DefaultModPath = @"Assets\Scripts\Core\mod";

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
            GameController = new GameController(LoggerFactory)
        };
        commandController = provider.CommandController;

        // Set up the Game
        MapBuilder.Initialize(DefaultModPath);
        Game.CreateEmpty();        
        Game.Current.Random = new System.Random(DefaultRandom);
        Game.Current.Players = ReadyPlayers();
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
    
    public void AttackWithSelectedArmies(int x, int y)
    {
        commandController.AddCommand(
            new AttackOnceCommand(provider.ArmyController, Game.Current.GetSelectedArmies(), x, y));
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
        commandController.AddCommand(
            new StartTurnCommand(provider.GameController));
    }

    public void EndTurn()
    {
        commandController.AddCommand(
            new EndTurnCommand(provider.GameController));
    }

    private List<Player> ReadyPlayers()
    {
        Game.Current.Players = new List<Player>();

        // Ready Player One
        ClanInfo clanInfo = ClanInfo.GetClanInfo("Sirians");
        Clan clan = Clan.Create(clanInfo);
        Player player1 = Player.Create(clan);
        Game.Current.Players.Add(player1);
        player1.Clan.IsHuman = true;

        // Ready Player Two
        clanInfo = ClanInfo.GetClanInfo("StormGiants");
        clan = Clan.Create(clanInfo);
        Player player2 = Player.Create(clan);
        Game.Current.Players.Add(player2);
        player2.Clan.IsHuman = false;

        return Game.Current.Players;
    }
}

