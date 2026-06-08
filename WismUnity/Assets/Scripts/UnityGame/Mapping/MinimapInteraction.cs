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

        this.mainCameraFollow = UnityUtilities.GameObjectHardFind("MainCamera")
            .GetComponent<CameraFollow>();
        this.mainCamera = UnityUtilities.GameObjectHardFind("MainCamera")
            .GetComponent<Camera>();
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
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            var unityManager = GetUnityManager();
            unityManager.InputManager.SkipInput();

            RectTransform panelRect = UnityUtilities.GameObjectHardFind("Minimap")
                .GetComponent<RectTransform>();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRect,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint))
            {
                return;
            }

            var rect = panelRect.rect;
            float miniNormalX = Mathf.Clamp01((localPoint.x - rect.xMin) / rect.width);
            float miniNormalY = Mathf.Clamp01((localPoint.y - rect.yMin) / rect.height);

            float x = miniNormalX * World.Current.Map.GetUpperBound(0);
            float y = miniNormalY * World.Current.Map.GetUpperBound(1);

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
