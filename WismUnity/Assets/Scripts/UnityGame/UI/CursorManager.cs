using System;
using UnityEngine;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    private const string CursorCanvasName = "WismCursorCanvas";
    private const float ReferenceViewportHeight = 720f;
    private const float MaxPointerHeight = 28f;
    private const float MaxTargetHeight = 36f;

    public enum HotspotAnchor
    {
        UpperLeft,
        Center
    }

    [SerializeField]
    private CursorMode cursorMode = CursorMode.Auto;
    [SerializeField]
    private bool useViewportScaledCursor = true;
    [SerializeField]
    private float minCursorScale = 0.45f;
    [SerializeField]
    private float maxCursorScale = 1f;

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

    private Canvas cursorCanvas;
    private RawImage cursorImage;
    private RectTransform cursorTransform;
    private Texture2D activeCursor;
    private HotspotAnchor activeHotspotAnchor;
    private float activeScale = -1f;

    void Start()
    {
        InfoCursor();
    }

    void LateUpdate()
    {
        if (!this.useViewportScaledCursor || this.cursorTransform == null || this.activeCursor == null)
        {
            return;
        }

        var nextScale = CalculateViewportScale(Screen.height, this.minCursorScale, this.maxCursorScale);
        if (!Mathf.Approximately(nextScale, this.activeScale))
        {
            ApplyOverlayCursor(this.activeCursor, this.activeHotspotAnchor, nextScale);
        }

        this.cursorTransform.anchoredPosition = Assets.Scripts.UI.WismUiInputAdapter.PointerPosition;
    }

    void OnDisable()
    {
        RestoreSystemCursor();
    }

    void OnDestroy()
    {
        RestoreSystemCursor();
    }

    private void SetCursor(Texture2D cursor, HotspotAnchor hotspotAnchor)
    {
        if (cursor == null)
        {
            HideOverlayCursor();
            Cursor.SetCursor(null, Vector2.zero, this.cursorMode);
            return;
        }

        if (this.useViewportScaledCursor)
        {
            ApplyOverlayCursor(
                cursor,
                hotspotAnchor,
                CalculateViewportScale(Screen.height, this.minCursorScale, this.maxCursorScale));
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

    public static Vector2 CalculatePivot(Texture2D cursor, HotspotAnchor hotspotAnchor)
    {
        if (cursor == null || cursor.width <= 0 || cursor.height <= 0)
        {
            return new Vector2(0f, 1f);
        }

        var hotspot = CalculateHotspot(cursor, hotspotAnchor);
        return new Vector2(
            Mathf.Clamp01(hotspot.x / cursor.width),
            Mathf.Clamp01(1f - (hotspot.y / cursor.height)));
    }

    public static float CalculateViewportScale(int viewportHeight, float minScale, float maxScale)
    {
        var safeMin = Mathf.Max(0.05f, minScale);
        var safeMax = Mathf.Max(safeMin, maxScale);
        var height = Mathf.Max(1, viewportHeight);
        return Mathf.Clamp(height / ReferenceViewportHeight, safeMin, safeMax);
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

    private void ApplyOverlayCursor(Texture2D cursor, HotspotAnchor hotspotAnchor, float scale)
    {
        EnsureOverlayCursor();

        this.activeCursor = cursor;
        this.activeHotspotAnchor = hotspotAnchor;
        this.activeScale = scale;

        cursor.filterMode = FilterMode.Point;
        this.cursorImage.texture = cursor;
        this.cursorImage.enabled = true;
        this.cursorTransform.pivot = CalculatePivot(cursor, hotspotAnchor);
        this.cursorTransform.sizeDelta = CalculateOverlaySize(cursor, hotspotAnchor, scale);
        this.cursorTransform.anchoredPosition = Assets.Scripts.UI.WismUiInputAdapter.PointerPosition;
        Cursor.SetCursor(null, Vector2.zero, this.cursorMode);
        Cursor.visible = false;
    }

    public static Vector2 CalculateOverlaySize(Texture2D cursor, HotspotAnchor hotspotAnchor, float scale)
    {
        if (cursor == null || cursor.width <= 0 || cursor.height <= 0)
        {
            return Vector2.one;
        }

        var targetHeight = hotspotAnchor == HotspotAnchor.UpperLeft ? MaxPointerHeight : MaxTargetHeight;
        var scaledHeight = cursor.height * scale;
        var cappedScale = scaledHeight > targetHeight
            ? targetHeight / cursor.height
            : scale;
        return new Vector2(
            Mathf.Max(1f, cursor.width * cappedScale),
            Mathf.Max(1f, cursor.height * cappedScale));
    }

    private void EnsureOverlayCursor()
    {
        if (this.cursorCanvas != null && this.cursorImage != null && this.cursorTransform != null)
        {
            return;
        }

        var canvasObject = new GameObject(CursorCanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);

        this.cursorCanvas = canvasObject.GetComponent<Canvas>();
        this.cursorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        this.cursorCanvas.sortingOrder = short.MaxValue;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        var imageObject = new GameObject("CursorImage", typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(canvasObject.transform, false);

        this.cursorImage = imageObject.GetComponent<RawImage>();
        this.cursorImage.raycastTarget = false;
        this.cursorTransform = imageObject.GetComponent<RectTransform>();
        this.cursorTransform.anchorMin = Vector2.zero;
        this.cursorTransform.anchorMax = Vector2.zero;
    }

    private void HideOverlayCursor()
    {
        this.activeCursor = null;
        this.activeScale = -1f;
        if (this.cursorImage != null)
        {
            this.cursorImage.enabled = false;
        }

        Cursor.visible = true;
    }

    private void RestoreSystemCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.visible = true;
        if (this.cursorCanvas != null)
        {
            Destroy(this.cursorCanvas.gameObject);
            this.cursorCanvas = null;
            this.cursorImage = null;
            this.cursorTransform = null;
        }
    }
}
