using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    public static SmoothCameraFollow Instance;
    public Transform target=null;
    public float smoothSpeed = 0.125f; 
    public Vector3 offset;

    private void Awake()
    {
        Instance = this;
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
        transform.LookAt(target);
        //rotate camera around the target to look same direction as the car smoothly
        //transform.RotateAround(target.position, Vector3.up, target.eulerAngles.y - transform.eulerAngles.y);
    }
}