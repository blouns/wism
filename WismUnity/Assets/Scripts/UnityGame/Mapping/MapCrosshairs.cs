using UnityEngine;
using Wism.Client.Core;

public class MapCrosshairs : MonoBehaviour
{
    private Camera mainCamera;
    private RectTransform crosshairsRect;
    private RectTransform minimapMapRect;
    private RectTransform minimapPanelRect;

    public void LateUpdate()
    {
        MoveCrosshairs();
    }

    private void MoveCrosshairs()
    {
        if (!Game.IsInitialized() || World.Current?.Map == null)
        {
            return;
        }

        var camera = GetMainCamera();
        var minimapMapRect = GetMinimapMapRect();
        AlignMinimapMapRect(minimapMapRect);

        var mapWidth = Mathf.Max(1, World.Current.Map.GetLength(0));
        var mapHeight = Mathf.Max(1, World.Current.Map.GetLength(1));
        var viewportMin = camera.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        var viewportMax = camera.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

        var left = Mathf.Clamp(Mathf.Min(viewportMin.x, viewportMax.x), 0f, mapWidth);
        var right = Mathf.Clamp(Mathf.Max(viewportMin.x, viewportMax.x), 0f, mapWidth);
        var bottom = Mathf.Clamp(Mathf.Min(viewportMin.y, viewportMax.y), 0f, mapHeight);
        var top = Mathf.Clamp(Mathf.Max(viewportMin.y, viewportMax.y), 0f, mapHeight);

        float newXPercent = ((left + right) * 0.5f) / mapWidth;
        float newYPercent = ((bottom + top) * 0.5f) / mapHeight;
        float newX = minimapMapRect.rect.width * newXPercent - (minimapMapRect.rect.width / 2f);
        float newY = minimapMapRect.rect.height * newYPercent - (minimapMapRect.rect.height / 2f);

        var crossRect = GetCrosshairsRect();
        AlignCrosshairsRect(crossRect, minimapMapRect);

        var viewportWidth = Mathf.Max(0f, right - left);
        var viewportHeight = Mathf.Max(0f, top - bottom);
        crossRect.sizeDelta = new Vector2(
            Mathf.Clamp01(viewportWidth / mapWidth) * minimapMapRect.rect.width,
            Mathf.Clamp01(viewportHeight / mapHeight) * minimapMapRect.rect.height);
        crossRect.anchoredPosition = new Vector2(
            Mathf.Clamp(newX, -(minimapMapRect.rect.width / 2f), minimapMapRect.rect.width / 2f),
            Mathf.Clamp(newY, -(minimapMapRect.rect.height / 2f), minimapMapRect.rect.height / 2f));
    }

    private void AlignMinimapMapRect(RectTransform mapRect)
    {
        var panelRect = GetMinimapPanelRect();
        if (mapRect == null || panelRect == null || mapRect.parent != panelRect)
        {
            return;
        }

        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.anchoredPosition = Vector2.zero;
    }

    private static void AlignCrosshairsRect(RectTransform crossRect, RectTransform mapRect)
    {
        if (crossRect == null || mapRect == null)
        {
            return;
        }

        if (crossRect.parent != mapRect)
        {
            crossRect.SetParent(mapRect, false);
        }

        crossRect.anchorMin = new Vector2(0.5f, 0.5f);
        crossRect.anchorMax = new Vector2(0.5f, 0.5f);
        crossRect.pivot = new Vector2(0.5f, 0.5f);
    }

    private RectTransform GetMinimapMapRect()
    {
        if (this.minimapMapRect == null)
        {
            this.minimapMapRect = UnityUtilities.GameObjectHardFind("Minimap")
                .GetComponent<RectTransform>();
        }

        return this.minimapMapRect;
    }

    private RectTransform GetMinimapPanelRect()
    {
        if (this.minimapPanelRect == null)
        {
            this.minimapPanelRect = UnityUtilities.GameObjectHardFind("MinimapPanel")
                .GetComponent<RectTransform>();
        }

        return this.minimapPanelRect;
    }

    private RectTransform GetCrosshairsRect()
    {
        if (this.crosshairsRect == null)
        {
            this.crosshairsRect = UnityUtilities.GameObjectHardFind("Crosshairs")
                .GetComponent<RectTransform>();
        }

        return this.crosshairsRect;
    }

    private Camera GetMainCamera()
    {
        if (this.mainCamera == null)
        {
            this.mainCamera = UnityUtilities.GameObjectHardFind("MainCamera")
                .GetComponent<Camera>();
        }

        return this.mainCamera;
    }
}
