using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Wism.Client.Agent.Commands;
using Wism.Client.Agent.Controllers;
using Wism.Client.Agent.InputProviders;
using Wism.Client.Core;

namespace Wism.Client.Agent
{
    /// <summary>
    /// Basic ASCII Console-based UI for testing
    /// </summary>
    public class AsciiView : ViewBase
    {
        private readonly ILogger logger;
        private readonly CommandController commandController;
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

        public AsciiView(ILoggerFactory logFactory, ArmyController armyController, CommandController commandController)
            : base(logFactory)
        {
            if (logFactory is null)
            {
                throw new ArgumentNullException(nameof(logFactory));
            }

            if (armyController is null)
            {
                throw new ArgumentNullException(nameof(armyController));
            }

            this.logger = logFactory.CreateLogger<AsciiView>();
            this.commandController = commandController ?? throw new ArgumentNullException(nameof(commandController));

            this.commandProviders = new List<ICommandProvider>()
            {
                new ConsoleCommandProvider(logFactory, commandController, armyController),
                new PlayerEvadingAICommandProvider(logFactory, commandController, armyController)
            };
        }

        protected override void DoTasks(ref int lastId)
        {
            Player humanPlayer = Game.Current.Players[0];

            foreach (ArmyCommand command in commandController.GetCommandsAfterId(lastId))
            {
                lastId = command.Id;

                logger.LogInformation($"Task executing: {command.Id}: {command.GetType()}");
                // TODO: Implement tri-state to return success, failure, or incomplete/continue
                // TODO: Switch to returning game state
                var success = command.Execute();

                if (success)
                {
                    logger.LogInformation($"Task successful");
                }
                else if (!success)
                {
                    logger.LogInformation($"Task failed");
                    if (command.Player == humanPlayer)
                    {                 
                        Console.Beep();
                    }
                }
            }
        }

        protected override void HandleInput()
        {
            foreach (ICommandProvider provider in this.commandProviders)
            {
                provider.GenerateCommands();
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
                    {
                        unit = tile.Armies[0].ShortName;
                    }

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
