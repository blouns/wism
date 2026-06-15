using Wism.Client.Core;
using Wism.Client.Terminal.Game;
using Wism.Client.Terminal.Rendering;
using WismGame = Wism.Client.Core.Game;

namespace Wism.Client.Terminal.Input;

public sealed class TerminalHost
{
    private readonly TerminalMapRenderer renderer = new();
    private readonly ConsoleFrameWriter writer = new();

    public int Run(TerminalGameSession session)
    {
        var viewport = new Viewport(session.MapWidth, session.MapHeight);
        FollowSelected(viewport);
        var options = new RenderOptions
        {
            TileMode = session.Options.TileMode,
            NoColor = session.Options.NoColor
        };

        var showHelp = false;
        var follow = true;
        var done = false;
        EnterAlternateScreen();
        try
        {
            while (!done)
            {
                if (follow)
                {
                    FollowSelected(viewport);
                }

                var frame = renderer.Render(session, viewport, Console.WindowWidth, Console.WindowHeight, options with { ShowHelp = showHelp });
                writer.Write(frame, options.NoColor);

                var key = Console.ReadKey(intercept: true);
                session.Recorder.RecordInput(KeyName(key));
                if (showHelp && key.Key != ConsoleKey.Oem2)
                {
                    showHelp = false;
                }

                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        session.TryDeselect();
                        break;
                    case ConsoleKey.F1:
                    case ConsoleKey.Oem2:
                        showHelp = !showHelp;
                        break;
                    case ConsoleKey.Tab:
                    case ConsoleKey.Spacebar:
                    case ConsoleKey.N:
                        session.TrySelectNext();
                        follow = true;
                        break;
                    case ConsoleKey.S:
                        session.TrySelectAt(viewport.CursorX, viewport.CursorY);
                        follow = true;
                        break;
                    case ConsoleKey.M:
                    case ConsoleKey.A:
                        session.TryMoveOrAttackTo(viewport.CursorX, viewport.CursorY);
                        follow = true;
                        break;
                    case ConsoleKey.D:
                        session.TryDefend();
                        break;
                    case ConsoleKey.Q:
                        if (WismGame.Current.ArmiesSelected())
                        {
                            session.TryQuitArmy();
                        }
                        else
                        {
                            done = true;
                        }

                        break;
                    case ConsoleKey.Z:
                        session.TrySearch();
                        break;
                    case ConsoleKey.T:
                        session.TryTake();
                        break;
                    case ConsoleKey.O:
                        session.TryDrop();
                        break;
                    case ConsoleKey.P:
                        session.TryStartProductionAt(viewport.CursorX, viewport.CursorY, -1);
                        break;
                    case ConsoleKey.E:
                        session.TryEndTurn();
                        follow = true;
                        break;
                    case ConsoleKey.F:
                        follow = !follow;
                        session.SetStatus(follow ? "Following selected stack." : "Follow off.");
                        break;
                    case ConsoleKey.Add:
                    case ConsoleKey.OemPlus:
                        options = options with { TileMode = NextMode(options.TileMode) };
                        break;
                    case ConsoleKey.Subtract:
                    case ConsoleKey.OemMinus:
                        options = options with { TileMode = PreviousMode(options.TileMode) };
                        break;
                    case ConsoleKey.UpArrow:
                        HandleMove(session, viewport, 0, 1, ref follow);
                        break;
                    case ConsoleKey.DownArrow:
                        HandleMove(session, viewport, 0, -1, ref follow);
                        break;
                    case ConsoleKey.LeftArrow:
                        HandleMove(session, viewport, -1, 0, ref follow);
                        break;
                    case ConsoleKey.RightArrow:
                        HandleMove(session, viewport, 1, 0, ref follow);
                        break;
                    case ConsoleKey.Oem1:
                        ReadAndExecuteCommand(session, viewport, ref follow);
                        break;
                }
            }
        }
        finally
        {
            session.CompleteRecording();
            LeaveAlternateScreen();
        }

        return 0;
    }

    private static void HandleMove(TerminalGameSession session, Viewport viewport, int dx, int dy, ref bool follow)
    {
        if (WismGame.Current.ArmiesSelected())
        {
            var selected = WismGame.Current.GetSelectedArmies();
            var origin = selected?[0].Tile;
            if (origin != null)
            {
                session.TryMoveOrAttackTo(origin.X + dx, origin.Y + dy);
                FollowSelected(viewport);
                follow = true;
                return;
            }
        }

        viewport.MoveCursor(dx, dy);
        follow = false;
    }

    private static void ReadAndExecuteCommand(TerminalGameSession session, Viewport viewport, ref bool follow)
    {
        LeaveAlternateScreen();
        Console.Write(":");
        var line = Console.ReadLine() ?? string.Empty;
        EnterAlternateScreen();
        session.Recorder.RecordInput(":" + line);
        CommandPalette.Execute(session, viewport, line);
        follow = false;
    }

    private static void FollowSelected(Viewport viewport)
    {
        var selected = WismGame.Current.GetSelectedArmies();
        if (selected == null || selected.Count == 0)
        {
            return;
        }

        viewport.CenterOn(selected[0].X, selected[0].Y);
    }

    private static TileRenderMode NextMode(TileRenderMode mode) =>
        mode switch
        {
            TileRenderMode.Compact => TileRenderMode.Readable,
            TileRenderMode.Readable => TileRenderMode.Detailed,
            _ => TileRenderMode.Detailed
        };

    private static TileRenderMode PreviousMode(TileRenderMode mode) =>
        mode switch
        {
            TileRenderMode.Detailed => TileRenderMode.Readable,
            TileRenderMode.Readable => TileRenderMode.Compact,
            _ => TileRenderMode.Compact
        };

    private static string KeyName(ConsoleKeyInfo key) =>
        key.KeyChar == '\0' ? key.Key.ToString() : key.KeyChar.ToString();

    private static void EnterAlternateScreen()
    {
        if (Console.IsOutputRedirected)
        {
            return;
        }

        Console.Write("\u001b[?1049h\u001b[?25l");
        Console.Clear();
    }

    private static void LeaveAlternateScreen()
    {
        if (Console.IsOutputRedirected)
        {
            return;
        }

        Console.ResetColor();
        Console.CursorVisible = true;
        Console.Write("\u001b[?25h\u001b[?1049l");
    }
}
