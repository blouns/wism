using System;
using System.Collections.Generic;
using Wism.Client.Api.CommandProviders;
using Wism.Client.Api.Commands;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.Common;
using System.Threading;
using Wism.Client.MapObjects;

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
            { "Pegasus", 'P' },
            { "WolfRiders", 'R' },
            { "GiantWarriors", 'R' },
            { "Archers", 'R' }
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

            this.logger = logFactory.CreateLogger();
            this.commandController = controllerProvider.CommandController;
            this.commandProviders = new List<ICommandProvider>()
            {
                new ConsoleCommandProvider(logFactory, controllerProvider)
            };
        }

        protected override void DoTasks(ref int lastId)
        {
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
                }
                else if (result == ActionState.Failed)
                {
                    logger.LogInformation($"Task failed");
                    lastId = command.Id;
                }
                else if (result == ActionState.InProgress)
                {
                    logger.LogInformation("Task started and in progress");
                    // Do NOT advance Command ID; we are still processing this command
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

            if (Game.Current.GameState == GameState.AttackingArmy)
            {
                DoBattleCutScene();
                if (Game.Current.ArmiesSelected())
                {
                    selectedTile = selectedArmies[0].Tile;
                }
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

        private void DoBattleCutScene()
        {
            var battleCommand = (PrepareForBattleCommand)this.commandController.
                GetCommand(LastId);

            var targetTile = World.Current.Map[battleCommand.X, battleCommand.Y];
            var attackingPlayer = battleCommand.Armies[0].Player;
            var attackingArmies = new List<Army>(battleCommand.Armies);
            attackingArmies.Sort(new ByArmyBattleOrder(targetTile));

            var defendingPlayer = battleCommand.Defenders[0].Player;
            var defendingArmies = targetTile.MusterArmy();
            defendingArmies.Sort(new ByArmyBattleOrder(targetTile));

            // Draw the initial battle
            DrawBattleSetupSequence(attackingPlayer, defendingPlayer);            

            var result = ActionState.NotStarted;
            var command = this.commandController.GetCommand(++LastId);

            while (result == ActionState.NotStarted ||
                   result == ActionState.InProgress)
            {
                DrawBattleUpdate(attackingPlayer.Clan, attackingArmies, defendingPlayer.Clan, defendingArmies);

                Thread.Sleep(750);

                // Attack                             
                result = command.Execute();
            }

            // Battle outcome
            DrawBattleUpdate(attackingPlayer.Clan, attackingArmies, defendingPlayer.Clan, defendingArmies);

            var name = attackingPlayer.Clan.DisplayName;
            var presentVerb = name.EndsWith('s') ? "are" : "is";
            var pastVerb = name.EndsWith('s') ? "have" : "has";

            if (result == ActionState.Succeeded)
            {
                Console.WriteLine($"{name} {presentVerb} victorious!");
            }
            else if (result == ActionState.Failed)
            {
                Console.WriteLine($"{name} {pastVerb} been defeated!");
            }
            else
            {
                Console.WriteLine("Error: Unexpected game state" + result);
            }
            Console.ReadKey();
        }

        private void DrawBattleUpdate(Clan attackingClan, List<Army> attackingArmies, Clan defendingClan, List<Army> defendingArmies)
        {
            var color = Console.ForegroundColor;
            Console.Clear();

            Console.ForegroundColor = GetColorForClan(defendingClan);
            Console.WriteLine($"{defendingClan.DisplayName}:");
            DrawArmies(defendingArmies);

            Console.WriteLine();

            Console.ForegroundColor = GetColorForClan(attackingClan);
            Console.WriteLine($"{attackingClan.DisplayName}:");
            DrawArmies(attackingArmies);

            Console.ForegroundColor = color;
            Console.Beep();
        }

        private void DrawArmies(List<Army> armies)
        {
            var originalColor = Console.ForegroundColor;

            foreach (var army in armies)
            {
                Console.ForegroundColor = GetColorForClan(army.Clan);

                Console.Write(army.DisplayName);
                if (army.IsDead)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(" [X]");
                }
                Console.WriteLine();
            }

            Console.ForegroundColor = originalColor;
        }

        private void DrawBattleSetupSequence(Player attacker, Player defender)
        {
            Console.Clear();
            Console.WriteLine("War! ...in a senseless mind.");
            Console.WriteLine($"{attacker.Clan.DisplayName} is attacking {defender.Clan.DisplayName}!");
            for (int i = 0; i < 3; i++)
            {
                Console.Beep();
                Thread.Sleep(750);
            }
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
