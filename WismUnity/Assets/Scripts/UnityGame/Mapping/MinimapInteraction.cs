using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using Wism.Client.Core;

public class MinimapInteraction : MonoBehaviour, IPointerDownHandler
{
    private Camera minimapCamera;
    private Camera mainCamera;
    private CameraFollow mainCameraFollow;
    private UnityManager unityManager;

    void Start()
    {
        AddPhysics2DRaycaster();
        EnsureCameraReferences();
    }

    void AddPhysics2DRaycaster()
    {
        Physics2DRaycaster physicsRaycaster = GameObject.FindObjectOfType<Physics2DRaycaster>();
        if (physicsRaycaster == null)
        {
            this.minimapCamera = GameObject.FindGameObjectWithTag("MinimapCamera")
                .GetComponent<Camera>();
            this.minimapCamera.gameObject
                .AddComponent<Physics2DRaycaster>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!Game.IsInitialized() || World.Current?.Map == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            var unityManager = GetUnityManager();
            unityManager.InputManager.SkipInput();

            RectTransform panelRect = UnityUtilities.GameObjectHardFind("Minimap")
                .GetComponent<RectTransform>();
            if (!TryProjectMinimapScreenPointToMapTarget(
                panelRect,
                eventData.position,
                eventData.pressEventCamera,
                World.Current.Map.GetLength(0),
                World.Current.Map.GetLength(1),
                out var target))
            {
                return;
            }

            EnsureCameraReferences();
            this.mainCameraFollow.SetCameraTarget(target);
        }
    }

    public static bool TryProjectMinimapScreenPointToMapTarget(
        RectTransform panelRect,
        Vector2 screenPosition,
        Camera eventCamera,
        int mapWidth,
        int mapHeight,
        out Vector3 target)
    {
        target = Vector3.zero;
        if (panelRect == null || mapWidth <= 0 || mapHeight <= 0)
        {
            return false;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRect,
            screenPosition,
            eventCamera,
            out var localPoint))
        {
            return false;
        }

        var rect = panelRect.rect;
        if (localPoint.x < rect.xMin || localPoint.x > rect.xMax ||
            localPoint.y < rect.yMin || localPoint.y > rect.yMax)
        {
            return false;
        }

        float miniNormalX = Mathf.Clamp01((localPoint.x - rect.xMin) / rect.width);
        float miniNormalY = Mathf.Clamp01((localPoint.y - rect.yMin) / rect.height);
        target = new Vector3(miniNormalX * mapWidth, miniNormalY * mapHeight, 0f);
        return true;
    }

    private UnityManager GetUnityManager()
    {
        if (this.unityManager == null)
        {
            this.unityManager = UnityUtilities.GameObjectHardFind("UnityManager")
                .GetComponent<UnityManager>();
        }

        return this.unityManager;
    }

    private void EnsureCameraReferences()
    {
        if (this.mainCameraFollow == null)
        {
            this.mainCameraFollow = UnityUtilities.GameObjectHardFind("MainCamera")
                .GetComponent<CameraFollow>();
        }

        if (this.mainCamera == null)
        {
            this.mainCamera = UnityUtilities.GameObjectHardFind("MainCamera")
                .GetComponent<Camera>();
        }
    }
}
