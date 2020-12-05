using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Wism.Client.Agent.Commands;
using Wism.Client.Agent.Controllers;
using Wism.Client.Agent.CommandProviders;
using Wism.Client.Core;
using System.Linq;

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

        public AsciiTurnBasedView(ILoggerFactory logFactory, ArmyController armyController, CommandController commandController, GameController gameController)
            : base(logFactory, armyController)
        {
            if (logFactory is null)
            {
                throw new ArgumentNullException(nameof(logFactory));
            }

            if (armyController is null)
            {
                throw new ArgumentNullException(nameof(armyController));
            }

            if (gameController is null)
            {
                throw new ArgumentNullException(nameof(gameController));
            }

            this.logger = logFactory.CreateLogger<AsciiView>();
            this.commandController = commandController ?? throw new ArgumentNullException(nameof(commandController));
            this.commandProviders = new List<ICommandProvider>()
            {
                new ConsoleTurnBasedCommandProvider(logFactory, commandController, armyController, gameController)
            };
        }

        protected override void DoTasks(ref int lastId)
        {
            Player humanPlayer = Game.Current.Players[0];

            foreach (Command command in commandController.GetCommandsAfterId(lastId))
            {
                logger.LogInformation($"Task executing: {command.Id}: {command.GetType()}");

                // Run the command
                var result = command.Execute();

                // Process the result
                if (result == ActionState.Succeeded)
                {
                    logger.LogInformation($"Task successful");
                    lastId = command.Id;
                    UpdateGameState();
                }
                else if (result == ActionState.Failed)
                {
                    logger.LogInformation($"Task failed");
                    lastId = command.Id;
                    if (command.Player == humanPlayer)
                    {                 
                        Console.Beep();
                    }
                    UpdateGameState();
                }
                else if (result == ActionState.InProgress)
                {
                    logger.LogInformation("Task started and in progress");                    
                    Game.Current.Transition(GameState.MovingArmy);

                    // Do not advance Command ID as we are still processing this command
                }
            }
        }

        private static void UpdateGameState()
        {
            var player1Armies = Game.Current.GetCurrentPlayer().GetArmies();
            if (player1Armies == null || player1Armies.Count == 0)
            {
                Game.Current.Transition(GameState.GameOver);
            }
            else if (Game.Current.GetSelectedArmies() == null ||
                     Game.Current.GetSelectedArmies().Count == 0 ||
                     Game.Current.GetSelectedArmies().Any(a => a.MovesRemaining == 0))
            {
                Game.Current.Transition(GameState.Ready);
            }
            else
            {
                Game.Current.Transition(GameState.SelectedArmy);
            }
        }

        protected override void HandleInput()
        {
            if ((Game.Current.GameState != GameState.Ready) &&
                (Game.Current.GameState != GameState.SelectedArmy))
            {
                // Do not solicit additional input
                return;
            }

            foreach (ICommandProvider provider in this.commandProviders)
            {
                provider.GenerateCommands();
            }
        }

        protected override void Draw()
        {
            var currentPlayerArmies = Game.Current.GetCurrentPlayer().GetArmies();
            var selectedArmies = Game.Current.GetSelectedArmies();
            Tile selectedTile = null;

            if (selectedArmies != null && selectedArmies.Count > 0)
            {
                selectedTile = selectedArmies[0].Tile;
            }
            else if (currentPlayerArmies.Count == 0)
            {
                // Game over
                Console.WriteLine("You have lost.");
                System.Environment.Exit(1);
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

        private char GetArmySymbol(string army)
        {
            return (armyMap.Keys.Contains(army)) ? armyMap[army] : ' ';
        }
    }
}
