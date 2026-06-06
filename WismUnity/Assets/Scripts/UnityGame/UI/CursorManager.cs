using System;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public enum HotspotAnchor
    {
        UpperLeft,
        Center
    }

    [SerializeField]
    private CursorMode cursorMode = CursorMode.ForceSoftware;

    [SerializeField]
    private Texture2D attack;
    [SerializeField]
    private Texture2D info;
    [SerializeField]
    private Texture2D magnify;
    [SerializeField]
    private Texture2D produce;
    [SerializeField]
    private Texture2D select;
    [SerializeField]
    private Texture2D point;

    [SerializeField]
    private Texture2D moveNorth;
    [SerializeField]
    private Texture2D moveNorthWest;
    [SerializeField]
    private Texture2D moveWest;
    [SerializeField]
    private Texture2D moveSouthWest;
    [SerializeField]
    private Texture2D moveSouth;
    [SerializeField]
    private Texture2D moveSouthEast;
    [SerializeField]
    private Texture2D moveEast;
    [SerializeField]
    private Texture2D moveNorthEast;

    void Start()
    {
        InfoCursor();
    }

    private void SetCursor(Texture2D cursor, HotspotAnchor hotspotAnchor)
    {
        if (cursor == null)
        {
            Cursor.SetCursor(null, Vector2.zero, this.cursorMode);
            return;
        }

        Cursor.SetCursor(cursor, CalculateHotspot(cursor, hotspotAnchor), this.cursorMode);
    }

    public static Vector2 CalculateHotspot(Texture2D cursor, HotspotAnchor hotspotAnchor)
    {
        if (cursor == null)
        {
            return Vector2.zero;
        }

        return hotspotAnchor == HotspotAnchor.Center
            ? new Vector2(cursor.width / 2f, cursor.height / 2f)
            : Vector2.zero;
    }

    public void AttackCursor()
    {
        SetCursor(this.attack, HotspotAnchor.Center);
    }

    public void InfoCursor()
    {
        SetCursor(this.info, HotspotAnchor.UpperLeft);
    }

    public void MagnifyCursor()
    {
        SetCursor(this.magnify, HotspotAnchor.Center);
    }

    public void MoveCursor(Vector3 heading)
    {
        const float midDeg = 22.5f;

        // Rotate compass to match Unity world's North
        float degrees = ((Mathf.Atan2(heading.y, -heading.x)) * Mathf.Rad2Deg);
        degrees = (degrees + 270f) % 360f;

        Texture2D moveCursor;
        if (degrees >= (360f - midDeg) || degrees <= (0f + midDeg))
        {
            // North
            moveCursor = this.moveNorth;
        }
        else if (degrees >= (0f + midDeg) && degrees <= (45f + midDeg))
        {
            // North east
            moveCursor = this.moveNorthEast;
        }
        else if (degrees >= (45f + midDeg) && degrees <= (90f + midDeg))
        {
            // East
            moveCursor = this.moveEast;
        }
        else if (degrees >= (90f + midDeg) && degrees <= (135f + midDeg))
        {
            // South-east
            moveCursor = this.moveSouthEast;
        }
        else if (degrees >= (135f + midDeg) && degrees <= (180f + midDeg))
        {
            // South
            moveCursor = this.moveSouth;
        }
        else if (degrees >= (180f + midDeg) && degrees <= (225f + midDeg))
        {
            // South-west
            moveCursor = this.moveSouthWest;
        }
        else if (degrees >= (225f + midDeg) && degrees <= (270f + midDeg))
        {
            // West
            moveCursor = this.moveWest;
        }
        else if (degrees >= (270f + midDeg) && degrees <= (315f + midDeg))
        {
            // North-west
            moveCursor = this.moveNorthWest;
        }
        else
        {
            throw new InvalidOperationException("Move cursor could not be calculated correctly.");
        }

        SetCursor(moveCursor, HotspotAnchor.Center);
    }

    public void ProduceCursor()
    {
        SetCursor(this.produce, HotspotAnchor.Center);
    }

    public void SelectCursor()
    {
        SetCursor(this.select, HotspotAnchor.Center);
    }

    public void PointCursor()
    {
        SetCursor(this.point, HotspotAnchor.UpperLeft);
    }
}
