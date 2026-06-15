using System.Text.Json;
using Wism.Client.Core;
using Wism.Client.Terminal.Game;
using Wism.Client.Terminal.Input;
using Wism.Client.Terminal.Rendering;
using WismGame = Wism.Client.Core.Game;

namespace Wism.Client.Terminal.Cli;

public static class AgentScriptRunner
{
    public static int Run(TerminalLaunchOptions options)
    {
        var session = TerminalGameSession.Create(options);
        return Run(session, options);
    }

    public static int Run(TerminalGameSession session, TerminalLaunchOptions options)
    {
        var viewport = new Viewport(session.MapWidth, session.MapHeight);
        var selected = WismGame.Current.GetSelectedArmies();
        if (selected != null && selected.Count > 0)
        {
            viewport.CenterOn(selected[0].X, selected[0].Y);
        }

        var lines = ReadInput(options.InputScript);
        using var writer = CreateWriter(options.OutputPath);
        var renderer = new TerminalMapRenderer();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            var trimmed = line.Trim();
            session.Recorder.RecordInput(trimmed);
            var rendered = false;
            if (trimmed.Equals("render", StringComparison.OrdinalIgnoreCase))
            {
                rendered = true;
            }
            else if (trimmed.Equals("up", StringComparison.OrdinalIgnoreCase))
            {
                viewport.MoveCursor(0, 1);
            }
            else if (trimmed.Equals("down", StringComparison.OrdinalIgnoreCase))
            {
                viewport.MoveCursor(0, -1);
            }
            else if (trimmed.Equals("left", StringComparison.OrdinalIgnoreCase))
            {
                viewport.MoveCursor(-1, 0);
            }
            else if (trimmed.Equals("right", StringComparison.OrdinalIgnoreCase))
            {
                viewport.MoveCursor(1, 0);
            }
            else
            {
                CommandPalette.Execute(session, viewport, trimmed);
            }

            var frame = renderer.Render(
                session,
                viewport,
                100,
                32,
                new RenderOptions { NoColor = true, TileMode = options.TileMode });

            WriteJsonLine(writer, new
            {
                input = trimmed,
                rendered,
                status = session.StatusMessage,
                cursor = new { viewport.CursorX, viewport.CursorY },
                currentPlayer = WismGame.Current.GetCurrentPlayer().Clan.ShortName,
                frame = frame.ToPlainText()
            });
        }

        session.CompleteRecording();
        return 0;
    }

    private static IEnumerable<string> ReadInput(string? inputScript)
    {
        if (!string.IsNullOrWhiteSpace(inputScript))
        {
            return File.ReadLines(inputScript);
        }

        if (!Console.IsInputRedirected)
        {
            return new[] { "render" };
        }

        var lines = new List<string>();
        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            lines.Add(line);
        }

        return lines.Count == 0 ? new[] { "render" } : lines;
    }

    private static TextWriter CreateWriter(string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return Console.Out;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? Environment.CurrentDirectory);
        return new StreamWriter(outputPath, append: false);
    }

    private static void WriteJsonLine(TextWriter writer, object value)
    {
        writer.WriteLine(JsonSerializer.Serialize(value));
        writer.Flush();
    }
}
