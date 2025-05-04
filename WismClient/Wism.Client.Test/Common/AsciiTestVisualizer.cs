using System;
using System.Threading;
using Wism.Client.Core;

[AttributeUsage(AttributeTargets.Method)]
public class AsciiVisualizerAttribute : Attribute
{
    public int DelayMilliseconds { get; }

    public AsciiVisualizerAttribute(int delayMilliseconds = 250)
    {
        DelayMilliseconds = delayMilliseconds;
    }
}

public static class AsciiTestVisualizer
{
    public static bool Enabled { get; private set; } = false;
    public static int DelayMilliseconds { get; private set; } = 0;

    public static void Enable(int delay = 250)
    {
        Enabled = true;
        DelayMilliseconds = delay;
    }

    public static void Draw()
    {
        if (!Enabled) return;

        try
        {
            Console.Clear();
        }
        catch
        {
            // Ignore any exceptions that occur while clearing the console.
            // Common test harness failure mode
        }

        Console.WriteLine("=== TEST MAP SNAPSHOT ===");

        for (var y = World.Current.Map.GetLength(1) - 1; y >= 0; y--)
        {
            for (var x = 0; x < World.Current.Map.GetLength(0); x++)
            {
                var tile = World.Current.Map[x, y];
                var terrain = tile.Terrain.ShortName;
                var army = tile.HasAnyArmies() ? tile.GetAllArmies()[0].ShortName : null;

                Console.Write($"{x}{y}{GetTerrainSymbol(terrain)}{GetArmySymbol(army)}{GetArmyCount(tile)}\t");
            }

            Console.WriteLine();
        }

        Console.WriteLine("==========================");
        Thread.Sleep(DelayMilliseconds);
    }

    private static char GetTerrainSymbol(string terrain) =>
        terrain switch
        {
            "Castle" => '^',
            "Grass" => '.',
            "Road" => '-',
            "Forest" => 'f',
            "Mountain" => 'm',
            "Water" => '~',
            _ => '?'
        };

    private static char GetArmySymbol(string army) =>
        string.IsNullOrWhiteSpace(army) ? ' ' : char.ToLower(army[0]);

    private static string GetArmyCount(Tile tile)
    {
        int total = (tile.Armies?.Count ?? 0) + (tile.VisitingArmies?.Count ?? 0);
        return total > 0 ? total.ToString() : " ";
    }
}