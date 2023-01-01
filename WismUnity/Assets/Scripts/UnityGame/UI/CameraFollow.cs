using UnityEngine;
using Wism.Client.Core;

public class CameraFollow : MonoBehaviour
{
    // TODO: Make this dynamic to tilemap and screen
    private const float FullHDXMinClamp = 19.2f;
    private const float FullHDXMaxClamp = 65.8f;
    private const float FullHDYMinClamp = 8.5f;
    private const float FullHDYMaxClamp = 70.1f;

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

    // Start is called before the first frame update
    void Start()
    {
        this.followCamera = GetComponent<Camera>();
        this.followCamera.orthographicSize = (Screen.height / 100f) / this.scale;
    }

    public void LateUpdate()
    {
        if (!Game.IsInitialized())
        {
            return;
        }

        HandleCameraMove();
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
