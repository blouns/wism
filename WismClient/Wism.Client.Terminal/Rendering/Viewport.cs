namespace Wism.Client.Terminal.Rendering;

public sealed class Viewport
{
    public Viewport(int worldWidth, int worldHeight)
    {
        WorldWidth = Math.Max(1, worldWidth);
        WorldHeight = Math.Max(1, worldHeight);
        CursorX = 0;
        CursorY = WorldHeight - 1;
    }

    public int WorldWidth { get; private set; }

    public int WorldHeight { get; private set; }

    public int X { get; private set; }

    public int Y { get; private set; }

    public int CursorX { get; private set; }

    public int CursorY { get; private set; }

    public int ViewWidth { get; private set; } = 1;

    public int ViewHeight { get; private set; } = 1;

    public void Resize(int viewWidth, int viewHeight)
    {
        ViewWidth = Math.Max(1, viewWidth);
        ViewHeight = Math.Max(1, viewHeight);
        CenterOn(CursorX, CursorY);
    }

    public void MoveCursor(int dx, int dy)
    {
        CursorX = Math.Clamp(CursorX + dx, 0, WorldWidth - 1);
        CursorY = Math.Clamp(CursorY + dy, 0, WorldHeight - 1);
        KeepCursorVisible();
    }

    public void SetCursor(int x, int y)
    {
        CursorX = Math.Clamp(x, 0, WorldWidth - 1);
        CursorY = Math.Clamp(y, 0, WorldHeight - 1);
        KeepCursorVisible();
    }

    public void CenterOn(int x, int y)
    {
        CursorX = Math.Clamp(x, 0, WorldWidth - 1);
        CursorY = Math.Clamp(y, 0, WorldHeight - 1);
        X = Math.Clamp(CursorX - ViewWidth / 2, 0, Math.Max(0, WorldWidth - ViewWidth));
        Y = Math.Clamp(CursorY - ViewHeight / 2, 0, Math.Max(0, WorldHeight - ViewHeight));
    }

    public bool Contains(int x, int y) =>
        x >= X && x < X + ViewWidth &&
        y >= Y && y < Y + ViewHeight;

    public int MapYForRow(int row) => Y + (ViewHeight - 1 - row);

    private void KeepCursorVisible()
    {
        if (CursorX < X)
        {
            X = CursorX;
        }
        else if (CursorX >= X + ViewWidth)
        {
            X = CursorX - ViewWidth + 1;
        }

        if (CursorY < Y)
        {
            Y = CursorY;
        }
        else if (CursorY >= Y + ViewHeight)
        {
            Y = CursorY - ViewHeight + 1;
        }

        X = Math.Clamp(X, 0, Math.Max(0, WorldWidth - ViewWidth));
        Y = Math.Clamp(Y, 0, Math.Max(0, WorldHeight - ViewHeight));
    }
}
