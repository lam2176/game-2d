using System.Collections;
using UnityEngine;

/// <summary>
/// Quái đơn giản: đi qua lại quanh điểm xuất phát, quay đầu khi gặp tường, mép vực hoặc hết quãng tuần tra.
/// Chạm vào người chơi thì gây sát thương (Health của người chơi có bất tử ngắn nên không trừ máu mỗi frame).
/// Trúng đòn thì bị đẩy lùi và khựng lại; hết máu thì dừng, mờ dần rồi biến mất.
/// Collider thân không va chạm vật lý với layer Player (Exclude Layers) để hai bên không đẩy nhau.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Tuần tra")]
    [Min(0f)] public float moveSpeed = 2f;
    [Tooltip("Đi tối đa bao xa mỗi phía tính từ điểm xuất phát.")]
    [Min(0f)] public float patrolDistance = 3f;
    public LayerMask groundLayer;
    [Tooltip("Object con được lật theo hướng đi.")]
    public Transform visual;

    [Header("Gây sát thương khi chạm")]
    [Min(0)] public int contactDamage = 1;
    public LayerMask playerLayer;

    [Header("Bị đánh")]
    public Vector2 knockback = new Vector2(4f, 4f);
    [Min(0f)] public float stunTime = 0.3f;
    [Min(0f)] public float deathFadeTime = 0.4f;

    Rigidbody2D rb;
    Collider2D body;
    Health health;
    ContactFilter2D playerFilter;
    readonly Collider2D[] contacts = new Collider2D[2];
    float startX;
    float direction = 1f;
    float stunnedUntil = float.NegativeInfinity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        health = GetComponent<Health>();
        startX = transform.position.x;
        playerFilter = new ContactFilter2D { useTriggers = false };
        playerFilter.SetLayerMask(playerLayer);
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    void FixedUpdate()
    {
        if (health.IsDead) return;
        DealContactDamage();
        if (Time.time < stunnedUntil || !IsGrounded()) return;

        if (ShouldTurn()) direction = -direction;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        if (visual != null) visual.localScale = new Vector3(direction, 1f, 1f);
    }

    bool IsGrounded()
    {
        Bounds b = body.bounds;
        return Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.15f, groundLayer);
    }

    bool ShouldTurn()
    {
        float x = transform.position.x;
        if ((direction > 0f && x >= startX + patrolDistance) || (direction < 0f && x <= startX - patrolDistance))
            return true;

        Bounds b = body.bounds;
        float frontX = direction > 0f ? b.max.x : b.min.x;
        bool wallAhead = Physics2D.Raycast(new Vector2(frontX, b.center.y), Vector2.right * direction, 0.1f, groundLayer);
        bool groundAhead = Physics2D.Raycast(new Vector2(frontX + direction * 0.1f, b.min.y + 0.05f), Vector2.down, 0.3f, groundLayer);
        return wallAhead || !groundAhead;
    }

    void DealContactDamage()
    {
        if (contactDamage <= 0) return;
        Bounds b = body.bounds;
        int count = Physics2D.OverlapBox(b.center, b.size, 0f, playerFilter, contacts);
        for (int i = 0; i < count; i++)
        {
            var target = contacts[i].GetComponentInParent<Health>();
            if (target != null) target.TakeDamage(contactDamage, transform.position);
        }
    }

    void OnDamaged(int amount, Vector2 source)
    {
        float away = Mathf.Sign(transform.position.x - source.x);
        rb.linearVelocity = new Vector2(away * knockback.x, knockback.y);
        stunnedUntil = Time.time + stunTime;
    }

    void OnDied()
    {
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (TryGetComponent(out HitFlash flash)) flash.enabled = false;
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        var renderers = (visual != null ? visual : transform).GetComponentsInChildren<SpriteRenderer>();
        var startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) startColors[i] = renderers[i].color;
        for (float t = 0f; t < deathFadeTime; t += Time.deltaTime)
        {
            float a = 1f - t / deathFadeTime;
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].color = new Color(startColors[i].r, startColors[i].g, startColors[i].b, startColors[i].a * a);
            yield return null;
        }
        gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        float x = Application.isPlaying ? startX : transform.position.x;
        float y = transform.position.y + 0.4f;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(new Vector3(x - patrolDistance, y), new Vector3(x + patrolDistance, y));
    }
}
