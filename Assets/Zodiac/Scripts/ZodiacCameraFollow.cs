using UnityEngine;

/// <summary>
/// Camera bám theo nhân vật trong LateUpdate (sau khi Rigidbody2D nội suy xong nên không giật).
/// Trục X: nhìn trước theo hướng chạy. Trục Y: giữ khung hình đứng yên khi nhảy bình thường,
/// chỉ dời theo khi tiếp đất ở độ cao mới, khi rơi xuống thấp hơn, hoặc khi lên quá cao.
/// Không lộ ra ngoài vùng <see cref="bounds"/>.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ZodiacCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector2 offset = new Vector2(0f, 2f);

    [Header("Trục X")]
    [Tooltip("Khoảng nhìn trước theo hướng di chuyển.")]
    public float lookAhead = 2f;
    public float lookAheadSpeed = 4f;
    public float smoothTimeX = 0.15f;

    [Header("Trục Y")]
    public float smoothTimeY = 0.25f;
    [Tooltip("Lên cao hơn mức tiếp đất gần nhất quá khoảng này thì camera mới bắt đầu theo lên.")]
    public float jumpFollowMargin = 3.5f;

    [Tooltip("Vùng thế giới camera được phép nhìn thấy.")]
    public Rect bounds = new Rect(-13f, -4f, 72f, 20f);

    Camera cam;
    Rigidbody2D targetBody;
    ZodiacPlayerController targetPlayer;
    float velocityX, velocityY;
    float currentLookAhead, focusY;

    void Awake() => cam = GetComponent<Camera>();

    void Start()
    {
        if (target == null) return;
        targetBody = target.GetComponent<Rigidbody2D>();
        targetPlayer = target.GetComponent<ZodiacPlayerController>();
        SnapToTarget();
    }

    /// <summary>Đặt camera ngay vào vị trí mong muốn, không làm mượt (dùng khi bắt đầu hoặc dịch chuyển nhân vật).</summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        focusY = target.position.y;
        velocityX = velocityY = 0f;
        transform.position = DesiredPosition();
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (targetBody != null && Mathf.Abs(targetBody.linearVelocity.x) > 0.5f)
            currentLookAhead = Mathf.MoveTowards(currentLookAhead, Mathf.Sign(targetBody.linearVelocity.x) * lookAhead,
                                                 lookAheadSpeed * Time.deltaTime);

        float y = target.position.y;
        bool grounded = targetPlayer == null || targetPlayer.IsGrounded;
        if (grounded || y < focusY) focusY = y;
        else if (y > focusY + jumpFollowMargin) focusY = y - jumpFollowMargin;

        Vector3 desired = DesiredPosition();
        Vector3 p = transform.position;
        p.x = Mathf.SmoothDamp(p.x, desired.x, ref velocityX, smoothTimeX);
        p.y = Mathf.SmoothDamp(p.y, desired.y, ref velocityY, smoothTimeY);
        transform.position = p;
    }

    Vector3 DesiredPosition()
    {
        float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
        float x = target.position.x + offset.x + currentLookAhead;
        float y = focusY + offset.y;
        x = ClampCentered(x, bounds.xMin + halfW, bounds.xMax - halfW);
        y = ClampCentered(y, bounds.yMin + halfH, bounds.yMax - halfH);
        return new Vector3(x, y, transform.position.z);
    }

    static float ClampCentered(float value, float min, float max) =>
        min > max ? (min + max) / 2f : Mathf.Clamp(value, min, max);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
