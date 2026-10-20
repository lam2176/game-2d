using System.Collections;
using UnityEngine;

/// <summary>
/// Phần chung của mọi quái: va chạm với người chơi (đạp đầu / sát thương tiếp xúc), bị đánh (đẩy lùi, choáng),
/// chết (mờ dần rồi tắt). Lớp con chỉ viết AI trong <see cref="Think"/> — chỉ chạy khi không choáng.
///
/// Va chạm với người chơi xử lý ở MỘT chỗ duy nhất (ResolvePlayerContacts, mỗi bước vật lý):
/// người chơi đang rơi và bàn chân ở trên đỉnh đầu quái từ bước trước → ĐẠP ĐẦU (quái mất máu, người chơi nảy lên);
/// quái có gai trên lưng (spiky) thì đạp vào là người chơi bị thương (như Spiny của Mario).
/// Còn lại (ngang/dưới) → sát thương tiếp xúc. Sau mỗi lần đạp có stompGrace để collider còn chồng lấn
/// không gây thêm đạp/sát thương. Health người chơi có bất tử ngắn nên không bị trừ máu mỗi frame.
/// Collider thân không va chạm vật lý với layer Player (Exclude Layers) để hai bên không đẩy nhau.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Chung")]
    public LayerMask groundLayer;
    public LayerMask playerLayer;
    [Tooltip("Object con chứa hình, được lật theo hướng nhìn.")]
    public Transform visual;

    [Header("Va chạm với người chơi")]
    [Tooltip("Sát thương khi chạm ngang/dưới. 0 = tắt.")]
    [Min(0)] public int contactDamage = 1;
    [Tooltip("Đạp lên đầu được không.")]
    public bool stompable = true;
    [Tooltip("Lưng có gai: đạp lên thì người chơi bị thương và bật lên.")]
    public bool spiky;
    [Min(0)] public int stompDamage = 1;
    [Tooltip("Vận tốc bật lên của người chơi khi đạp đầu.")]
    [Min(0f)] public float stompBounce = 12f;
    [Tooltip("Bàn chân người chơi ở bước trước được phép thấp hơn đỉnh đầu quái tối đa bao nhiêu (unit).")]
    [Min(0f)] public float stompTolerance = 0.1f;
    [Tooltip("Sau khi bị đạp, bỏ qua mọi va chạm với người chơi trong khoảng này (giây).")]
    [Min(0f)] public float stompGrace = 0.25f;

    [Header("Bị đánh")]
    public Vector2 knockback = new Vector2(4f, 4f);
    [Tooltip("Hệ số nhận đẩy lùi (0 = đứng vững như boss).")]
    [Min(0f)] public float knockbackTaken = 1f;
    [Min(0f)] public float stunTime = 0.3f;
    [Tooltip("Nhân thời gian choáng do chiêu (boss < 1).")]
    [Min(0f)] public float stunMultiplier = 1f;
    [Min(0f)] public float deathFadeTime = 0.4f;

    protected Rigidbody2D rb;
    protected Collider2D body;
    protected Health health;
    protected ZodiacPlayerController player;
    protected Health playerHealth;
    protected ContactFilter2D playerFilter;
    protected readonly Collider2D[] contacts = new Collider2D[2];
    protected float direction = 1f;
    float stunnedUntil = float.NegativeInfinity;
    float stompGraceUntil = float.NegativeInfinity;
    Transform stunIcon;

    public bool IsStunned => Time.time < stunnedUntil;
    public Health Health => health;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        health = GetComponent<Health>();
        playerFilter = new ContactFilter2D { useTriggers = false };
        playerFilter.SetLayerMask(playerLayer);
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    protected virtual void Start()
    {
        player = ZodiacPlayerController.Current != null ? ZodiacPlayerController.Current : FindAnyObjectByType<ZodiacPlayerController>();
        if (player != null) playerHealth = player.GetComponent<Health>();
    }

    void FixedUpdate()
    {
        if (health.IsDead) return;
        ResolvePlayerContacts();
        UpdateStunIcon();
        if (IsStunned) return;
        Think();
        UpdateVisual();
    }

    /// <summary>AI mỗi bước vật lý (không gọi khi đang choáng hoặc đã chết).</summary>
    protected abstract void Think();

    /// <summary>Huỷ đòn đang ra (bị đánh, bị choáng).</summary>
    protected virtual void Interrupt() { }

    /// <summary>Choáng thêm (chiêu sóng âm, sóng chấn).</summary>
    public void Stun(float seconds)
    {
        seconds *= stunMultiplier;
        if (seconds < 0.15f) return;   // choáng quá ngắn (đạp đầu boss) thì bỏ qua — không ngắt được đòn
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds);
        Interrupt();
    }

    protected bool CanSeePlayer(float rangeX, float rangeY)
    {
        if (player == null || !player.ControlEnabled || player.IsHidden || (playerHealth != null && playerHealth.IsDead))
            return false;
        Vector2 d = player.transform.position - transform.position;
        return Mathf.Abs(d.x) <= rangeX && Mathf.Abs(d.y) <= rangeY;
    }

    protected Vector2 PlayerCenter => player != null ? (Vector2)player.BodyCollider.bounds.center : Vector2.zero;

    // ───────── Va chạm với người chơi ─────────

    void ResolvePlayerContacts()
    {
        if (Time.time < stompGraceUntil) return;
        Bounds b = body.bounds;
        int count = Physics2D.OverlapBox(b.center, b.size, 0f, playerFilter, contacts);
        for (int i = 0; i < count; i++)
        {
            var target = ZodiacPlayerController.FromCollider(contacts[i]);
            if (target == null || target.IsHidden) continue;

            if (stompable && IsStomp(contacts[i], b))
            {
                Stomp(target);
                return;   // cùng lần va chạm này không gây sát thương tiếp xúc
            }
            if (contactDamage > 0 && target.TryGetComponent(out Health h))
                h.TakeDamage(contactDamage, transform.position);
        }
    }

    /// <summary>
    /// Đạp đầu khi: người chơi không bay lên, và bàn chân ở bước vật lý trước còn ở trên đỉnh đầu quái
    /// (trừ stompTolerance). Dùng vị trí bước trước vì khi rơi nhanh chân có thể đã lún vào trong thân quái.
    /// </summary>
    bool IsStomp(Collider2D playerCollider, Bounds enemyBounds)
    {
        Rigidbody2D playerBody = playerCollider.attachedRigidbody;
        float vy = playerBody.linearVelocity.y - Mathf.Max(0f, rb.linearVelocity.y);   // quái bay lên đâm vào chân không tính là đạp
        if (vy > 0.01f) return false;
        float feet = playerCollider.bounds.min.y;
        float previousFeet = feet - vy * Time.fixedDeltaTime;
        return previousFeet >= enemyBounds.max.y - stompTolerance;
    }

    void Stomp(ZodiacPlayerController target)
    {
        stompGraceUntil = Time.time + stompGrace;
        if (spiky)
        {
            // Gai trên lưng: người chơi bị đâm nhưng vẫn nảy lên (không bị kẹt trên lưng quái).
            if (target.TryGetComponent(out Health h)) h.TakeDamage(new Hit(contactDamage, transform.position, 0f));
            target.Bounce(stompBounce * 0.8f);
            return;
        }
        int multiplier = target.Character != null ? target.Character.stompDamageMultiplier : 1;
        health.TakeDamage(new Hit(stompDamage * multiplier, target.transform.position, 0f, 0.4f));
        target.Bounce(stompBounce);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.stomp, 0.9f, 0.08f, transform.position);
        GameFeel.HitStop(0.04f);
        GameFeel.Shake(0.12f);
        ZodiacParticles.Dust(new Vector2(body.bounds.center.x, body.bounds.max.y), Vector2.zero, 6);
        OnStomped();
    }

    /// <summary>Bị đạp mà chưa chết.</summary>
    protected virtual void OnStomped() { }

    // ───────── Máu ─────────

    protected virtual void OnDamaged(Hit hit)
    {
        float k = hit.knockback * knockbackTaken;
        if (k > 0f)
        {
            float away = Mathf.Sign(transform.position.x - hit.source.x);
            if (rb.bodyType == RigidbodyType2D.Dynamic)
                rb.linearVelocity = new Vector2(away * knockback.x * k, knockback.y * Mathf.Min(k, 1.5f));
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + stunTime);
            Interrupt();
        }
        if (hit.stun > 0f) Stun(hit.stun);
    }

    /// <summary>Màu mảnh vỡ khi chết (lấy từ hình đầu tiên của quái).</summary>
    protected Color DebrisColor
    {
        get
        {
            var sr = (visual != null ? visual : transform).GetComponentInChildren<SpriteRenderer>();
            return sr != null ? sr.color : Color.gray;
        }
    }

    protected virtual void OnDied()
    {
        ZodiacParticles.Debris(body.bounds.center, DebrisColor, 14, body.bounds.extents.x);
        ZodiacParticles.Sparks(body.bounds.center, Color.white, 10);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.enemyDeath, 0.9f, 0.1f, transform.position);
        GameFeel.Shake(0.15f);
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (visual != null) visual.localScale = new Vector3(direction, 1f, 1f);
        if (TryGetComponent(out HitFlash flash)) flash.enabled = false;
        if (stunIcon != null) stunIcon.gameObject.SetActive(false);
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

    // ───────── Hiển thị ─────────

    protected virtual void UpdateVisual()
    {
        if (visual != null) visual.localScale = new Vector3(direction, 1f, 1f);
    }

    /// <summary>Ba ngôi sao vàng quay trên đầu khi bị choáng do chiêu.</summary>
    void UpdateStunIcon()
    {
        bool show = stunnedUntil - Time.time > 0.45f;   // choáng ngắn khi trúng đòn thường thì không hiện
        if (show && stunIcon == null)
        {
            stunIcon = new GameObject("StunIcon").transform;
            stunIcon.SetParent(transform, false);
            for (int i = 0; i < 3; i++)
            {
                var star = ZodiacFx.Shape("Star", true, Vector2.zero, Vector2.one * 0.18f, new Color(1f, 0.9f, 0.2f), 22, stunIcon);
                float a = i * Mathf.PI * 2f / 3f;
                star.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.35f, Mathf.Sin(a) * 0.12f, 0f);
            }
        }
        if (stunIcon == null) return;
        stunIcon.gameObject.SetActive(show);
        if (show)
        {
            stunIcon.position = new Vector3(body.bounds.center.x, body.bounds.max.y + 0.25f, 0f);
            for (int i = 0; i < stunIcon.childCount; i++)
            {
                float a = Time.time * 6f + i * Mathf.PI * 2f / 3f;
                stunIcon.GetChild(i).localPosition = new Vector3(Mathf.Cos(a) * 0.35f, Mathf.Sin(a) * 0.12f, 0f);
            }
        }
    }

    // ───────── Tiện ích di chuyển ─────────

    protected void SetHorizontalVelocity(float vx) => rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

    protected bool IsGrounded()
    {
        Bounds b = body.bounds;
        return Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.15f, groundLayer);
    }

    /// <summary>Có tường ngay phía trước hoặc phía trước là mép vực.</summary>
    protected bool BlockedAhead(float dir)
    {
        Bounds b = body.bounds;
        float frontX = dir > 0f ? b.max.x : b.min.x;
        bool wallAhead = Physics2D.Raycast(new Vector2(frontX, b.center.y), Vector2.right * dir, 0.1f, groundLayer);
        bool groundAhead = Physics2D.Raycast(new Vector2(frontX + dir * 0.1f, b.min.y + 0.05f), Vector2.down, 0.3f, groundLayer);
        return wallAhead || !groundAhead;
    }
}
