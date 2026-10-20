using UnityEngine;

/// <summary>Dấu "!" khi quái phát hiện người chơi: bật to ra, nổi lên một chút, rồi mờ dần và tự huỷ.</summary>
public class AlertMark : MonoBehaviour
{
    public float lifetime = 0.7f;

    SpriteRenderer[] renderers;
    Vector3 start;
    float t;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
        start = transform.position;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = t / lifetime;
        float pop = k < 0.15f ? Mathf.Lerp(0.3f, 1.3f, k / 0.15f) : Mathf.Lerp(1.3f, 1f, Mathf.Clamp01((k - 0.15f) / 0.15f));
        transform.localScale = Vector3.one * pop;
        transform.position = start + Vector3.up * (0.3f * Mathf.Clamp01(k * 2f));
        float a = 1f - Mathf.Clamp01((k - 0.6f) / 0.4f);
        foreach (var r in renderers)
        {
            var c = r.color;
            c.a = a;
            r.color = c;
        }
        if (t >= lifetime) Destroy(gameObject);
    }
}
