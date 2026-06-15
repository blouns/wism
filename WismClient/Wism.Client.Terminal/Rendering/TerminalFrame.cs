using System.Text;

namespace Wism.Client.Terminal.Rendering;

public sealed class TerminalFrame
{
    private readonly TerminalCell[,] cells;

    public TerminalFrame(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        cells = new TerminalCell[Width, Height];
        Clear();
    }

    public int Width { get; }

    public int Height { get; }

    public TerminalCell this[int x, int y] => cells[x, y];

    public void Clear(TerminalCell? cell = null)
    {
        var fill = cell ?? TerminalCell.Empty;
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                cells[x, y] = fill;
            }
        }
    }

    public void Write(int x, int y, char glyph, ConsoleColor foreground, ConsoleColor background = ConsoleColor.Black)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return;
        }

        cells[x, y] = new TerminalCell(glyph, foreground, background);
    }

    public void WriteText(int x, int y, string text, ConsoleColor foreground, ConsoleColor background = ConsoleColor.Black)
    {
        if (string.IsNullOrEmpty(text) || y < 0 || y >= Height)
        {
            return;
        }

        for (var i = 0; i < text.Length && x + i < Width; i++)
        {
            if (x + i >= 0)
            {
                Write(x + i, y, text[i], foreground, background);
            }
        }
    }

    public string ToPlainText()
    {
        var builder = new StringBuilder((Width + Environment.NewLine.Length) * Height);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                builder.Append(cells[x, y].Glyph);
            }

            if (y < Height - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }
}
