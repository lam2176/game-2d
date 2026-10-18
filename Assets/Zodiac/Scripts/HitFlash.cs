using UnityEngine;

/// <summary>
/// Phản hồi khi trúng đòn: tô màu các SpriteRenderer trong flashDuration, sau đó nhấp nháy mờ
/// trong lúc Health còn bất tử. Không cần animation — chỉ đổi SpriteRenderer.color.
/// </summary>
[RequireComponent(typeof(Health))]
public class HitFlash : MonoBehaviour
{
    [Tooltip("Để trống = mọi SpriteRenderer con.")]
    public SpriteRenderer[] renderers;
    public Color flashColor = new Color(1f, 0.35f, 0.35f);
    [Min(0f)] public float flashDuration = 0.1f;
    [Min(0.01f)] public float blinkInterval = 0.08f;

    Health health;
    Color[] baseColors;
    float flashUntil = float.NegativeInfinity;

    void Awake()
    {
        health = GetComponent<Health>();
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        health.Damaged += (_, __) => flashUntil = Time.time + flashDuration;
    }

    void LateUpdate()
    {
        bool flashing = Time.time < flashUntil;
        bool dimmed = !flashing && health.IsInvulnerable && !health.IsDead &&
                      Mathf.FloorToInt(Time.time / blinkInterval) % 2 == 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            Color c = flashing ? flashColor : baseColors[i];
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
