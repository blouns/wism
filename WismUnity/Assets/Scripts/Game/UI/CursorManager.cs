using System;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
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
        SetCursor(info);
    }

    private void SetCursor(Texture2D cursor)
    {
        // Set the cursor origin to its center (default is upper left corner)
        Vector2 cursorOffset = new Vector2(cursor.width / 2, cursor.height / 2);
        Cursor.SetCursor(cursor, cursorOffset, CursorMode.Auto);
    }

    public void AttackCursor()
    {
        SetCursor(attack);
    }

    public void InfoCursor()
    {
        SetCursor(info);
    }

    public void MagnifyCursor()
    {
        SetCursor(magnify);
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
            moveCursor = moveNorth;
        }
        else if (degrees >= (0f + midDeg) && degrees <= (45f + midDeg))
        {
            // North east
            moveCursor = moveNorthEast;
        }
        else if (degrees >= (45f + midDeg) && degrees <= (90f + midDeg))
        {
            // East
            moveCursor = moveEast;
        }
        else if (degrees >= (90f + midDeg) && degrees <= (135f + midDeg))
        {
            // South-east
            moveCursor = moveSouthEast;
        }
        else if (degrees >= (135f + midDeg) && degrees <= (180f + midDeg))
        {
            // South
            moveCursor = moveSouth;
        }
        else if (degrees >= (180f + midDeg) && degrees <= (225f + midDeg))
        {
            // South-west
            moveCursor = moveSouthWest;
        }
        else if (degrees >= (225f + midDeg) && degrees <= (270f + midDeg))
        {
            // West
            moveCursor = moveWest;
        }
        else if (degrees >= (270f + midDeg) && degrees <= (315f + midDeg))
        {
            // North-west
            moveCursor = moveNorthWest;
        }
        else
        {
            throw new InvalidOperationException("Move cursor could not be calculated correctly.");
        }

        SetCursor(moveCursor);
    }

    public void ProduceCursor()
    {
        SetCursor(produce);
    }

    public void SelectCursor()
    {
        SetCursor(select);
    }

    public void PointCursor()
    {
        SetCursor(point);
    }
}
