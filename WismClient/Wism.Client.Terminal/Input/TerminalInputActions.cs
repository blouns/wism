using Wism.Client.Terminal.Game;
using Wism.Client.Terminal.Rendering;
using WismGame = Wism.Client.Core.Game;

namespace Wism.Client.Terminal.Input;

public static class TerminalInputActions
{
    public static void ClickCursor(TerminalGameSession session, Viewport viewport, ref bool follow)
    {
        if (WismGame.Current.ArmiesSelected())
        {
            session.TryMoveOrAttackTo(viewport.CursorX, viewport.CursorY);
            follow = true;
            return;
        }

        session.TrySelectAt(viewport.CursorX, viewport.CursorY);
        follow = true;
    }

    public static void MoveCursor(Viewport viewport, int dx, int dy, ref bool follow)
    {
        viewport.MoveCursor(dx, dy);
        follow = false;
    }

    public static void FollowSelected(Viewport viewport)
    {
        var selected = WismGame.Current.GetSelectedArmies();
        if (selected == null || selected.Count == 0)
        {
            return;
        }

        viewport.CenterOn(selected[0].X, selected[0].Y);
    }
}
