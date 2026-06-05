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
using Wism.Client.Common;

public class CaptureModule : ITacticalModule
{
    private readonly ArmyController armyController;
    private readonly IWismLogger logger;

    public CaptureModule(ArmyController armyController, IWismLogger logger)
    {
        this.armyController = armyController;
        this.logger = logger;
    }

    public IEnumerable<IBid> GenerateBids(World world)
    {
        var bids = new List<IBid>();
        var player = Game.Current.GetCurrentPlayer();

        var cities = world.GetCities()
            .Where(c => c.Clan != player.Clan)
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

        var stacks = player.GetArmies()
            .Where(a => a.MovesRemaining > 0)
            .GroupBy(a => (a.Tile.X, a.Tile.Y));

        foreach (var group in stacks)
        {
            var stack = group.ToList();
            if (stack.Count == 0)
                continue;

            var leader = stack[0];

            var targetCity = FindNearestCapturableCity(leader, cities);
            if (targetCity == null)
            {
                logger.LogInformation($"[Capture] No reachable cities found for stack at ({leader.Tile.X},{leader.Tile.Y}).");
                continue;
            }

            var distance = AiUtilities.GetManhattanDistance(leader.Tile, targetCity.Tile);
            var utility = 1.0 / (distance + 1);

            logger.LogInformation($"[Capture] Bidding army stack at ({leader.Tile.X},{leader.Tile.Y}) to target city at ({targetCity.Tile.X},{targetCity.Tile.Y}) with utility {utility:0.000}.");

            bids.Add(new SimpleBid(stack, this, utility));
        }

        return bids;
    }

    public IEnumerable<ICommandAction> GenerateCommands(List<Army> armies, World world)
    {
        var commands = new List<ICommandAction>();

        // 1) Snapshot current selection
        var current = Game.Current.ArmiesSelected()
            ? Game.Current.GetSelectedArmies()
            : new List<Army>();

        var army = armies[0];
        var capturableCities = world.GetCities()
            .Where(c => c.Clan != army.Player.Clan)
            .ToList();

        var target = FindNearestCapturableCity(army, capturableCities);
        if (target == null)
            return commands;

        // 2) If in range, generate full attack pipeline, then filter it
        if (target.Tile.CanAttackHere(armies) && AiUtilities.IsInAttackRange(armies, target.Tile))
        {
            logger.LogInformation($"[Capture] Army attacking city at ({target.Tile.X},{target.Tile.Y})");

            var raw = AiUtilities.GenerateAttackCommands(
                armyController, armies, new List<ICommandAction>(), target.Tile);

            foreach (var cmd in raw)
            {
                // skip redundant select
                if (cmd is SelectArmyCommand sel
                    && sel.Armies.Count == current.Count
                    && !sel.Armies.Except(current).Any())
                {
                    logger.LogInformation("[Capture] Skipping duplicate SelectArmyCommand");
                    continue;
                }

                commands.Add(cmd);
                if (cmd is SelectArmyCommand s)
                    current = s.Armies;
            }

            return commands;
        }

        // 3) Otherwise just move toward the city
        var attackPosition = AiUtilities.FindAttackPosition(
            target.Tile, armies, Game.Current.PathingStrategy, logger);

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
