using UnityEngine;

/// <summary>
/// Vệt chém hình lưỡi liềm: khi bật lên thì quét một cung (xoay từ trên xuống, hoặc ngược lại ở đòn 2),
/// phóng to dần và mờ đi trong thời gian hitbox có hiệu lực. Đòn kết combo to và sáng hơn.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SlashEffect : MonoBehaviour
{
    public Color color = new Color(1f, 1f, 0.9f, 0.85f);
    public Color heavyColor = new Color(1f, 0.85f, 0.4f, 0.95f);
    [Tooltip("Góc quét (độ).")]
    public float sweep = 70f;

    SpriteRenderer sr;
    float duration = 0.1f, startTime;
    bool reverse, heavy;
    Quaternion baseRotation;
    Vector3 baseScale;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    public void Configure(float activeTime, bool reversed, bool heavyHit)
    {
        duration = Mathf.Max(0.02f, activeTime);
        reverse = reversed;
        heavy = heavyHit;
        baseRotation = transform.localRotation;
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        startTime = Time.time;
        if (baseScale == Vector3.zero) { baseRotation = transform.localRotation; baseScale = transform.localScale; }
        Apply(0f);
    }

    void Update() => Apply(Mathf.Clamp01((Time.time - startTime) / duration));

    void Apply(float k)
    {
        float e = 1f - (1f - k) * (1f - k);
        float a = Mathf.Lerp(sweep / 2f, -sweep / 2f, e) * (reverse ? -1f : 1f);
        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, a);
        transform.localScale = baseScale * Mathf.Lerp(0.75f, heavy ? 1.2f : 1.05f, e);
        Color c = heavy ? heavyColor : color;
        c.a *= 1f - k * 0.7f;
        sr.color = c;
        sr.flipY = reverse;
    }
}
