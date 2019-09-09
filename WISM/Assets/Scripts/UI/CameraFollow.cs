using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float speed;
    public float scale;
    Camera followCamera;

    // Start is called before the first frame update
    void Start()
    {
        followCamera = GetComponent<Camera>();    
    }

    // Update is called once per frame
    void Update()
    {
        followCamera.orthographicSize = (Screen.height / 100f) / this.scale;

        if (target)
        {
            transform.position = Vector3.Lerp(transform.position, target.position, speed) + new Vector3(0f, 0f, -10f);
        }
    }
}
