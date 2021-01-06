using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // TODO: Make this dynamic to tilemap
    private const float DefaultXMinClamp = 15.5f;
    private const float DefaultXMaxClamp = 69.5f;
    private const float DefaultYMinClamp = 8.8f;
    private const float DefaultYMaxClamp = 72f;

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
                SetCameraTarget(target);
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

    public void SetCameraTarget(Transform newTarget)
    {
        Vector3 lerpPosition = Vector3.Lerp(
                            transform.position, newTarget.position, speed) +
                            new Vector3(0f, 0f, -10f);

        Vector3 newPosition = new Vector3(
            Mathf.Clamp(lerpPosition.x, xMinClamp, xMaxClamp),
            Mathf.Clamp(lerpPosition.y, yMinClamp, yMaxClamp),
            lerpPosition.z);

        transform.position = newPosition;
    }
}
