using UnityEngine;

/// <summary>
/// Bóng tròn mờ dưới chân (kiểu beat 'em up cổ điển): luôn nằm sát mặt đất ngay dưới chủ thể, nhỏ và nhạt dần
/// khi chủ thể lên cao. Cho người chơi thấy rõ mình / quái đang ở làn nào và sẽ đáp xuống đâu — bóng đổ thật
/// từ mặt trời đổ chéo nên không dùng để canh làn được.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BlobShadow : MonoBehaviour
{
    [Tooltip("Chủ thể được bám theo (để trống = object cha).")]
    public Transform target;
    public LayerMask groundLayers = ~0;
    [Tooltip("Đường kính bóng (unit) khi đứng trên đất.")]
    public float size = 1f;
    [Range(0f, 1f)] public float opacity = 0.4f;
    [Tooltip("Cao hơn mặt đất từng này thì bóng nhỏ nhất.")]
    public float fadeHeight = 4f;

    SpriteRenderer sr;
    Color baseColor;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (target == null) target = transform.parent;
        if (sr.sprite == null) sr.sprite = ZodiacFx.SoftCircle;
        baseColor = new Color(0f, 0f, 0f, opacity);
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 p = target.position;
        float ground = 0f;
        if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 30f, groundLayers, QueryTriggerInteraction.Ignore))
            ground = hit.point.y;
        float k = 1f - Mathf.Clamp01((p.y - ground) / Mathf.Max(0.01f, fadeHeight)) * 0.6f;
        transform.SetPositionAndRotation(new Vector3(p.x, ground + 0.03f, p.z), Quaternion.Euler(90f, 0f, 0f));
        transform.localScale = new Vector3(size * k, size * 0.55f * k, 1f);
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * k);
    }
}
