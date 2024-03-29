using Assets.Scripts.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using Wism.Client.Core;

public class MinimapInteraction : MonoBehaviour, IPointerDownHandler
{
    private Camera minimapCamera;
    private Camera mainCamera;
    private CameraFollow mainCameraFollow;
    private Vector3? minimapNormalVector;
    private UnityManager unityManager;

    void Start()
    {
        AddPhysics2DRaycaster();

        this.mainCameraFollow = UnityUtilities.GameObjectHardFind("MainCamera")
            .GetComponent<CameraFollow>();
        this.mainCamera = UnityUtilities.GameObjectHardFind("MainCamera")
            .GetComponent<Camera>();
    }

    private Vector3 GetMinimapNormal()
    {
        if (this.minimapNormalVector == null)
        {
            RectTransform canvasRect = UnityUtilities.GameObjectHardFind("CameraCanvas").
                        GetComponent<RectTransform>();
            RectTransform panelRect = UnityUtilities.GameObjectHardFind("Minimap").
                GetComponent<RectTransform>();

            this.minimapNormalVector = new Vector3(
                panelRect.sizeDelta.x / canvasRect.sizeDelta.x,
                panelRect.sizeDelta.y / canvasRect.sizeDelta.y,
                0f);
        }

        return this.minimapNormalVector.Value;
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
        // Center tuning
        const float xOffset = 6f;
        const float yOffset = 7f;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            var unityManager = GetUnityManager();
            unityManager.InputManager.SkipInput();

            var minimapNormal = GetMinimapNormal();
            var viewportVector = this.mainCamera.ScreenToViewportPoint(
                eventData.pointerCurrentRaycast.screenPosition);
            float miniNormalX = 1 - (1 - viewportVector.x) / minimapNormal.x;
            float miniNormalY = 1 - (1 - viewportVector.y) / minimapNormal.y;

            float x = miniNormalX * (World.Current.Map.GetUpperBound(0) + 1) + xOffset;
            float y = miniNormalY * (World.Current.Map.GetUpperBound(1) + 1) + yOffset;

            this.mainCameraFollow.SetCameraTarget(new Vector3(x, y, 0f));
        }
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
}
