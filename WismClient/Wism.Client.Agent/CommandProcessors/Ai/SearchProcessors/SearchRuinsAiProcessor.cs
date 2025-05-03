using System;
using System.Collections.Generic;
using Wism.Client.Agent.CommandProcessors.Ai.SearchProcessors.BoonIdentifiers;
using Wism.Client.Agent.UI;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Locations;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Core.Boons;
using Wism.Client.MapObjects;

namespace Wism.Client.Agent.CommandProcessors.Ai.SearchProcessors;

public class SearchRuinsAiProcessor : ICommandProcessor
{
    private readonly AsciiGame asciiGame;
    private readonly List<IBoonIdentifier> boonIdentifiers;
    private IWismLogger logger;

    public SearchRuinsAiProcessor(IWismLoggerFactory loggerFactory, AsciiGame asciiGame)
    {
        if (loggerFactory is null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        logger = loggerFactory.CreateLogger();
        this.asciiGame = asciiGame ?? throw new ArgumentNullException(nameof(asciiGame));
        boonIdentifiers = new List<IBoonIdentifier>
        {
            new AlliesAiBoonIdentifier(),
            new ThroneAiBoonIdentifier(),
            new ArtifactAiBoonIdentifier(),
            new GoldAiBoonIdentifier()
        };
    }

    public bool CanExecute(ICommandAction command)
    {
        // Ruins and tombs are interchangeable
        return command is SearchRuinsCommand;
    }

    public ActionState Execute(ICommandAction command)
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
            Notify.DisplayAndWait("You have found nothing!");
            return ActionState.Failed;
        }

        if (location.Boon is ThroneBoon)
        {
            Notify.Information("A throne stands before you. Will you sit in the throne?");
            var key = Console.ReadKey();
            if (key.Key != ConsoleKey.Y)
            {
                return ActionState.Failed;
            }

            Console.WriteLine();
        }

        var monster = location.Monster;
        if (monster != null)
        {
            Notify.DisplayAndWait($"{hero.DisplayName} encounters a {monster}...");
        }

        // Search the ruins
        var result = ruinsCommand.Execute();
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