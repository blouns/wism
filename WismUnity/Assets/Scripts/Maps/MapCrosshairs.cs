using Assets.Scripts.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using Wism.Client.Core;

public class MapCrosshairs : MonoBehaviour
{
    private Camera mainCamera;
    private RectTransform crosshairsRect;
    private RectTransform minimapPanelRect;

    public void LateUpdate()
    {
        MoveCrosshairs();
    }

    private void MoveCrosshairs()
    {
        var camera = GetMainCamera();
        var center = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        
        var minimapPanelRect = GetMinimapPanelRect();

        float newXPercent = center.x / (float)World.Current.Map.GetUpperBound(0);
        float newYPercent = center.y / (float)World.Current.Map.GetUpperBound(1);
        float newX = minimapPanelRect.sizeDelta.x * newXPercent - (minimapPanelRect.sizeDelta.x / 2f);
        float newY = minimapPanelRect.sizeDelta.y * newYPercent - (minimapPanelRect.sizeDelta.y / 2f);

        // TODO: Clamp to minimap
        var crossRect = GetCrosshairsRect();
        crossRect.localPosition = new Vector3(newX, newY, 0f);
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
            this.mainCamera = GameObject.FindGameObjectWithTag("MainCamera")
                .GetComponent<Camera>();
        }

        return this.mainCamera;
    }
}
