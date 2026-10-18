using UnityEngine;

/// <summary>Camera bám theo mục tiêu (nhân vật) một cách mượt.</summary>
public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector2 offset = new Vector2(0f, 1.5f);
    public float smoothTime = 0.15f;

    Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 goal = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}
