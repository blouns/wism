using System;
using System.Collections.Generic;
using Wism.Client.Api.CommandProcessors;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Controllers;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.CommandProcessors
{
    public class SearchRuinsProcessor : ICommandProcessor
    {
        private ILogger logger;
        private readonly AsciiGame asciiGame;
        private readonly List<IBoonIdentfier> boonIdentifiers;

        public SearchRuinsProcessor(ILoggerFactory loggerFactory, AsciiGame asciiGame)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
            this.asciiGame = asciiGame ?? throw new ArgumentNullException(nameof(asciiGame));
            this.boonIdentifiers = new List<IBoonIdentfier>()
            {
                new AlliesBoonIdentifier(),
                new AltarBoonIdentifier(),
                new ArtifactBoonIdentifier(),
                new GoldBoonIdentifier(),
            };
        }

        public bool CanExecute(ICommandAction command)
        {
            return command is SearchRuinsProcessor;
        }

        public ActionState Execute(ICommandAction command)
        {
            var ruinsCommand = (SearchRuinsCommand)command;
            var targetTile = World.Current.Map[ruinsCommand.Location.X, ruinsCommand.Location.Y];
            var searchingPlayer = ruinsCommand.Armies[0].Player;
            var searchingArmies = new List<Army>(ruinsCommand.Armies);
            var location = targetTile.Location;

            if (location == null)
            {
                throw new InvalidOperationException("No location found on this tile: " + targetTile);
            }

            var hero = searchingArmies.Find(a => 
                a is Hero && 
                a.Tile == targetTile &&
                a.MovesRemaining > 0);

            if (hero == null)
            {
                Console.WriteLine("You have found nothing!");
                Console.ReadKey();
                return ActionState.Failed;
            }

            if (location.HasMonster())
            {
                Console.WriteLine($"{0} encounters a {1}...");
                Console.ReadKey();
            }            

            // Search the ruins
            var result = ruinsCommand.Execute();

            if (result == ActionState.Succeeded)
            {
                Console.WriteLine("...and is victorious!");
                Console.ReadKey();

                DisplayBoon(ruinsCommand.Boon);
            }
            else if (result == ActionState.Failed &&
                     hero.IsDead)
            {
                Console.WriteLine("...and is slain!");                
            }
            else
            {
                Console.WriteLine("You have found nothing!");                
            }

            Console.ReadKey();

            return result;
        }

        private void DisplayBoon(IBoon boon)
        {
            foreach (var identifier in boonIdentifiers)
            {
                if (identifier.CanIdentify(boon))
                {
                    identifier.Identify(boon);
                }
            }

            throw new ArgumentException("Cannot find a BoonProcessor to match boon: " + boon);
        }
    }
}
