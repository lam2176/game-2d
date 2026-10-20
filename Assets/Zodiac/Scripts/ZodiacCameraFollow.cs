using UnityEngine;

/// <summary>
/// Camera bám theo nhân vật trong LateUpdate (sau khi Rigidbody2D nội suy xong nên không giật).
/// Trục X: nhìn trước theo hướng chạy. Trục Y: giữ khung hình đứng yên khi nhảy bình thường,
/// chỉ dời theo khi tiếp đất ở độ cao mới, khi rơi xuống thấp hơn, hoặc khi lên quá cao.
/// Không lộ ra ngoài vùng <see cref="bounds"/>.
/// Rung màn hình theo "trauma" (Squirrel Eiserloh, GDC 2016): độ rung = trauma², nhiễu Perlin, giảm dần theo thời gian;
/// phần rung cộng lên trên vị trí bám theo nên không làm lệch camera về lâu dài.
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

    [Header("Rung")]
    [Tooltip("Độ lệch tối đa khi trauma = 1 (unit).")]
    public float maxShakeOffset = 0.6f;
    [Tooltip("Góc nghiêng tối đa khi trauma = 1 (độ).")]
    public float maxShakeAngle = 2.5f;
    [Tooltip("Trauma giảm bao nhiêu mỗi giây.")]
    public float traumaDecay = 1.6f;

    /// <summary>Camera chính của màn (để GameFeel gọi rung).</summary>
    public static ZodiacCameraFollow Main { get; private set; }

    Camera cam;
    Rigidbody2D targetBody;
    ZodiacPlayerController targetPlayer;
    float velocityX, velocityY;
    float currentLookAhead, focusY;
    Vector3 followPosition;
    float trauma;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Main = this;
        followPosition = transform.position;
    }

    void OnDestroy()
    {
        if (Main == this) Main = null;
    }

    /// <summary>Thêm độ rung (0..1, cộng dồn, tối đa 1).</summary>
    public void AddTrauma(float amount) => trauma = Mathf.Clamp01(trauma + amount);

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
        followPosition = DesiredPosition();
        transform.position = followPosition;
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
        Vector3 p = followPosition;
        p.x = Mathf.SmoothDamp(p.x, desired.x, ref velocityX, smoothTimeX);
        p.y = Mathf.SmoothDamp(p.y, desired.y, ref velocityY, smoothTimeY);
        followPosition = p;

        // Rung: dùng thời gian thực để vẫn rung trong lúc hitstop
        float shake = trauma * trauma;
        float t = Time.unscaledTime * 25f;
        Vector3 offset = new Vector3(Mathf.PerlinNoise(t, 1.7f) - 0.5f, Mathf.PerlinNoise(3.3f, t) - 0.5f, 0f) * (2f * maxShakeOffset * shake);
        transform.SetPositionAndRotation(p + offset,
            Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(t, 7.1f) - 0.5f) * 2f * maxShakeAngle * shake));
        trauma = Mathf.Max(0f, trauma - traumaDecay * Time.unscaledDeltaTime);
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
