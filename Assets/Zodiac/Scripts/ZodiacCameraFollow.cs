using UnityEngine;

/// <summary>
/// Camera phối cảnh kiểu beat 'em up 2.5D: đứng lùi phía trước con đường, cúi nhìn xuống một góc cố định,
/// bám theo nhân vật dọc trục X (nhìn trước theo hướng chạy), chỉ dịch nhẹ theo chiều sâu Z để làn trước/sau luôn trong khung.
/// Không lộ ra ngoài [<see cref="minX"/>, <see cref="maxX"/>] — tính theo bề ngang khung hình tại làn giữa.
/// Khi đánh đợt quái (<see cref="EncounterZone"/>) khung hình bị khoá trong đấu trường bằng <see cref="Lock"/>.
/// Rung màn hình theo "trauma" (Squirrel Eiserloh, GDC 2016): độ rung = trauma², nhiễu Perlin, giảm dần theo thời gian;
/// phần rung cộng lên trên vị trí bám theo nên không làm lệch camera về lâu dài.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ZodiacCameraFollow : MonoBehaviour
{
    public Transform target;
    [Tooltip("Vị trí camera so với điểm nhìn (điểm nhìn = x nhân vật, làn giữa).")]
    public Vector3 offset = new Vector3(0f, 6.2f, -12.5f);
    [Tooltip("Độ cao điểm nhìn so với mặt đất.")]
    public float lookHeight = 1.3f;
    [Tooltip("Z của làn giữa con đường.")]
    public float laneCenterZ;
    [Tooltip("Camera dịch theo Z của nhân vật bao nhiêu phần (0 = đứng yên, 1 = bám hẳn).")]
    [Range(0f, 1f)] public float depthFollow = 0.25f;

    [Header("Trục X")]
    [Tooltip("Khoảng nhìn trước theo hướng di chuyển.")]
    public float lookAhead = 1.5f;
    public float lookAheadSpeed = 3f;
    public float smoothTime = 0.18f;

    [Tooltip("Giới hạn tâm khung hình theo X (đầu và cuối con đường).")]
    public float minX = -1000f, maxX = 1000f;

    [Header("Rung")]
    [Tooltip("Độ lệch tối đa khi trauma = 1 (unit).")]
    public float maxShakeOffset = 0.5f;
    [Tooltip("Góc nghiêng tối đa khi trauma = 1 (độ).")]
    public float maxShakeAngle = 2f;
    [Tooltip("Trauma giảm bao nhiêu mỗi giây.")]
    public float traumaDecay = 1.6f;

    /// <summary>Camera chính của màn (để GameFeel gọi rung).</summary>
    public static ZodiacCameraFollow Main { get; private set; }

    Camera cam;
    Rigidbody targetBody;
    float velocityX, velocityZ, currentLookAhead;
    Vector3 focus;
    float trauma;
    bool locked;
    float lockMin, lockMax;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Main = this;
    }

    void OnDestroy()
    {
        if (Main == this) Main = null;
    }

    /// <summary>Thêm độ rung (0..1, cộng dồn, tối đa 1).</summary>
    public void AddTrauma(float amount) => trauma = Mathf.Clamp01(trauma + amount);

    /// <summary>Bề ngang nửa khung hình (unit) tại làn giữa — để khoá đấu trường vừa khít màn hình.</summary>
    public float HalfViewWidth
    {
        get
        {
            float distance = new Vector2(offset.y - lookHeight, offset.z).magnitude;
            float halfHeight = distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return halfHeight * cam.aspect;
        }
    }

    /// <summary>Khoá tâm khung hình trong [xMin + nửa khung, xMax − nửa khung] (đấu trường).</summary>
    public void Lock(float xMin, float xMax)
    {
        locked = true;
        lockMin = xMin;
        lockMax = xMax;
    }

    public void Unlock() => locked = false;

    void Start()
    {
        if (target == null) return;
        targetBody = target.GetComponent<Rigidbody>();
        SnapToTarget();
    }

    /// <summary>Đặt camera ngay vào vị trí mong muốn, không làm mượt (dùng khi bắt đầu hoặc dịch chuyển nhân vật).</summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        velocityX = velocityZ = 0f;
        currentLookAhead = 0f;
        focus = DesiredFocus();
        Apply(Vector3.zero, 0f);
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (targetBody != null && Mathf.Abs(targetBody.linearVelocity.x) > 0.5f)
            currentLookAhead = Mathf.MoveTowards(currentLookAhead, Mathf.Sign(targetBody.linearVelocity.x) * lookAhead,
                                                 lookAheadSpeed * Time.deltaTime);

        Vector3 desired = DesiredFocus();
        focus.x = Mathf.SmoothDamp(focus.x, desired.x, ref velocityX, smoothTime);
        focus.z = Mathf.SmoothDamp(focus.z, desired.z, ref velocityZ, smoothTime * 2f);
        focus.y = desired.y;

        // Rung: dùng thời gian thực để vẫn rung trong lúc hitstop
        float shake = trauma * trauma;
        float t = Time.unscaledTime * 25f;
        Vector3 jitter = new Vector3(Mathf.PerlinNoise(t, 1.7f) - 0.5f, Mathf.PerlinNoise(3.3f, t) - 0.5f, 0f) * (2f * maxShakeOffset * shake);
        float roll = (Mathf.PerlinNoise(t, 7.1f) - 0.5f) * 2f * maxShakeAngle * shake;
        Apply(jitter, roll);
        trauma = Mathf.Max(0f, trauma - traumaDecay * Time.unscaledDeltaTime);
    }

    void Apply(Vector3 jitter, float roll)
    {
        Vector3 position = focus + offset;
        Quaternion look = Quaternion.LookRotation(focus - position);
        transform.SetPositionAndRotation(position + look * jitter, look * Quaternion.Euler(0f, 0f, roll));
    }

    Vector3 DesiredFocus()
    {
        float half = HalfViewWidth;
        float min = minX + half, max = maxX - half;
        if (locked)
        {
            min = Mathf.Max(min, lockMin + half);
            max = Mathf.Min(max, lockMax - half);
        }
        float x = ClampCentered(target.position.x + currentLookAhead, min, max);
        float z = laneCenterZ + (target.position.z - laneCenterZ) * depthFollow;
        return new Vector3(x, lookHeight, z);
    }

    static float ClampCentered(float value, float min, float max) =>
        min > max ? (min + max) / 2f : Mathf.Clamp(value, min, max);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(minX, 0f, laneCenterZ), new Vector3(maxX, 0f, laneCenterZ));
    }
}
