using UnityEngine;
using UnityEngine.UI;

public static class ScreenOverlayCanvasHost
{
    public const string ContractName = "screen-overlay-canvas-v1";

    static readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);

    public static Canvas Ensure(GameObject host, int sortingOrder = 0)
    {
        var canvas = host.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = host.AddComponent<Canvas>();
        }

        Configure(canvas, sortingOrder);
        return canvas;
    }

    public static void Configure(Canvas canvas, int sortingOrder = 0)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}
