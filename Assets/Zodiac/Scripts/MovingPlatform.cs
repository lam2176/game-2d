using UnityEngine;

/// <summary>
/// Bục di chuyển qua lại giữa vị trí xuất phát và xuất phát + travel (Rigidbody2D Kinematic, MovePosition).
/// Chậm lại ở hai đầu (ease) để người chơi dễ canh nhảy. Người chơi đứng trên bục tự đi theo
/// (ZodiacPlayerController đọc <see cref="Velocity"/>).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [Tooltip("Quãng đường đi tính từ vị trí ban đầu (unit).")]
    public Vector2 travel = new Vector2(4f, 0f);
    [Tooltip("Thời gian đi hết một chiều (giây).")]
    [Min(0.1f)] public float duration = 2.5f;
    [Tooltip("Lệch pha (0..1) để nhiều bục không chạy giống hệt nhau.")]
    [Range(0f, 1f)] public float phase;

    Rigidbody2D rb;
    Vector2 start;

    public Vector2 Velocity { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        start = rb.position;
    }

    void FixedUpdate()
    {
        float t = Mathf.PingPong(Time.time / duration + phase * 2f, 1f);
        float eased = t * t * (3f - 2f * t);   // smoothstep
        Vector2 target = start + travel * eased;
        Velocity = (target - rb.position) / Time.fixedDeltaTime;
        rb.MovePosition(target);
    }

    void OnDrawGizmos()
    {
        Vector2 from = Application.isPlaying ? start : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(from, from + travel);
        Gizmos.DrawWireSphere(from + travel, 0.2f);
    }
}
