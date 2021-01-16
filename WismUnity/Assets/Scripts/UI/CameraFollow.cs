using UnityEngine;
using Wism.Client.Core;

public class CameraFollow : MonoBehaviour
{
    // TODO: Make this dynamic to tilemap and screen
    private const float FullHDXMinClamp = 19.2f;
    private const float FullHDXMaxClamp = 65.8f;
    private const float FullHDYMinClamp = 8.5f;
    private const float FullHDYMaxClamp = 70.1f;

    private const float SurfacePro6XMinClamp = 13.75f;
    private const float SurfacePro6XMaxClamp = 71f;
    private const float SurfacePro6YMinClamp = 7.5f;
    private const float SurfacePro6YMaxClamp = 73f;

    public Transform target;
    public float speed;
    public float scale;
    public float xMinClamp;
    public float xMaxClamp;
    public float yMinClamp;
    public float yMaxClamp;
    public bool isFollowing;

    private Vector3 origin;
    private Vector3 difference;
    private bool isDragging;    

    Camera followCamera;

    // Start is called before the first frame update
    void Start()
    {
        followCamera = GetComponent<Camera>();
        followCamera.orthographicSize = (Screen.height / 100f) / this.scale;
    }

    public void LateUpdate()
    {
        UpdateCameraState();
        HandleRightClickDrag();
    }

    private void HandleRightClickDrag()
    {
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
            if (target && isFollowing)
            {
                SetCameraTargetLerp(target);
            }
        }

        if (isDragging == true)
        {
            Vector3 move = origin - difference;
            Vector3 newPosition = new Vector3(
                        Mathf.Clamp(move.x, xMinClamp, xMaxClamp),
                        Mathf.Clamp(move.y, yMinClamp, yMaxClamp),
                        move.z);
            followCamera.transform.position = newPosition;
        }
    }

    public void SetCameraTarget(Vector3 vector)
    {
        transform.position = vector + new Vector3(0f, 0f, -10f);
    }

    public void SetCameraTargetLerp(Transform newTarget)
    {
        SetCameraTargetLerp(newTarget.position);
    }

    public void SetCameraTargetLerp(Vector3 vector)
    {
        Vector3 lerpPosition = Vector3.Lerp(
                            transform.position, vector, speed) +
                            new Vector3(0f, 0f, -10f);

        Vector3 newPosition = new Vector3(
            Mathf.Clamp(lerpPosition.x, xMinClamp, xMaxClamp),
            Mathf.Clamp(lerpPosition.y, yMinClamp, yMaxClamp),
            lerpPosition.z);

        transform.position = newPosition;
    }

    private void UpdateCameraState()
    {        
        if (Game.Current.GameState == GameState.MovingArmy)
        {
            isFollowing = true;
        }
        else
        {
            isFollowing = false;
        }
    }
}
