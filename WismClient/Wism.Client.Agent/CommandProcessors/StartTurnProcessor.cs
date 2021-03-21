using System;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;
using Wism.Client.Core.Heros;

namespace Wism.Client.Agent.CommandProcessors
{
    public class StartTurnProcessor : ICommandProcessor
    {
        private ILogger logger;
        private readonly AsciiGame asciiGame;
        private readonly IRecruitHeroStrategy recruitingStrategy = new DefaultRecruitHeroStrategy();

        public StartTurnProcessor(ILoggerFactory loggerFactory, AsciiGame asciiGame)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.asciiGame = asciiGame ?? throw new ArgumentNullException(nameof(asciiGame));
        }

        public bool CanExecute(ICommandAction command)
        {
            return command is StartTurnCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            var state = command.Execute();

            var startTurnCommand = (StartTurnCommand)command;
            var player = startTurnCommand.Player;
            Notify.DisplayAndWait($"{player.Clan.DisplayName} turn is starting...");
            
            if (player.Gold >= player.NewHeroPrice)
            {
                OfferHeroToPlayer(player);
            }

            return state;
        }

        private ActionState OfferHeroToPlayer(Core.Player player)
        {
            var city = recruitingStrategy.GetTargetCity(player);
            var gold = player.NewHeroPrice;

            Notify.Information($"A hero in {city.DisplayName} offers to join you for {gold} gp!");
            Notify.Information($"You have {player.Gold} gp.");
            Notify.Information($"[A]ccept or [r]eject?");
            var key = Console.ReadKey();
            if (key.Key != ConsoleKey.A)
            {
                Console.WriteLine();
                return ActionState.Failed;
            }
            Console.WriteLine();
            
            // You're hired!
            var hero = player.HireHero(city.Tile, player.NewHeroPrice);

            // Name the hero
            Notify.Information($"Enter a name [Default: {hero.DisplayName}]:");
            string name = Console.ReadLine();
            if (!String.IsNullOrWhiteSpace(name))
            {
                hero.DisplayName = name;
            }

            // Check for any allies the hero brought with them
            var allies = recruitingStrategy.GetAllies(player);
            if (allies.Count > 0)
            {
                Notify.DisplayAndWait($"And the hero brings {allies.Count} allies!");
                foreach (var armyInfo in allies)
                {
                    player.ConscriptArmy(armyInfo, city.Tile);
                }
            }            

            return ActionState.Succeeded;
        }
    }
}
