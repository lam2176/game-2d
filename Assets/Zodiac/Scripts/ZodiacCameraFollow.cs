using UnityEngine;

/// <summary>
/// Camera bám theo nhân vật, nhìn trước theo hướng đang chạy và không lộ ra ngoài vùng <see cref="bounds"/>.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ZodiacCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector2 offset = new Vector2(0f, 2f);
    [Tooltip("Khoảng nhìn trước theo hướng di chuyển.")]
    public float lookAhead = 2f;
    public float lookAheadSpeed = 4f;
    public float smoothTime = 0.2f;
    [Tooltip("Vùng thế giới camera được phép nhìn thấy.")]
    public Rect bounds = new Rect(-13f, -4f, 72f, 20f);

    Camera cam;
    Rigidbody2D targetBody;
    Vector3 velocity;
    float currentLookAhead;

    void Awake() => cam = GetComponent<Camera>();

    void Start()
    {
        if (target == null) return;
        targetBody = target.GetComponent<Rigidbody2D>();
        transform.position = DesiredPosition();
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (targetBody != null && Mathf.Abs(targetBody.linearVelocity.x) > 0.5f)
            currentLookAhead = Mathf.MoveTowards(currentLookAhead, Mathf.Sign(targetBody.linearVelocity.x) * lookAhead,
                                                 lookAheadSpeed * Time.deltaTime);
        transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref velocity, smoothTime);
    }

    Vector3 DesiredPosition()
    {
        float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
        Vector2 p = (Vector2)target.position + offset + Vector2.right * currentLookAhead;
        p.x = ClampCentered(p.x, bounds.xMin + halfW, bounds.xMax - halfW);
        p.y = ClampCentered(p.y, bounds.yMin + halfH, bounds.yMax - halfH);
        return new Vector3(p.x, p.y, transform.position.z);
    }

    static float ClampCentered(float value, float min, float max) =>
        min > max ? (min + max) / 2f : Mathf.Clamp(value, min, max);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
