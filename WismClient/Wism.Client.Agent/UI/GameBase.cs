using System;
using System.Threading.Tasks;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.Modules;

namespace Wism.Client.Agent;

/// <summary>
///     Template base class for a generic UI
/// </summary>
public abstract class GameBase
{
    public const int DefaultGameSpeed = 100;
    public const int DefaultAttackSpeed = 750;
    private readonly ArmyController armyController;
    private readonly ILogger logger;
    private int lastId;

    public GameBase(ILoggerFactory loggerFactory, ControllerProvider controllerProvider)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        if (controllerProvider is null)
        {
            throw new ArgumentNullException(nameof(controllerProvider));
        }

        this.logger = loggerFactory.CreateLogger();
        this.armyController = controllerProvider.ArmyController;
        this.PlayerController = controllerProvider.PlayerController;
        this.GameSpeed = DefaultGameSpeed;
    }

    public int GameSpeed { get; set; }

    public int LastId
    {
        get => this.lastId;
        set => this.lastId = value;
    }

    public PlayerController PlayerController { get; }

    public async Task RunAsync()
    {
        this.logger.LogInformation("WISM successfully started");

        try
        {
            this.CreateTestGame();

            while (true)
            {
                // Game loop
                this.Draw();
                this.HandleInput();
                this.DoTasks(ref this.lastId);

                await Task.Delay(this.GameSpeed);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            this.logger.LogError(ex.ToString());
            throw;
        }
    }

    protected abstract void DoTasks(ref int lastId);

    protected abstract void HandleInput();

    protected abstract void Draw();

    /// <summary>
    ///     For testing purposes only. Creates a default world for testing.
    /// </summary>
    private void CreateTestGame()
    {
        var worldName = "AsciiWorld";

        Game.CreateDefaultGame(worldName);
        var world = World.Current;
        var map = world.Map;

        // Some walking around money
        Game.Current.Players[0].Gold = 2000;

        // Create a default hero for testing
        var heroTile = map[1, 1];
        Game.Current.Players[0].HireHero(heroTile);
        Game.Current.Players[0].ConscriptArmy(
            ModFactory.FindArmyInfo("HeavyInfantry"),
            heroTile);
        Game.Current.Players[0].ConscriptArmy(
            ModFactory.FindArmyInfo("Pegasus"),
            heroTile);

        // Set the player's selected army to a default for testing
        this.armyController.SelectArmy(heroTile.Armies);

        // Create an opponent for testing
        var enemyTile1 = map[3, 3];
        Game.Current.Players[1].HireHero(enemyTile1);
        Game.Current.Players[1].ConscriptArmy(
            ModFactory.FindArmyInfo("LightInfantry"),
            enemyTile1);
        Game.Current.Players[1].ConscriptArmy(
            ModFactory.FindArmyInfo("LightInfantry"),
            enemyTile1);
        Game.Current.Players[1].ConscriptArmy(
            ModFactory.FindArmyInfo("LightInfantry"),
            enemyTile1);
        Game.Current.Players[1].ConscriptArmy(
            ModFactory.FindArmyInfo("LightInfantry"),
            enemyTile1);

        var enemyTile2 = map[3, 2];
        Game.Current.Players[1].ConscriptArmy(
            ModFactory.FindArmyInfo("LightInfantry"),
            enemyTile2);

        // Add cities and locations
        MapBuilder.AddCitiesFromWorldPath(world, worldName);
        MapBuilder.AddLocationsFromWorldPath(world, worldName);
        MapBuilder.AllocateBoons(world.GetLocations());
    }
}