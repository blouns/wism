using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wism.Client.Api.CommandPublisher;
using Wism.Client.CommandProcessors;
using Wism.Client.CommandProviders;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;

namespace Wism.Client.Agent;

/// <summary>
///     Template base class for a generic UI
/// </summary>
public abstract class GameBase
{
    public const int DefaultGameSpeed = 100;
    public const int DefaultAttackSpeed = 750;
    private readonly IWismLogger logger;
    private readonly IWismLoggerFactory loggerFactory;
    private readonly ControllerProvider controllerProvider;
    private readonly ICommandProcessor commandProcessor;
    private readonly CommandIpcPublisher commandIpcPublisher;
    private int lastId;
    private Dictionary<Player, ICommandProvider> playerCommandersDictionary;

    public GameBase(
        IWismLoggerFactory loggerFactory, 
        ControllerProvider controllerProvider, 
        ICommandProcessor commandProcessor,
        CommandIpcPublisher commandIpcPublisher)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        if (controllerProvider is null)
        {
            throw new ArgumentNullException(nameof(controllerProvider));
        }

        if (commandProcessor is null)
        {
            throw new ArgumentNullException(nameof(commandProcessor));
        }

        this.logger = loggerFactory.CreateLogger();
        this.PlayerController = controllerProvider.PlayerController;
        this.GameSpeed = DefaultGameSpeed;
        this.loggerFactory = loggerFactory;
        this.controllerProvider = controllerProvider;
        this.commandProcessor = commandProcessor;
        this.commandIpcPublisher = commandIpcPublisher;
    }

    public int GameSpeed { get; set; }

    public int LastId
    {
        get => this.lastId;
        set => this.lastId = value;
    }

    public PlayerController PlayerController { get; }
    public Dictionary<Player, ICommandProvider> PlayerCommanders { get => playerCommandersDictionary; set => playerCommandersDictionary = value; }

    public IWismLoggerFactory LoggerFactory => loggerFactory;

    public ControllerProvider ControllerProvider => controllerProvider;

    public ICommandProcessor CommandProcessor => commandProcessor;

    public CommandIpcPublisher CommandIpcPublisher => commandIpcPublisher;

    public async Task RunAsync()
    {
        this.logger.LogInformation("WISM successfully started");

        try
        {
            this.CreateGame();

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

    protected abstract void CreateGame();
}