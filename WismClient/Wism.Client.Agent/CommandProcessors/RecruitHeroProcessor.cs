using System;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;

namespace Wism.Client.Agent.CommandProcessors
{
    public class RecruitHeroProcessor : ICommandProcessor
    {
        private ILogger logger;
        private readonly AsciiGame asciiGame;

        public RecruitHeroProcessor(ILoggerFactory loggerFactory, AsciiGame asciiGame)
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
            return command is RecruitHeroCommand;
        }

        public ActionState Execute(ICommandAction command)
        {
            ActionState state = ActionState.Failed;

            var recruitCommand = (RecruitHeroCommand)command;
            var player = recruitCommand.Player;
            if (player.IsDead)
            {
                return ActionState.Failed;
            }

            if (recruitCommand.Result == ActionState.NotStarted)
            {
                // Find's a hero if one is available
                state = command.Execute();
            }

            if (state == ActionState.Succeeded)
            {
                // Here is available; offer to player if enough money
                if (player.Gold >= recruitCommand.HeroPrice)
                {
                    state = OfferHeroToPlayer(recruitCommand);
                }
                else
                {
                    // Not enough money
                    state = ActionState.Failed;
                }
            }

            return state;
        }

        private ActionState OfferHeroToPlayer(RecruitHeroCommand command)
        {
            ActionState state;

            var player = command.Player;
            var city = command.HeroTile.City;
            var gold = command.HeroPrice;

            Notify.Information($"A hero in {city.DisplayName} offers to join you for {gold} gp!");
            Notify.Information($"You have {player.Gold} gp.");
            Notify.Information($"[A]ccept or [r]eject?");
            var key = Console.ReadKey();
            if (key.Key != ConsoleKey.A)
            {

                command.HeroAccepted = false;
                state = ActionState.Failed;
            }
            else
            {
                command.HeroAccepted = true;
                state = ActionState.Succeeded;
            }
            Console.WriteLine();            

            return state;
        }
    }
}
