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
            { "Forest", '¶' },
            { "Mountain", '^' },
            { "Grass", '.' },
            { "Water", '~' },
            { "Hill", 'h' },
            { "Marsh", '%' },
            { "Road", '=' },
            { "Bridge", '=' },
            { "Castle", '$' },
            { "Ruins", '¥' },
            { "Temple", '†' },
            { "Tomb", '€' },
            { "Tower", '#' },
            { "Void", 'v' }
        };

        IDictionary<string, ConsoleColor> clanColorsMap = new Dictionary<string, ConsoleColor>
        {
            { "Sirians", ConsoleColor.White },
            { "StormGiants", ConsoleColor.Yellow },
            { "GreyDwarves", ConsoleColor.DarkYellow },
            { "OrcsOfKor", ConsoleColor.Red },
            { "Elvallie", ConsoleColor.Green },
            { "Selentines", ConsoleColor.DarkBlue },
            { "HorseLords", ConsoleColor.Blue },
            { "LordBane", ConsoleColor.DarkRed },
            { "Neutral", ConsoleColor.Gray }
        };

        public AsciiView(ILoggerFactory logFactory, ControllerProvider controllerProvider)
            : base(logFactory, controllerProvider)
        {
            if (logFactory is null)
            {
                throw new ArgumentNullException(nameof(logFactory));
            }

            if (controllerProvider is null)
            {
                throw new ArgumentNullException(nameof(controllerProvider));
            }

            this.logger = logFactory.CreateLogger<AsciiView>();
            this.commandController = controllerProvider.CommandController;
            this.commandProviders = new List<ICommandProvider>()
            {
                new ConsoleCommandProvider(logFactory, 
                controllerProvider.CommandController, controllerProvider.ArmyController, controllerProvider.GameController)
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
            var currentPlayer = Game.Current.GetCurrentPlayer();
            var currentPlayerArmies = currentPlayer.GetArmies();
            var selectedArmies = Game.Current.GetSelectedArmies();
            Tile selectedTile = null;
            ConsoleColor beforeColor;

            if (selectedArmies != null && selectedArmies.Count > 0)
            {
                selectedTile = selectedArmies[0].Tile;
            }
            else if (currentPlayerArmies.Count == 0)
            {
                // Game over
                Console.WriteLine($"{currentPlayer.Clan.DisplayName} is no longer in the fight!");
                System.Environment.Exit(1);
            }

            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("==========================================");
            for (int y = 0; y < World.Current.Map.GetLength(1); y++)
            {
                for (int x = 0; x < World.Current.Map.GetLength(0); x++)
                {                    
                    Tile tile = World.Current.Map[x, y];
                    string terrain = tile.Terrain.ShortName;
                    string army = String.Empty;
                    Clan clan = null;
                    if (tile.HasVisitingArmies())
                    {
                        army = tile.VisitingArmies[0].ShortName;
                        clan = tile.VisitingArmies[0].Clan;
                    }
                    else if (tile.HasArmies())
                    {
                        army = tile.Armies[0].ShortName;
                        clan = tile.Armies[0].Clan;
                    }

                    if (selectedTile == tile)
                    {
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    else
                    {
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }

                    // Location
                    Console.Write($"{tile.X}{tile.Y}");

                    // Terrain
                    if (tile.HasCity())
                    {
                        beforeColor = Console.ForegroundColor;
                        Console.ForegroundColor = GetColorForClan(tile.City.Clan);
                        Console.Write($"{GetTerrainSymbol(terrain)}");
                        Console.ForegroundColor = beforeColor;
                    }
                    else
                    {
                        Console.Write($"{GetTerrainSymbol(terrain)}");
                    }
                    
                    // Army
                    beforeColor = Console.ForegroundColor;
                    Console.ForegroundColor = GetColorForClan(clan);
                    Console.Write($"{GetArmySymbol(army)}");
                    Console.ForegroundColor = beforeColor;
                    
                    // Army Count
                    Console.Write($"{GetArmyCount(tile)}");

                    // Buffer
                    Console.Write("\t");
                }
                Console.WriteLine();                
            }
            Console.WriteLine("==========================================");
        }

        private ConsoleColor GetColorForClan(Clan clan)
        {
            if (clan == null)
            {
                return ConsoleColor.Gray;
            }

            return clanColorsMap.Keys.Contains(clan.ShortName) ? clanColorsMap[clan.ShortName] : ConsoleColor.Gray;
        }

        private static string GetArmyCount(Tile tile)
        {
            int totalArmies = 0;
            if (tile.HasArmies())
            {
                totalArmies = tile.Armies.Count;
            }

            if (tile.HasVisitingArmies())
            {
                totalArmies += tile.VisitingArmies.Count;
            }

            return (totalArmies == 0) ? " " : totalArmies.ToString();
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
