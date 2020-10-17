using AutoMapper;
using BranallyGames.Wism;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Wism.Client.Api;
using Wism.Client.Api.Controllers;
using Wism.Client.Model;
using Wism.Client.Model.Commands;

namespace Wism.Client.AsciiUi
{
    /// <summary>
    /// Basic ASCII Console-based UI for testing
    /// </summary>
    public class WismAsciiView : WismViewBase
    {
        private readonly ILogger logger;
        private readonly CommandController commandController;
        private readonly IMapper mapper;
        private int lastId = 0;
        private Army selectedArmy;

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
            : base(logFactory)
        {
            this.logger = logFactory.CreateLogger<WismAsciiView>();
            this.commandController = commandController ?? throw new ArgumentNullException(nameof(commandController));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        private void GameOver()
        {
            // You have lost!
            Console.WriteLine("Your hero has died and you have lost!");
            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();
        }

        private char GetTerrainSymbol(string terrain)
        {
            return (terrainMap.Keys.Contains(terrain)) ? terrainMap[terrain] : '?';
        }

        private char GetUnitSymbol(string unit)
        {
            return (unitMap.Keys.Contains(unit)) ? unitMap[unit] : ' ';
        }

        protected override void DoTasks(ref int lastId)
        {
            logger.LogInformation("Doing tasks");
            System.Threading.Thread.Sleep(1000);
        }

        protected override void HandleInput()
        {
            Console.Write("Enter a command: ");
            var keyInfo = Console.ReadKey();
            var army = mapper.Map<ArmyDto>(this.selectedArmy);
            
            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    commandController.AddCommand(new ArmyMoveCommandDto()
                    {
                        Army = army,
                        X = army.X,
                        Y = army.Y - 1
                    });
                    break;
                case ConsoleKey.DownArrow:
                    commandController.AddCommand(new ArmyMoveCommandDto()
                    {
                        Army = army,
                        X = army.X,
                        Y = army.Y + 1
                    });
                    break;
                case ConsoleKey.LeftArrow:
                    commandController.AddCommand(new ArmyMoveCommandDto()
                    {
                        Army = army,
                        X = army.X - 1,
                        Y = army.Y
                    });
                    break;
                case ConsoleKey.RightArrow:
                    commandController.AddCommand(new ArmyMoveCommandDto()
                    {
                        Army = army,
                        X = army.X + 1,
                        Y = army.Y
                    });
                    break;
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

        private void SetupWorld()
        {
            World.CreateDefaultWorld();
            World.Current.Players[0].HireHero(World.Current.Map[2, 2]);
            World.Current.Players[1].ConscriptArmy(ModFactory.FindUnitInfo("LightInfantry"), World.Current.Map[1, 1]);
            this.selectedArmy = World.Current.Players[0].GetArmies()[0];
        }
    }
}
