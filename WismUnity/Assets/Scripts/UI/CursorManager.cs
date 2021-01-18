using System.Collections;
using System.Collections.Generic;
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
    private Texture2D move;
    [SerializeField]
    private Texture2D produce;
    [SerializeField]
    private Texture2D select;
    [SerializeField]
    private Texture2D point;

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

    public void MoveCursor()
    {
        SetCursor(move);
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
