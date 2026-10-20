using UnityEngine;

/// <summary>
/// Phản hồi khi trúng đòn: tô màu các SpriteRenderer trong flashDuration, sau đó nhấp nháy mờ
/// trong lúc Health còn bất tử. Không cần animation — chỉ đổi SpriteRenderer.color.
/// Các script khác (tàng hình, nhiễm độc) nhân thêm màu qua <see cref="tint"/>.
/// </summary>
[RequireComponent(typeof(Health))]
public class HitFlash : MonoBehaviour
{
    [Tooltip("Để trống = mọi SpriteRenderer con.")]
    public SpriteRenderer[] renderers;
    public Color flashColor = new Color(1f, 0.35f, 0.35f);
    [Min(0f)] public float flashDuration = 0.1f;
    [Min(0.01f)] public float blinkInterval = 0.08f;
    [Tooltip("Màu nhân thêm vào màu gốc (trắng = không đổi).")]
    public Color tint = Color.white;
    [Tooltip("Tắt nhấp nháy khi bất tử do chiêu (lướt, tàng hình) — chỉ nháy sau khi trúng đòn.")]
    public bool blinkOnlyAfterHit = true;

    Health health;
    Color[] baseColors;
    float flashUntil = float.NegativeInfinity;
    float blinkUntil = float.NegativeInfinity;

    void Awake()
    {
        health = GetComponent<Health>();
        CaptureRenderers();
        health.Damaged += _ =>
        {
            flashUntil = Time.time + flashDuration;
            blinkUntil = Time.time + health.invulnerableTime;
        };
    }

    /// <summary>Lưu lại màu gốc — gọi lại khi đổi sprite/renderer lúc chạy.</summary>
    public void CaptureRenderers()
    {
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
    }

    void LateUpdate()
    {
        bool flashing = Time.time < flashUntil;
        bool blinking = blinkOnlyAfterHit ? Time.time < blinkUntil : health.IsInvulnerable;
        bool dimmed = !flashing && blinking && !health.IsDead &&
                      Mathf.FloorToInt(Time.time / blinkInterval) % 2 == 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            Color c = flashing ? flashColor : baseColors[i] * tint;
            if (dimmed) c.a *= 0.35f;
            renderers[i].color = c;
        }
    }

    void OnDisable()
    {
        if (baseColors == null) return;
        for (int i = 0; i < renderers.Length; i++) renderers[i].color = baseColors[i];
    }
}
