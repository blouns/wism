namespace Wism.Client.Terminal.Rendering;

public enum TileRenderMode
{
    Compact,
    Readable,
    Detailed
}

public sealed record RenderOptions
{
    public TileRenderMode TileMode { get; init; } = TileRenderMode.Readable;

    public bool NoColor { get; init; }

    public bool ShowHelp { get; init; }

    public int TileWidth => TileMode switch
    {
        TileRenderMode.Compact => 1,
        TileRenderMode.Detailed => 3,
        _ => 2
    };
}
