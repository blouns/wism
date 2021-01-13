using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wism.Client.Core;

public class MapCrosshairs : MonoBehaviour
{
    public void LateUpdate()
    {
        //MoveCrosshairs();
    }

    private static void MoveCrosshairs()
    {
        var camera = GameObject.FindGameObjectWithTag("MainCamera");
        var cameraRect = camera.gameObject.GetComponent<RectTransform>();

        // BUGBUG: Always missing; maybe need a collider on the Grid?
        if (!Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, 500f))        
        {
            return;
        }

        var crossGO = UnityUtilities.GameObjectHardFind("Crosshairs");
        var crossRect = crossGO.GetComponent<RectTransform>();
        var minimapPanelRect = UnityUtilities.GameObjectHardFind("MinimapPanel").GetComponent<RectTransform>();

        // TODO: Change to using tilemap dimensions rather than World Map
        float newXPercent = hit.point.x / (float)World.Current.Map.GetUpperBound(0);
        float newYPercent = hit.point.y / (float)World.Current.Map.GetUpperBound(1);
        float newX = minimapPanelRect.sizeDelta.x * newXPercent - (minimapPanelRect.sizeDelta.x / 2f);
        float newY = minimapPanelRect.sizeDelta.y * newYPercent - (minimapPanelRect.sizeDelta.y / 2f);

        // TODO: Clamp to minimap
        crossRect.localPosition = new Vector3(newX, newY, 0f);
    }
}
