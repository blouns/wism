namespace Wism.Client.Terminal.Rendering;

public readonly record struct TerminalCell(
    char Glyph,
    ConsoleColor Foreground,
    ConsoleColor Background)
{
    public static TerminalCell Empty { get; } = new(' ', ConsoleColor.Gray, ConsoleColor.Black);
}
