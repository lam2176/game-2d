using System.Collections;
using UnityEngine;

/// <summary>
/// Quái đơn giản (hình placeholder, chưa có sprite/animation):
/// - Tuần tra qua lại quanh điểm xuất phát, quay đầu khi gặp tường, mép vực hoặc hết quãng tuần tra.
/// - Thấy người chơi trong detectRange (và chênh độ cao ≤ verticalRange) thì đuổi theo, không lao xuống vực.
/// - Đủ gần thì dừng lại gồng đòn (windupTime, thân bẹp xuống để báo hiệu) → lao tới cắn (attackActiveTime)
///   → hồi đòn attackCooldown.
/// - Va chạm với người chơi xử lý ở MỘT chỗ duy nhất (ResolvePlayerContacts, mỗi bước vật lý):
///   người chơi đang rơi và bàn chân ở trên đỉnh đầu quái từ bước trước → ĐẠP ĐẦU (quái mất máu, người chơi nảy lên);
///   còn lại (ngang/dưới) → sát thương tiếp xúc. Sau mỗi lần đạp có stompGrace để collider còn chồng lấn
///   không gây thêm đạp/sát thương. Health người chơi có bất tử ngắn nên không bị trừ máu mỗi frame.
/// Collider thân không va chạm vật lý với layer Player (Exclude Layers) để hai bên không đẩy nhau.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
public class EnemyPatrol : MonoBehaviour
{
    enum State { Patrol, Chase, Windup, Attack }

    [Header("Tuần tra")]
    [Min(0f)] public float moveSpeed = 2f;
    [Tooltip("Đi tối đa bao xa mỗi phía tính từ điểm xuất phát.")]
    [Min(0f)] public float patrolDistance = 3f;
    public LayerMask groundLayer;
    [Tooltip("Object con được lật theo hướng đi.")]
    public Transform visual;

    [Header("Phát hiện & đuổi")]
    [Tooltip("Thấy người chơi khi khoảng cách ngang ≤ giá trị này.")]
    [Min(0f)] public float detectRange = 5f;
    [Tooltip("Bỏ đuổi khi khoảng cách ngang > giá trị này.")]
    [Min(0f)] public float loseRange = 7f;
    [Tooltip("Chỉ để ý người chơi khi chênh độ cao ≤ giá trị này.")]
    [Min(0f)] public float verticalRange = 1.5f;
    [Min(0f)] public float chaseSpeed = 3f;

    [Header("Tấn công")]
    [Tooltip("Dừng lại và gồng đòn khi khoảng cách ngang (tâm-tâm) ≤ giá trị này.")]
    [Min(0f)] public float attackRange = 1.3f;
    [Tooltip("Thời gian gồng đòn trước khi lao tới (giây) — người chơi có thời gian né.")]
    [Min(0f)] public float windupTime = 0.45f;
    [Min(0.02f)] public float attackActiveTime = 0.15f;
    public float lungeSpeed = 6f;
    [Min(0)] public int attackDamage = 1;
    [Tooltip("Từ lúc kết thúc đòn tới khi được gồng đòn tiếp (giây).")]
    [Min(0f)] public float attackCooldown = 1.2f;
    [Tooltip("Vùng cắn phía trước (tính theo hướng nhìn, gốc ở chân quái).")]
    public Vector2 attackBoxOffset = new Vector2(0.75f, 0.4f);
    public Vector2 attackBoxSize = new Vector2(0.7f, 0.6f);

    [Header("Va chạm với người chơi")]
    [Tooltip("Sát thương khi chạm ngang/dưới. 0 = tắt.")]
    [Min(0)] public int contactDamage = 1;
    public LayerMask playerLayer;
    [Min(0)] public int stompDamage = 1;
    [Tooltip("Vận tốc bật lên của người chơi khi đạp đầu.")]
    [Min(0f)] public float stompBounce = 12f;
    [Tooltip("Bàn chân người chơi ở bước trước được phép thấp hơn đỉnh đầu quái tối đa bao nhiêu (unit).")]
    [Min(0f)] public float stompTolerance = 0.1f;
    [Tooltip("Sau khi bị đạp, bỏ qua mọi va chạm với người chơi trong khoảng này (giây).")]
    [Min(0f)] public float stompGrace = 0.25f;

    [Header("Bị đánh")]
    public Vector2 knockback = new Vector2(4f, 4f);
    [Min(0f)] public float stunTime = 0.3f;
    [Min(0f)] public float deathFadeTime = 0.4f;

    Rigidbody2D rb;
    Collider2D body;
    Health health;
    ZodiacPlayerController player;
    Health playerHealth;
    ContactFilter2D playerFilter;
    readonly Collider2D[] contacts = new Collider2D[2];
    float startX;
    float direction = 1f;
    float stunnedUntil = float.NegativeInfinity;
    float stompGraceUntil = float.NegativeInfinity;
    State state;
    float stateEndTime, nextAttackTime;
    bool attackLanded;

    /// <summary>Trạng thái AI hiện tại (để kiểm tra/debug).</summary>
    public string CurrentState => state.ToString();

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

    void Start()
    {
        player = FindAnyObjectByType<ZodiacPlayerController>();
        if (player != null) playerHealth = player.GetComponent<Health>();
    }

    void FixedUpdate()
    {
        if (health.IsDead) return;
        ResolvePlayerContacts();
        if (Time.time < stunnedUntil || !IsGrounded()) return;

        switch (state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase: UpdateChase(); break;
            case State.Windup: UpdateWindup(); break;
            case State.Attack: UpdateAttack(); break;
        }
        UpdateVisual();
    }

    // ───────── AI ─────────

    void UpdatePatrol()
    {
        if (CanSeePlayer(detectRange)) { state = State.Chase; return; }

        float x = transform.position.x;
        bool pastLimit = (direction > 0f && x >= startX + patrolDistance) || (direction < 0f && x <= startX - patrolDistance);
        if (pastLimit || BlockedAhead(direction)) direction = -direction;
        SetHorizontalVelocity(direction * moveSpeed);
    }

    void UpdateChase()
    {
        if (!CanSeePlayer(loseRange)) { state = State.Patrol; return; }

        float dx = player.transform.position.x - transform.position.x;
        direction = dx >= 0f ? 1f : -1f;
        if (Mathf.Abs(dx) <= attackRange)
        {
            SetHorizontalVelocity(0f);   // đứng chờ hồi đòn
            if (Time.time >= nextAttackTime)
            {
                state = State.Windup;
                stateEndTime = Time.time + windupTime;
            }
        }
        else
        {
            SetHorizontalVelocity(BlockedAhead(direction) ? 0f : direction * chaseSpeed);
        }
    }

    void UpdateWindup()
    {
        SetHorizontalVelocity(0f);
        if (Time.time < stateEndTime) return;
        state = State.Attack;
        stateEndTime = Time.time + attackActiveTime;
        attackLanded = false;
    }

    void UpdateAttack()
    {
        SetHorizontalVelocity(BlockedAhead(direction) ? 0f : direction * lungeSpeed);
        if (!attackLanded && attackDamage > 0)
        {
            Vector2 center = (Vector2)transform.position + new Vector2(attackBoxOffset.x * direction, attackBoxOffset.y);
            int count = Physics2D.OverlapBox(center, attackBoxSize, 0f, playerFilter, contacts);
            for (int i = 0; i < count && !attackLanded; i++)
            {
                var target = contacts[i].GetComponentInParent<Health>();
                if (target != null)
                {
                    target.TakeDamage(attackDamage, transform.position);
                    attackLanded = true;   // mỗi đòn trúng tối đa 1 lần
                }
            }
        }
        if (Time.time >= stateEndTime) EndAttack();
    }

    void EndAttack()
    {
        state = State.Chase;
        nextAttackTime = Time.time + attackCooldown;
    }

    bool CanSeePlayer(float range)
    {
        if (player == null || !player.ControlEnabled || (playerHealth != null && playerHealth.IsDead)) return false;
        Vector2 d = player.transform.position - transform.position;
        return Mathf.Abs(d.x) <= range && Mathf.Abs(d.y) <= verticalRange;
    }

    // ───────── Va chạm với người chơi ─────────

    void ResolvePlayerContacts()
    {
        if (Time.time < stompGraceUntil) return;
        Bounds b = body.bounds;
        int count = Physics2D.OverlapBox(b.center, b.size, 0f, playerFilter, contacts);
        for (int i = 0; i < count; i++)
        {
            var target = ZodiacPlayerController.FromCollider(contacts[i]);
            if (target == null) continue;

            if (IsStomp(contacts[i], b))
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
        float vy = playerBody.linearVelocity.y;
        if (vy > 0.01f) return false;
        float feet = playerCollider.bounds.min.y;
        float previousFeet = feet - vy * Time.fixedDeltaTime;
        return previousFeet >= enemyBounds.max.y - stompTolerance;
    }

    void Stomp(ZodiacPlayerController target)
    {
        stompGraceUntil = Time.time + stompGrace;
        health.TakeDamage(stompDamage, target.transform.position);
        if (!health.IsDead)
        {
            rb.linearVelocity = Vector2.zero;   // bị đạp: bẹp tại chỗ, không văng ngang
            state = State.Chase;
            nextAttackTime = Mathf.Max(nextAttackTime, Time.time + attackCooldown);
        }
        target.Bounce(stompBounce);
    }

    // ───────── Di chuyển / hiển thị ─────────

    void SetHorizontalVelocity(float vx) => rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

    bool IsGrounded()
    {
        Bounds b = body.bounds;
        return Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.15f, groundLayer);
    }

    /// <summary>Có tường ngay phía trước hoặc phía trước là mép vực.</summary>
    bool BlockedAhead(float dir)
    {
        Bounds b = body.bounds;
        float frontX = dir > 0f ? b.max.x : b.min.x;
        bool wallAhead = Physics2D.Raycast(new Vector2(frontX, b.center.y), Vector2.right * dir, 0.1f, groundLayer);
        bool groundAhead = Physics2D.Raycast(new Vector2(frontX + dir * 0.1f, b.min.y + 0.05f), Vector2.down, 0.3f, groundLayer);
        return wallAhead || !groundAhead;
    }

    void UpdateVisual()
    {
        if (visual == null) return;
        // Gồng đòn: thân bẹp xuống, phình ngang — báo hiệu sắp tấn công (thay cho animation chưa có).
        visual.localScale = state == State.Windup
            ? new Vector3(direction * 1.15f, 0.85f, 1f)
            : new Vector3(direction, 1f, 1f);
    }

    // ───────── Máu ─────────

    void OnDamaged(int amount, Vector2 source)
    {
        float away = Mathf.Sign(transform.position.x - source.x);
        rb.linearVelocity = new Vector2(away * knockback.x, knockback.y);
        stunnedUntil = Time.time + stunTime;
        if (state == State.Windup || state == State.Attack) EndAttack();   // bị đánh thì huỷ đòn đang ra
    }

    void OnDied()
    {
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (visual != null) visual.localScale = new Vector3(direction, 1f, 1f);
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

        Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.4f, new Vector3(detectRange * 2f, verticalRange * 2f, 0f));

        float dir = Application.isPlaying ? direction : 1f;
        Gizmos.color = Application.isPlaying && state == State.Attack ? Color.red : new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireCube(transform.position + new Vector3(attackBoxOffset.x * dir, attackBoxOffset.y), attackBoxSize);
    }
}
