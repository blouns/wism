using Wism.Companion.Shared.Events;

public static class MapSnapshotExtensions
{
    /// <summary>
    ///  Returns a new MapSnapshot whose Y‐axis is flipped
    ///  (so 0→max, 1→max−1, …).
    ///  Leaves Width/Height and all other fields intact.
    /// </summary>
    public static MapSnapshot FlipYAxis(this MapSnapshot src)
    {
        var maxY = src.Height - 1;

        // flip tiles
        foreach (var t in src.Tiles)
            t.Y = maxY - t.Y;

        // flip armies/heroes
        foreach (var a in src.Armies)
            a.Position.Y = maxY - a.Position.Y;

        // flip cities
        foreach (var c in src.Cities)
            c.Position.Y = maxY - c.Position.Y;

        // if you have a SelectedArmy:
        if (src.SelectedArmy != null)
            src.SelectedArmy.Position.Y = maxY - src.SelectedArmy.Position.Y;

        return src;
    }
}
