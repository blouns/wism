using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using Assets.Scripts.UI;
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
        followCamera = GetComponent<Camera>();
        followCamera.orthographicSize = (Screen.height / 100f) / this.scale;
    }

    public void LateUpdate()
    {
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
            difference = (followCamera.ScreenToWorldPoint(Input.mousePosition)) - followCamera.transform.position;
            if (isDragging == false)
            {
                isDragging = true;
                origin = followCamera.ScreenToWorldPoint(Input.mousePosition);
            }
        }       
        else
        {
            isDragging = false;
            if (target != null && 
               (Game.Current.GameState == GameState.MovingArmy))
            {
                // Linearly interpolate
                SetCameraTargetLerp(target);
            }
            // Snap to a location
            else if (target != null && 
                    (Game.Current.GameState != GameState.SelectedArmy))
            {
                // Snap to target
                SetCameraTarget(target.position);
                target = null;
            }
            //  Snap to selected army, but only once to avoid "snap-back"
            else if (target != null && 
                    (Game.Current.GameState == GameState.SelectedArmy) && 
                    !centered)
            {
                SetCameraTarget(target.position);
                centered = true;
            }
        }

        if (isDragging == true)
        {
            Vector3 move = origin - difference;
            followCamera.transform.position = ClampVectorToTilemap(move);
        }
    }

    private Vector3 ClampVectorToTilemap(Vector3 vector)
    {
        return new Vector3(
            Mathf.Clamp(vector.x, xMinClamp, xMaxClamp),
            Mathf.Clamp(vector.y, yMinClamp, yMaxClamp),
            vector.z);
    }

    public void SetCameraTarget(Vector3 vector)
    {
        transform.position = ClampVectorToTilemap(vector + new Vector3(0f, 0f, -10f));
    }

    public void SetCameraTargetLerp(Transform newTarget)
    {
        SetCameraTargetLerp(newTarget.position);
    }

    public void SetCameraTargetLerp(Vector3 vector)
    {
        Vector3 lerpPosition = Vector3.Lerp(transform.position, vector, speed) +
                            new Vector3(0f, 0f, -10f);

        transform.position = ClampVectorToTilemap(lerpPosition);
    }
}