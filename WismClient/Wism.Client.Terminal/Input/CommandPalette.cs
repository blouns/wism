using Wism.Client.Terminal.Game;
using Wism.Client.Terminal.Rendering;

namespace Wism.Client.Terminal.Input;

public static class CommandPalette
{
    public static bool Execute(TerminalGameSession session, Viewport viewport, string command)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var verb = parts[0].ToLowerInvariant();
        int ReadInt(int index, int fallback) =>
            index < parts.Length && int.TryParse(parts[index], out var parsed) ? parsed : fallback;

        switch (verb)
        {
            case "goto":
            case "g":
                viewport.CenterOn(ReadInt(1, viewport.CursorX), ReadInt(2, viewport.CursorY));
                session.SetStatus($"Cursor at {viewport.CursorX},{viewport.CursorY}.");
                return true;
            case "select":
            case "s":
                viewport.SetCursor(ReadInt(1, viewport.CursorX), ReadInt(2, viewport.CursorY));
                return session.TrySelectAt(viewport.CursorX, viewport.CursorY);
            case "move":
            case "m":
                viewport.SetCursor(ReadInt(1, viewport.CursorX), ReadInt(2, viewport.CursorY));
                return session.TryMoveOrAttackTo(viewport.CursorX, viewport.CursorY);
            case "attack":
            case "a":
                viewport.SetCursor(ReadInt(1, viewport.CursorX), ReadInt(2, viewport.CursorY));
                return session.TryMoveOrAttackTo(viewport.CursorX, viewport.CursorY);
            case "end":
            case "e":
                return session.TryEndTurn();
            case "next":
            case "n":
                return session.TrySelectNext();
            case "save":
                return session.Save(parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty);
            case "load":
                return session.Load(parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty);
            case "produce":
            case "p":
                return session.TryStartProductionAt(viewport.CursorX, viewport.CursorY, ReadInt(1, -1));
            case "help":
            case "?":
                session.SetStatus("Press ? for the key help overlay.");
                return true;
            default:
                session.SetStatus($"Unknown command: {verb}");
                return false;
        }
    }
}
