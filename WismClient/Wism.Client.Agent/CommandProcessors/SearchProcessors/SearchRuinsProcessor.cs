using System;
using System.Collections.Generic;
using Wism.Client.Agent.CommandProcessors.SearchProcessors.BoonIdentifiers;
using Wism.Client.Api.Telemetry;
using Wism.Client.Commands;
using Wism.Client.Commands.Locations;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Core.Boons;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.CommandProcessors.SearchProcessors;

public class SearchRuinsProcessor : InstrumentedProcessor
{
    private readonly List<IBoonIdentifier> boonIdentifiers;
    private IWismLogger logger;

    public SearchRuinsProcessor(IWismLoggerFactory loggerFactory, CommandIpcPublisher publisher)
        : base(publisher)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }
        logger = loggerFactory.CreateLogger();
  
        boonIdentifiers = new List<IBoonIdentifier>
        {
            new AlliesBoonIdentifier(),
            new ThroneBoonIdentifier(),
            new ArtifactBoonIdentifier(),
            new GoldBoonIdentifier()
        };
    }

    public override bool CanExecute(ICommandAction command)
    {
        // Ruins and tombs are interchangeable
        return command is SearchRuinsCommand;
    }

    public override ActionState ExecuteInternal(ICommandAction command)
    {
        var ruinsCommand = command as SearchRuinsCommand;

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

        if (hero == null ||
            ruinsCommand.Location.Searched)
        {
            if (IsHuman)
            {
                Notify.DisplayAndWait("You have found nothing!");
            }

            return ActionState.Failed;
        }

        if (location.Boon is ThroneBoon)
        {
            if (IsHuman)
            {
                Notify.DisplayAndWait("You have found a throne! Press any key to sit on it.");
                var key = Console.ReadKey();
                if (key.Key != ConsoleKey.Y)
                {
                    return ActionState.Failed;
                }

                Console.WriteLine();
            }
            else
            {
                // Automatically accept for AI players
            }
        }

        var monster = location.Monster;
        if (IsHuman && monster != null)
        {
            Notify.DisplayAndWait($"{hero.DisplayName} encounters a {monster}...");
        }

        // Search the ruins
        var result = ruinsCommand.Execute();

        if (IsHuman)
        {
            if (result == ActionState.Succeeded)
            {
                if (monster != null)
                {
                    Notify.DisplayAndWait("...and is victorious!");
                }

                DisplayBoon(ruinsCommand.Boon);
            }
            else if (result == ActionState.Failed &&
                     hero.IsDead)
            {
                Notify.DisplayAndWait("...and is slain!");
            }
            else
            {
                Notify.DisplayAndWait("You have found nothing!");
            }
        }
        else
        {
            // Display nothing for AI players
        }

        return result;
    }

    private void DisplayBoon(IBoon boon)
    {
        foreach (var identifier in boonIdentifiers)
        {
            if (identifier.CanIdentify(boon))
            {
                identifier.Identify(boon);
                return;
            }
        }

        throw new ArgumentException("Cannot identify boon: " + boon);
    }
}