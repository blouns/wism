using System.Text;

namespace Wism.Client.Terminal.Rendering;

public sealed class ConsoleFrameWriter
{
    private TerminalFrame? previous;

    public void Write(TerminalFrame frame, bool noColor)
    {
        Console.CursorVisible = false;
        if (previous == null || previous.Width != frame.Width || previous.Height != frame.Height)
        {
            Console.SetCursorPosition(0, 0);
            Console.Clear();
            previous = null;
        }

        for (var y = 0; y < frame.Height; y++)
        {
            var x = 0;
            while (x < frame.Width)
            {
                var cell = frame[x, y];
                if (previous != null && previous[x, y].Equals(cell))
                {
                    x++;
                    continue;
                }

                Console.SetCursorPosition(x, y);
                if (!noColor)
                {
                    Console.ForegroundColor = cell.Foreground;
                    Console.BackgroundColor = cell.Background;
                }

                var run = new StringBuilder();
                while (x < frame.Width)
                {
                    var next = frame[x, y];
                    if (!next.Equals(cell))
                    {
                        break;
                    }

                    if (previous != null && previous[x, y].Equals(next))
                    {
                        break;
                    }

                    run.Append(next.Glyph);
                    x++;
                }

                Console.Write(run.ToString());
            }
        }

        Console.ResetColor();
        previous = frame;
    }
}
