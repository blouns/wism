using UnityEngine;
using Wism.Client.Core;

public class MapCrosshairs : MonoBehaviour
{
    private Camera mainCamera;
    private RectTransform crosshairsRect;
    private RectTransform minimapMapRect;

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
        var center = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));

        var minimapMapRect = GetMinimapMapRect();

        var mapWidth = Mathf.Max(1, World.Current.Map.GetLength(0));
        var mapHeight = Mathf.Max(1, World.Current.Map.GetLength(1));
        float newXPercent = Mathf.Clamp01(center.x / mapWidth);
        float newYPercent = Mathf.Clamp01(center.y / mapHeight);
        float newX = minimapMapRect.rect.width * newXPercent - (minimapMapRect.rect.width / 2f);
        float newY = minimapMapRect.rect.height * newYPercent - (minimapMapRect.rect.height / 2f);

        var crossRect = GetCrosshairsRect();
        var viewportHeight = camera.orthographicSize * 2f;
        var viewportWidth = viewportHeight * camera.aspect;
        crossRect.sizeDelta = new Vector2(
            Mathf.Clamp((viewportWidth / mapWidth) * minimapMapRect.rect.width, 8f, minimapMapRect.rect.width),
            Mathf.Clamp((viewportHeight / mapHeight) * minimapMapRect.rect.height, 8f, minimapMapRect.rect.height));
        crossRect.localPosition = new Vector3(
            Mathf.Clamp(newX, -(minimapMapRect.rect.width / 2f), minimapMapRect.rect.width / 2f),
            Mathf.Clamp(newY, -(minimapMapRect.rect.height / 2f), minimapMapRect.rect.height / 2f),
            0f);
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
