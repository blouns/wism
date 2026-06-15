using Wism.Client.Core;
using Wism.Client.MapObjects;
using Wism.Client.Terminal.Game;
using WismGame = Wism.Client.Core.Game;

namespace Wism.Client.Terminal.Rendering;

public sealed class TerminalMapRenderer
{
    private const int HeaderHeight = 2;
    private const int FooterHeight = 2;

    public TerminalFrame Render(TerminalGameSession session, Viewport viewport, int width, int height, RenderOptions options)
    {
        var frame = new TerminalFrame(width, height);
        var inspectorWidth = width >= 100 ? 34 : 0;
        var mapWidth = Math.Max(20, width - inspectorWidth - (inspectorWidth > 0 ? 1 : 0));
        var mapHeight = Math.Max(8, height - HeaderHeight - FooterHeight);
        var tileWidth = options.TileWidth;
        var viewTilesWide = Math.Max(1, mapWidth / tileWidth);

        viewport.Resize(viewTilesWide, mapHeight);

        DrawHeader(frame, session);
        DrawMap(frame, session, viewport, options, 0, HeaderHeight, mapWidth, mapHeight);

        if (inspectorWidth > 0)
        {
            var x = mapWidth + 1;
            DrawDivider(frame, mapWidth, HeaderHeight, mapHeight);
            DrawInspector(frame, session, viewport, x, HeaderHeight, inspectorWidth, mapHeight);
        }

        DrawFooter(frame, session, height - FooterHeight);

        if (options.ShowHelp)
        {
            DrawHelp(frame);
        }

        return frame;
    }

    private static void DrawHeader(TerminalFrame frame, TerminalGameSession session)
    {
        var player = WismGame.Current.GetCurrentPlayer();
        var title = $" WISM Terminal | {session.WorldName} {session.MapWidth}x{session.MapHeight} | {player.Clan.DisplayName} | Turn {player.Turn} | Gold {player.Gold} ";
        frame.WriteText(0, 0, Fit(title, frame.Width), ConsoleColor.White, ConsoleColor.DarkBlue);
        frame.WriteText(0, 1, Fit(" ?:help  :commands  arrows:pan/move  tab/space:next  e:end  q:quit", frame.Width), ConsoleColor.Gray);
    }

    private static void DrawMap(
        TerminalFrame frame,
        TerminalGameSession session,
        Viewport viewport,
        RenderOptions options,
        int left,
        int top,
        int mapWidth,
        int mapHeight)
    {
        var tileWidth = options.TileWidth;
        var map = World.Current.Map;
        for (var row = 0; row < mapHeight; row++)
        {
            var y = viewport.MapYForRow(row);
            if (y < 0 || y >= session.MapHeight)
            {
                continue;
            }

            for (var col = 0; col < viewport.ViewWidth; col++)
            {
                var x = viewport.X + col;
                if (x < 0 || x >= session.MapWidth)
                {
                    continue;
                }

                var tile = map[x, y];
                var screenX = left + col * tileWidth;
                var screenY = top + row;
                var selected = IsSelected(tile);
                var cursor = x == viewport.CursorX && y == viewport.CursorY;
                var background = cursor ? ConsoleColor.DarkGray : selected ? ConsoleColor.Gray : ConsoleColor.Black;
                var foreground = GetForeground(tile, options.NoColor, selected || cursor);

                DrawTile(frame, tile, options.TileMode, screenX, screenY, tileWidth, foreground, background);
            }
        }
    }

    private static void DrawTile(
        TerminalFrame frame,
        Tile tile,
        TileRenderMode mode,
        int x,
        int y,
        int width,
        ConsoleColor foreground,
        ConsoleColor background)
    {
        var glyph = TileGlyphs.GetTileGlyph(tile, mode);
        frame.Write(x, y, glyph, foreground, background);

        if (width >= 2)
        {
            var stack = tile.GetAllArmies().Count;
            var second = stack > 0 ? Math.Min(stack, 9).ToString()[0] : TileGlyphs.GetTerrainGlyph(tile.Terrain.ShortName);
            frame.Write(x + 1, y, second, foreground, background);
        }

        if (width >= 3)
        {
            var owner = tile.HasAnyArmies()
                ? tile.GetAllArmies()[0].Clan.ShortName[0]
                : tile.HasCity() && tile.City.Clan != null
                    ? tile.City.Clan.ShortName[0]
                    : ' ';
            frame.Write(x + 2, y, owner, foreground, background);
        }
    }

    private static ConsoleColor GetForeground(Tile tile, bool noColor, bool inverted)
    {
        if (inverted)
        {
            return ConsoleColor.Black;
        }

        if (noColor)
        {
            return ConsoleColor.Gray;
        }

        if (tile.HasAnyArmies())
        {
            return TileGlyphs.GetClanColor(tile.GetAllArmies()[0].Clan);
        }

        if (tile.HasCity() && tile.City.Clan != null)
        {
            return TileGlyphs.GetClanColor(tile.City.Clan);
        }

        return TileGlyphs.GetTerrainColor(tile.Terrain.ShortName);
    }

    private static bool IsSelected(Tile tile)
    {
        if (!WismGame.Current.ArmiesSelected())
        {
            return false;
        }

        var selected = WismGame.Current.GetSelectedArmies();
        return selected != null && selected.Count > 0 && selected[0].Tile == tile;
    }

    private static void DrawDivider(TerminalFrame frame, int x, int top, int height)
    {
        for (var y = top; y < top + height && y < frame.Height; y++)
        {
            frame.Write(x, y, '|', ConsoleColor.DarkGray);
        }
    }

    private static void DrawInspector(TerminalFrame frame, TerminalGameSession session, Viewport viewport, int left, int top, int width, int height)
    {
        var tile = World.Current.Map[viewport.CursorX, viewport.CursorY];
        var line = top;
        Write(frame, left, line++, width, "INSPECTOR", ConsoleColor.White);
        Write(frame, left, line++, width, $"Tile     {tile.X},{tile.Y}", ConsoleColor.Gray);
        Write(frame, left, line++, width, $"Terrain  {tile.Terrain.DisplayName}", ConsoleColor.Gray);

        if (tile.HasCity())
        {
            Write(frame, left, line++, width, $"City     {tile.City.DisplayName}", ConsoleColor.Yellow);
            Write(frame, left, line++, width, $"Owner    {tile.City.Clan?.DisplayName ?? "Neutral"}", ConsoleColor.Yellow);
            Write(frame, left, line++, width, $"Defense  {tile.City.Defense}", ConsoleColor.Yellow);
        }

        if (tile.HasLocation())
        {
            Write(frame, left, line++, width, $"Site     {tile.Location.DisplayName}", ConsoleColor.Cyan);
            Write(frame, left, line++, width, $"Kind     {tile.Location.Kind}", ConsoleColor.Cyan);
        }

        var armies = tile.GetAllArmies();
        if (armies.Count > 0)
        {
            Write(frame, left, line++, width, $"Armies   {armies.Count}", ConsoleColor.White);
            foreach (var army in armies.Take(Math.Max(0, height - 18)))
            {
                Write(frame, left, line++, width, $"{army.Clan.ShortName}: {army.DisplayName ?? army.KindName} S{army.Strength} M{army.MovesRemaining}", TileGlyphs.GetClanColor(army.Clan));
            }
        }

        line = Math.Max(line + 1, top + height - 10);
        DrawMiniMap(frame, session, viewport, left, line, width, Math.Min(9, top + height - line));
    }

    private static void DrawMiniMap(TerminalFrame frame, TerminalGameSession session, Viewport viewport, int left, int top, int width, int height)
    {
        if (height <= 1)
        {
            return;
        }

        Write(frame, left, top, width, "MINIMAP", ConsoleColor.White);
        var mapWidth = Math.Min(width, 30);
        var mapHeight = height - 1;
        for (var y = 0; y < mapHeight; y++)
        {
            for (var x = 0; x < mapWidth; x++)
            {
                var mapX = x * session.MapWidth / Math.Max(1, mapWidth);
                var mapY = (mapHeight - 1 - y) * session.MapHeight / Math.Max(1, mapHeight);
                var inView = viewport.Contains(mapX, mapY);
                var tile = World.Current.Map[mapX, mapY];
                var glyph = inView ? '#' : tile.Terrain.ShortName == "Water" ? '~' : '.';
                var color = inView ? ConsoleColor.White : TileGlyphs.GetTerrainColor(tile.Terrain.ShortName);
                frame.Write(left + x, top + 1 + y, glyph, color);
            }
        }
    }

    private static void DrawFooter(TerminalFrame frame, TerminalGameSession session, int top)
    {
        frame.WriteText(0, top, Fit(session.StatusMessage, frame.Width), ConsoleColor.White);
        frame.WriteText(0, top + 1, Fit(session.RecordStatus, frame.Width), ConsoleColor.DarkGray);
    }

    private static void DrawHelp(TerminalFrame frame)
    {
        var boxWidth = Math.Min(76, frame.Width - 4);
        var boxHeight = Math.Min(16, frame.Height - 4);
        var left = Math.Max(0, (frame.Width - boxWidth) / 2);
        var top = Math.Max(0, (frame.Height - boxHeight) / 2);
        for (var y = top; y < top + boxHeight; y++)
        {
            for (var x = left; x < left + boxWidth; x++)
            {
                frame.Write(x, y, ' ', ConsoleColor.White, ConsoleColor.DarkBlue);
            }
        }

        var lines = new[]
        {
            "WISM TERMINAL KEYS",
            "Arrows: move selected stack, otherwise move cursor/pan",
            "S select cursor stack   Esc deselect   Space/Tab next army",
            "M move selected stack to cursor   A attack cursor",
            "D defend   Q quit stack for turn   Z search   T take   O drop",
            "P show production choices at cursor city",
            "E end turn   : command palette   +/- tile density   F follow",
            "Commands: goto x y | select x y | move x y | save path | load path",
            "Press ? to close help."
        };
        for (var i = 0; i < lines.Length && i + 1 < boxHeight; i++)
        {
            frame.WriteText(left + 2, top + 1 + i, Fit(lines[i], boxWidth - 4), ConsoleColor.White, ConsoleColor.DarkBlue);
        }
    }

    private static void Write(TerminalFrame frame, int left, int line, int width, string text, ConsoleColor color) =>
        frame.WriteText(left, line, Fit(text, width), color);

    private static string Fit(string text, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        return text.Length <= width ? text.PadRight(width) : text[..Math.Max(0, width - 1)] + ">";
    }
}
