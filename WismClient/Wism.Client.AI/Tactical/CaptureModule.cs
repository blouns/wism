using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using Wism.Client.AI.Tactical;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;
using System.Linq;
using Wism.Client.AI.Framework;

public class CaptureModule : ITacticalModule
{
    private readonly ArmyController armyController;
    private readonly ILogger<CaptureModule> logger;

    public CaptureModule(ArmyController armyController, ILogger<CaptureModule> logger)
    {
        this.armyController = armyController;
        this.logger = logger;
    }

    public IEnumerable<IBid> GenerateBids(World world)
    {
        var bids = new List<IBid>();
        var player = Game.Current.GetCurrentPlayer();

        var cities = world.GetCities()
            .Where(c => c.Clan != player.Clan) // Target only neutral or enemy cities
            .ToList();

        if (cities.Count == 0)
        {
            logger.LogInformation("[Capture] No capturable cities found.");
            return bids;
        }

        foreach (var city in cities)
        {
            logger.LogInformation($"[Capture] Found city at ({city.Tile.X},{city.Tile.Y}) owned by {city.Clan?.ShortName ?? "Neutral"}.");
        }

        foreach (var army in player.GetArmies())
        {
            if (army.MovesRemaining <= 0)
            {
                logger.LogInformation($"[Capture] Skipping army at ({army.Tile.X},{army.Tile.Y}) — no moves remaining.");
                continue;
            }

            var targetCity = FindNearestCapturableCity(army, cities);
            if (targetCity == null)
            {
                logger.LogInformation($"[Capture] No reachable cities found for army at ({army.Tile.X},{army.Tile.Y}).");
                continue;
            }

            var distance = AiUtilities.GetManhattanDistance(army.Tile, targetCity.Tile);
            var utility = 1.0 / (distance + 1);

            logger.LogInformation($"[Capture] Bidding army at ({army.Tile.X},{army.Tile.Y}) to target city at ({targetCity.Tile.X},{targetCity.Tile.Y}) with utility {utility:0.000}.");

            bids.Add(new SimpleBid(new List<Army> { army }, this, utility));
        }

        return bids;
    }


    public IEnumerable<ICommandAction> GenerateCommands(List<Army> armies, World world)
    {
        var army = armies[0];
        var commands = new List<ICommandAction>();

        var capturableCities = world.GetCities()
            .Where(c => c.Clan != army.Player.Clan)
            .ToList();

        var target = FindNearestCapturableCity(army, capturableCities);
        if (target == null) return commands;

        // If adjacent and can attack — attack the city
        if (target.Tile.CanAttackHere(armies) && AiUtilities.IsInAttackRange(armies, target.Tile))
        {
            logger.LogInformation($"[Capture] Army attacking city at ({target.Tile.X},{target.Tile.Y})");

            return AiUtilities.GenerateAttackCommands(armyController, armies, commands, target.Tile);
        }

        // Otherwise move toward the city
        var attackPosition = AiUtilities.FindAttackPosition(target.Tile, armies, Game.Current.PathingStrategy, logger);
        if (attackPosition != null)
        {
            logger.LogInformation($"[Capture] Army moving toward city at ({attackPosition.X},{attackPosition.Y})");

            commands.Add(new MoveOnceCommand(armyController, armies, attackPosition.X, attackPosition.Y));
        }
        else
        {
            logger.LogWarning($"[Capture] Could not find valid attack position for city at ({target.Tile.X},{target.Tile.Y})");
        }

        return commands;
    }


    private City FindNearestCapturableCity(Army army, List<City> cities)
    {
        var sorted = cities
        .OrderBy(c => AiUtilities.GetManhattanDistance(army.Tile, c.Tile))
        .ToList();

        foreach (var city in sorted)
        {
            Console.WriteLine($"[Capture] Considering city at ({city.Tile.X},{city.Tile.Y}) owned by {city.Clan?.ShortName ?? "Neutral"}");
        }

        return sorted.FirstOrDefault(); // or null
    }

    private int ManhattanDistance(Tile a, Tile b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }
}
