using System;
using System.Collections.Generic;
using Wism.Client.Agent.CommandProcessors;
using Wism.Client.Agent.UI;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.CommandProviders;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Agent
{
    /// <summary>
    /// Basic ASCII Console-based UI for testing
    /// </summary>
    public class AsciiGame : GameBase
    {
        private readonly ILogger logger;
        private readonly CommandController commandController;
        private readonly List<ICommandProvider> commandProviders;
        private readonly List<ICommandProcessor> commandProcessors;

        public AsciiGame(ILoggerFactory logFactory, ControllerProvider controllerProvider)
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

            this.logger = logFactory.CreateLogger();
            this.commandController = controllerProvider.CommandController;
            this.commandProviders = new List<ICommandProvider>()
            {
                new ConsoleCommandProvider(logFactory, controllerProvider)
            };
            this.commandProcessors = new List<ICommandProcessor>()
            {
                new PrepareForBattleProcessor(logFactory, this),
                new BattleProcessor(logFactory),
                new CompleteBattleProcessor(logFactory, this),
                new StandardProcessor(logFactory)
            };
        }

        protected override void DoTasks(ref int lastId)
        {
            foreach (Command command in commandController.GetCommandsAfterId(lastId))
            {
                logger.LogInformation($"Task executing: {command.Id}: {command.GetType()}");

                // Run the command
                var result = ActionState.NotStarted;
                foreach (var processor in this.commandProcessors)
                {
                    if (processor.CanExecute(command))
                    {
                        result = processor.Execute(command);
                        break;
                    }
                }

                // Process the result
                if (result == ActionState.Succeeded)
                {
                    logger.LogInformation($"Task successful");
                    lastId = command.Id;
                }
                else if (result == ActionState.Failed)
                {
                    logger.LogInformation($"Task failed");
                    lastId = command.Id;
                }
                else if (result == ActionState.InProgress)
                {
                    logger.LogInformation("Task started and in progress");
                    // Do NOT advance Command ID
                    break;
                }
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

            // Attack cut scene is handled by attack processors
            if (Game.Current.GameState == GameState.AttackingArmy ||
                Game.Current.GameState == GameState.CompletedBattle)
            {
                return;
            }

            // Draw standard map
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
                        Console.ForegroundColor = AsciiMapper.GetColorForClan(tile.City.Clan);
                        Console.Write($"{AsciiMapper.GetTerrainSymbol(terrain)}");
                        Console.ForegroundColor = beforeColor;
                    }
                    else
                    {
                        Console.Write($"{AsciiMapper.GetTerrainSymbol(terrain)}");
                    }
                    
                    // Army
                    beforeColor = Console.ForegroundColor;
                    Console.ForegroundColor = AsciiMapper.GetColorForClan(clan);
                    Console.Write($"{AsciiMapper.GetArmySymbol(army)}");
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
    }
}
