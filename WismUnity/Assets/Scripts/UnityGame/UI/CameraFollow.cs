using UnityEngine;
using Wism.Client.Core;

// Frame the selection after UnityManager has drawn its new highlight in LateUpdate.
[DefaultExecutionOrder(100)]
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float speed;
    public float scale;

    public float xMinClamp;
    public float xMaxClamp;
    public float yMinClamp;
    public float yMaxClamp;

    private Vector3 origin;
    private Vector3 difference;
    private bool isDragging;
    private bool centered;
    private Camera followCamera;
    private Assets.Scripts.Tilemaps.WorldTilemap worldTilemap;
    private int lastMapWidth;
    private int lastMapHeight;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private float lastCameraAspect;
    private float lastOrthographicSize;

    // Start is called before the first frame update
    void Start()
    {
        this.followCamera = GetComponent<Camera>();
        ConfigureBoundsFromCurrentWorld();
    }

    public void LateUpdate()
    {
        if (!Game.IsInitialized())
        {
            return;
        }

        ConfigureBoundsFromCurrentWorld();
        HandleCameraMove();
    }

    public void ConfigureBoundsFromCurrentWorld()
    {
        if (this.followCamera == null)
        {
            this.followCamera = GetComponent<Camera>();
        }

        if (this.followCamera == null)
        {
            return;
        }

        if (this.scale > 0f)
        {
            this.followCamera.orthographicSize = (Screen.height / 100f) / this.scale;
        }

        if (!Game.IsInitialized() || World.Current?.Map == null)
        {
            return;
        }

        var mapWidth = World.Current.Map.GetLength(0);
        var mapHeight = World.Current.Map.GetLength(1);
        if (mapWidth <= 0 || mapHeight <= 0)
        {
            return;
        }

        if (mapWidth == this.lastMapWidth &&
            mapHeight == this.lastMapHeight &&
            Screen.width == this.lastScreenWidth &&
            Screen.height == this.lastScreenHeight &&
            Mathf.Approximately(this.followCamera.aspect, this.lastCameraAspect) &&
            Mathf.Approximately(this.followCamera.orthographicSize, this.lastOrthographicSize))
        {
            return;
        }

        this.lastMapWidth = mapWidth;
        this.lastMapHeight = mapHeight;
        this.lastScreenWidth = Screen.width;
        this.lastScreenHeight = Screen.height;
        this.lastCameraAspect = this.followCamera.aspect;
        this.lastOrthographicSize = this.followCamera.orthographicSize;

        // Keep the camera center on playable terrain, not its entire viewport.
        // Edge cities can then be centered without adding traversable map cells.
        if (this.worldTilemap == null)
            this.worldTilemap = FindFirstObjectByType<Assets.Scripts.Tilemaps.WorldTilemap>();
        var first = this.worldTilemap != null
            ? this.worldTilemap.ConvertGameToUnityVector(0, 0) : new Vector3(0.5f, 0.5f);
        var last = this.worldTilemap != null
            ? this.worldTilemap.ConvertGameToUnityVector(mapWidth - 1, mapHeight - 1)
            : new Vector3(mapWidth - 0.5f, mapHeight - 0.5f);
        this.xMinClamp = Mathf.Min(first.x, last.x);
        this.xMaxClamp = Mathf.Max(first.x, last.x);
        this.yMinClamp = Mathf.Min(first.y, last.y);
        this.yMaxClamp = Mathf.Max(first.y, last.y);
        this.followCamera.clearFlags = CameraClearFlags.SolidColor;
        this.followCamera.backgroundColor = new Color32(28, 30, 32, 255);

        var position = this.transform.position;
        this.transform.position = new Vector3(
            Mathf.Clamp(position.x, this.xMinClamp, this.xMaxClamp),
            Mathf.Clamp(position.y, this.yMinClamp, this.yMaxClamp), position.z);
    }

    public void ResetCamera()
    {
        this.centered = false;
        this.isDragging = false;
    }

    private void HandleCameraMove()
    {
        // Right mouse button drags the screen
        if (Input.GetMouseButton(1))
        {
            this.difference = (this.followCamera.ScreenToWorldPoint(Input.mousePosition)) - this.followCamera.transform.position;
            if (this.isDragging == false)
            {
                this.isDragging = true;
                this.origin = this.followCamera.ScreenToWorldPoint(Input.mousePosition);
            }
        }
        else
        {
            this.isDragging = false;
            if (this.target != null &&
               (Game.Current.GameState == GameState.MovingArmy))
            {
                // Linearly interpolate
                SetCameraTargetLerp(this.target);
            }
            // Snap to a location
            else if (this.target != null &&
                    (Game.Current.GameState != GameState.SelectedArmy))
            {
                // Snap to target
                SetCameraTarget(this.target.position);
                this.target = null;
            }
            //  Snap to selected army, but only once to avoid "snap-back"
            else if (this.target != null &&
                    (Game.Current.GameState == GameState.SelectedArmy) &&
                    !this.centered)
            {
                SetCameraTarget(this.target.position);
                this.centered = true;
            }
        }

        if (this.isDragging == true)
        {
            Vector3 move = this.origin - this.difference;
            this.followCamera.transform.position = ClampVectorToTilemap(move);
        }
    }

    private Vector3 ClampVectorToTilemap(Vector3 vector)
    {
        ConfigureBoundsFromCurrentWorld();

        return new Vector3(
            Mathf.Clamp(vector.x, this.xMinClamp, this.xMaxClamp),
            Mathf.Clamp(vector.y, this.yMinClamp, this.yMaxClamp),
            vector.z);
    }

    public void SetCameraTarget(Vector3 vector)
    {
        this.transform.position = ClampVectorToTilemap(vector + new Vector3(0f, 0f, -10f));
    }

    public void SetCameraTargetLerp(Transform newTarget)
    {
        SetCameraTargetLerp(newTarget.position);
    }

    public void SetCameraTargetLerp(Vector3 vector)
    {
        Vector3 lerpPosition = Vector3.Lerp(this.transform.position, vector, this.speed) +
                            new Vector3(0f, 0f, -10f);

        this.transform.position = ClampVectorToTilemap(lerpPosition);
    }
}
