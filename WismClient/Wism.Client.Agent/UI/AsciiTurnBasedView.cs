using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Wism.Client.Agent.Commands;
using Wism.Client.Agent.Controllers;
using Wism.Client.Agent.CommandProviders;
using Wism.Client.Core;

namespace Wism.Client.Agent
{
    /// <summary>
    /// Basic ASCII Console-based UI for testing
    /// </summary>
    public class AsciiTurnBasedView : ViewBase
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

        public AsciiTurnBasedView(ILoggerFactory logFactory, ArmyController armyController, CommandController commandController)
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
                new ConsoleTurnBasedCommandProvider(logFactory, commandController, armyController)
            };
        }

        protected override void DoTasks(ref int lastId)
        {
            Player humanPlayer = Game.Current.Players[0];

            foreach (Command command in commandController.GetCommandsAfterId(lastId))
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
            var selectedArmies = Game.Current.GetSelectedArmies();
            Tile selectedTile = null;
            if (selectedArmies != null)
            {
                selectedTile = selectedArmies[0].Tile;
            }

            Console.WriteLine("=========================================================================================");
            for (int y = 0; y < World.Current.Map.GetLength(1); y++)
            {
                for (int x = 0; x < World.Current.Map.GetLength(0); x++)
                {
                    Tile tile = World.Current.Map[x, y];
                    string terrain = tile.Terrain.ShortName;
                    string army = String.Empty;
                    if (tile.HasVisitingArmies())
                    {
                        army = tile.VisitingArmies[0].ShortName;
                    }
                    else if (tile.HasArmies())
                    {
                        army = tile.Armies[0].ShortName;
                    }

                    string format = "({0},{1}):[{2},{3}]\t";
                    if (selectedTile == tile)
                    {
                        format = "({0},{1}):{{{2},{3}}}\t";
                    }

                    Console.Write(format,
                        tile.X,
                        tile.Y,
                        GetTerrainSymbol(terrain),
                        GetArmySymbol(army));
                }
                Console.WriteLine();                
            }
            Console.WriteLine("=========================================================================================");
        }

        private char GetTerrainSymbol(string terrain)
        {
            return (terrainMap.Keys.Contains(terrain)) ? terrainMap[terrain] : '?';
        }

        private char GetArmySymbol(string unit)
        {
            return (armyMap.Keys.Contains(unit)) ? armyMap[unit] : ' ';
        }
    }
}
