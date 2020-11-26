using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Wism.Client.Agent;
using Wism.Client.Agent.Commands;
using Wism.Client.Agent.Controllers;
using Wism.Client.Agent.InputProviders;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent
{
    /// <summary>
    /// Basic ASCII Console-based UI for testing
    /// </summary>
    public class WismAsciiView : WismViewBase
    {
        private readonly ILogger logger;
        private readonly CommandController commandController;
        private readonly ArmyController armyController;
        private readonly List<ICommandProvider> commandProviders;

        IDictionary<string, char> armyMap = new Dictionary<string, char>
        {
            { "Hero", 'H' },
            { "LightInfantry", 'i' },
            { "HeavyInfantry", 'I' },
            { "Cavalry", 'c' },
            { "Pegasus", 'P' }
        };

        IDictionary<string, char> terrainMap = new Dictionary<string, char>
        {
            { "Forest", 'F' },
            { "Mountain", 'M' },
            { "Grass", 'G' },
            { "Water", 'W' },
            { "Hill", 'h' },
            { "Marsh", 'm' },
            { "Road", 'R' },
            { "Bridge", 'B' },
            { "Castle", 'C' },
            { "Ruins", 'r' },
            { "Temple", 'T' },
            { "Tomb", 't' },
            { "Tower", 'K' },
            { "Void", 'v' }
        };

        public WismAsciiView(ILoggerFactory logFactory, ArmyController armyController, CommandController commandController)
            : base(logFactory)
        {
            if (logFactory is null)
            {
                throw new ArgumentNullException(nameof(logFactory));
            }

            this.logger = logFactory.CreateLogger<WismAsciiView>();
            this.commandController = commandController ?? throw new ArgumentNullException(nameof(commandController));
            this.armyController = armyController ?? throw new ArgumentNullException(nameof(armyController));

            this.commandProviders = new List<ICommandProvider>()
            {
                new ConsoleCommandProvider(logFactory, commandController),
                new PlayerEvadingAICommandProvider(logFactory, commandController)
            };
        }

        protected override void DoTasks(ref int lastId)
        {
            if (this.selectedArmies == null)
            {
                // You have lost!
                Console.WriteLine("Your hero has died and you have lost!");
                Console.WriteLine("Press any key to quit...");
                Console.ReadKey();
                return;
            }

            foreach (Command command in commandController.GetCommandsAfterId(lastId))
            {
                logger.LogInformation($"Executing Task: {command.Id}: {command.GetType().ToString()}");
                lastId = command.Id;

                // TODO: Move to IAction
                if (command is MoveCommand)
                {
                    MoveCommand armyMoveCommand = (MoveCommand)command;

                    // Get the armies to move
                    var army = FindArmyById(armyMoveCommand.Army.Id);
                    var armies = new List<Army>() { army };

                    // Get the destination tile
                    Tile targetTile = World.Current.Map[armyMoveCommand.X, armyMoveCommand.Y];

                    if (!armyController.TryMove(armies, targetTile))
                    {
                        Console.WriteLine("Cannot move there.");
                        Console.Beep();
                    }
                }
            }
        }

        private Army FindArmyById(int id)
        {
            foreach (var player in Game.Current.Players)
            {
                foreach (var army in player.GetArmies())
                {
                    if (army.Id == id)
                    {
                        return army;
                    }
                }
            }

            throw new ArgumentOutOfRangeException(nameof(id), "Army could not be found.");
        }

        protected override void HandleInput()
        {
            foreach (ICommandProvider provider in this.commandProviders)
            {
                provider.Produce();
            }
        }

        protected override void Draw()
        {
            Console.Clear();
            for (int y = 0; y < World.Current.Map.GetLength(1); y++)
            {
                for (int x = 0; x < World.Current.Map.GetLength(0); x++)
                {
                    Tile tile = World.Current.Map[x, y];
                    string terrain = tile.Terrain.ShortName;
                    string unit = String.Empty;
                    if (tile.HasArmies())
                        unit = tile.Armies[0].ShortName;

                    Console.Write("({0},{1}):[{2},{3}]\t",
                        tile.X,
                        tile.Y,
                        GetTerrainSymbol(terrain),
                        GetUnitSymbol(unit));
                }
                Console.WriteLine();
            }
        }

        private char GetTerrainSymbol(string terrain)
        {
            return (terrainMap.Keys.Contains(terrain)) ? terrainMap[terrain] : '?';
        }

        private char GetUnitSymbol(string unit)
        {
            return (armyMap.Keys.Contains(unit)) ? armyMap[unit] : ' ';
        }
    }
}
