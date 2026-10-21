using System.Collections;
using UnityEngine;

/// <summary>
/// Đồ đập vỡ được trong khu (hũ sành, rương gỗ): nằm trên layer Enemy để đòn đánh của người chơi trúng,
/// mỗi đòn rung lắc, hết máu thì vỡ tung thành mảnh và rơi xu / đồ theo bảng <see cref="loot"/>.
/// Không phải quái: không tính vào việc dọn khu, không cộng chuỗi đòn.
/// </summary>
[RequireComponent(typeof(Health))]
public class Breakable : MonoBehaviour
{
    [Tooltip("Phần hình (rung khi trúng đòn, ẩn khi vỡ).")]
    public Transform visual;
    public Color debrisColor = new Color(0.55f, 0.35f, 0.2f);
    public LootEntry[] loot = System.Array.Empty<LootEntry>();
    public Vector2Int coinDrop = new Vector2Int(1, 3);

    Health health;
    float shake;
    Vector3 visualStart;

    void Awake()
    {
        health = GetComponent<Health>();
        health.Damaged += OnDamaged;
        health.Died += OnBroken;
        if (visual != null) visualStart = visual.localPosition;
    }

    void OnDamaged(Hit hit)
    {
        shake = 0.25f;
        var b = Bounds;
        ZodiacParticles.Debris(b.center, debrisColor, 4, b.extents.x * 0.6f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.footstepWood, 0.7f, 0.15f, transform.position);
    }

    Bounds Bounds => TryGetComponent(out Collider c) ? c.bounds : new Bounds(transform.position + Vector3.up * 0.4f, Vector3.one * 0.8f);

    void Update()
    {
        if (visual == null || shake <= 0f) return;
        shake = Mathf.Max(0f, shake - Time.deltaTime);
        float k = shake * 0.35f;
        visual.localPosition = visualStart + new Vector3(Mathf.Sin(Time.time * 70f) * k, 0f, Mathf.Cos(Time.time * 55f) * k * 0.5f);
        visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 60f) * shake * 30f);
    }

    void OnBroken()
    {
        var b = Bounds;
        ZodiacParticles.Debris(b.center, debrisColor, 16, b.extents.x);
        ZodiacFx.Puff(b.center, new Color(0.6f, 0.5f, 0.4f, 0.7f), 1.1f);
        GameFeel.Shake(0.08f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.enemyDeath, 0.6f, 0.2f, transform.position);
        LootPickup.DropFrom(loot, coinDrop, transform.position, ZodiacDifficulty.LootScale);
        foreach (var c in GetComponents<Collider>()) c.enabled = false;
        StartCoroutine(Vanish());
    }

    IEnumerator Vanish()
    {
        if (visual != null)
            for (float t = 0f; t < 0.12f; t += Time.deltaTime)
            {
                visual.localScale = Vector3.one * (1f + t * 3f);
                yield return null;
            }
        gameObject.SetActive(false);
    }
}
