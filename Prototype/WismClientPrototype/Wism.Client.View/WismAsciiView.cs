using AutoMapper;
using BranallyGames.Wism;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Wism.Client.Agent.Controllers;
using Wism.Client.Agent.InputProviders;
using Wism.Client.Model.Commands;

namespace Wism.Client.View
{
    /// <summary>
    /// Basic ASCII Console-based UI for testing
    /// </summary>
    public class WismAsciiView : WismViewBase
    {
        private readonly ILogger logger;
        private readonly CommandController commandController;
        private readonly IMapper mapper;
        private readonly List<IInputProvider> inputProviders;

        IDictionary<string, char> unitMap = new Dictionary<string, char>
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

        public WismAsciiView(ILoggerFactory logFactory, CommandController commandController, IMapper mapper)
            : base(logFactory, commandController, mapper)
        {
            if (logFactory is null)
            {
                throw new ArgumentNullException(nameof(logFactory));
            }

            if (mapper is null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            this.logger = logFactory.CreateLogger<WismAsciiView>();
            this.commandController = commandController ?? throw new ArgumentNullException(nameof(commandController));

            this.inputProviders = new List<IInputProvider>()
            {
                new ConsoleInputProvider(logFactory, commandController, mapper),
                new PlayerEvadingAIProvider(logFactory, commandController, mapper)
            };
        }

        protected override void DoTasks(ref int lastId)
        {
            if (this.selectedArmy == null)
            {
                // You have lost!
                Console.WriteLine("Your hero has died and you have lost!");
                Console.WriteLine("Press any key to quit...");
                Console.ReadKey();
                return;
            }

            foreach (CommandDto command in commandController.GetCommandsAfterId(lastId))
            {
                logger.LogInformation($"Executing Task: {command.Id}: {command.GetType().ToString()}");
                lastId = command.Id;

                // TODO: Move to IAction
                if (command is MoveCommandDto)
                {
                    MoveCommandDto armyMoveCommand = (MoveCommandDto)command;
                    // REVIEW: Should we instead use the mapper (ArmyDto --> Army)?
                    var army = FindArmyByGuid(armyMoveCommand.Army.Guid);
                    if (!army.TryMove(new Coordinates(armyMoveCommand.X, armyMoveCommand.Y)))
                    {
                        Console.WriteLine("Cannot move there.");
                        Console.Beep();
                    }
                }
            }
        }

        private Army FindArmyByGuid(Guid guid)
        {
            foreach (Player player in World.Current.Players)
            {
                foreach (Army army in player.GetArmies())
                {
                    if (army.Guid == guid)
                    {
                        return army;
                    }
                }
            }

            throw new ArgumentOutOfRangeException(nameof(guid), "Army could not be found.");
        }

        protected override void HandleInput()
        {
            foreach (IInputProvider provider in this.inputProviders)
            {
                provider.ProcessInput();
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
                    string terrain = tile.Terrain.ID;
                    string unit = String.Empty;
                    if (tile.Army != null)
                        unit = tile.Army.ID;

                    Console.Write("{0}:[{1},{2}]\t",
                        tile.Coordinates.ToString(),
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
            return (unitMap.Keys.Contains(unit)) ? unitMap[unit] : ' ';
        }
    }
}
