using UnityEngine;

/// <summary>
/// Quái đi bộ (Lính Đất Nung — kiểu Husk của Hollow Knight / Goomba có răng):
/// - Tuần tra qua lại quanh điểm xuất phát, quay đầu khi gặp tường, mép vực hoặc hết quãng tuần tra.
/// - Thấy người chơi trong detectRange (và chênh độ cao ≤ verticalRange) thì đuổi theo, không lao xuống vực.
/// - Đủ gần thì dừng lại gồng đòn (windupTime, thân bẹp xuống để báo hiệu) → lao tới cắn (attackActiveTime)
///   → hồi đòn attackCooldown.
/// Bỏ chase (chases = false) + bật spiky ở EnemyBase là ra Bọ Gai: chỉ tuần tra, không đạp được.
/// </summary>
public class EnemyPatrol : EnemyBase
{
    enum State { Patrol, Chase, Windup, Attack }

    [Header("Tuần tra")]
    [Min(0f)] public float moveSpeed = 2f;
    [Tooltip("Đi tối đa bao xa mỗi phía tính từ điểm xuất phát.")]
    [Min(0f)] public float patrolDistance = 3f;

    [Header("Phát hiện & đuổi")]
    [Tooltip("Tắt = chỉ tuần tra (Bọ Gai).")]
    public bool chases = true;
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

    float startX;
    State state;
    float stateEndTime, nextAttackTime;
    bool attackLanded;

    /// <summary>Trạng thái AI hiện tại (để kiểm tra/debug).</summary>
    public string CurrentState => state.ToString();

    protected override void Awake()
    {
        base.Awake();
        startX = transform.position.x;
    }

    protected override void Think()
    {
        if (!IsGrounded()) return;
        switch (state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase: UpdateChase(); break;
            case State.Windup: UpdateWindup(); break;
            case State.Attack: UpdateAttack(); break;
        }
    }

    void UpdatePatrol()
    {
        if (chases && CanSeePlayer(detectRange, verticalRange)) { state = State.Chase; return; }

        float x = transform.position.x;
        bool pastLimit = (direction > 0f && x >= startX + patrolDistance) || (direction < 0f && x <= startX - patrolDistance);
        if (pastLimit || BlockedAhead(direction)) direction = -direction;
        SetHorizontalVelocity(direction * moveSpeed);
    }

    void UpdateChase()
    {
        if (!CanSeePlayer(loseRange, verticalRange)) { state = State.Patrol; return; }

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

    protected override void Interrupt()
    {
        if (state == State.Windup || state == State.Attack) EndAttack();   // bị đánh thì huỷ đòn đang ra
    }

    protected override void OnStomped()
    {
        rb.linearVelocity = Vector2.zero;   // bị đạp: bẹp tại chỗ, không văng ngang
        state = chases ? State.Chase : State.Patrol;
        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + attackCooldown);
    }

    protected override void UpdateVisual()
    {
        if (visual == null) return;
        // Gồng đòn: thân bẹp xuống, phình ngang — báo hiệu sắp tấn công (thay cho animation chưa có).
        visual.localScale = state == State.Windup
            ? new Vector3(direction * 1.15f, 0.85f, 1f)
            : new Vector3(direction, 1f, 1f);
    }

    void OnDrawGizmosSelected()
    {
        float x = Application.isPlaying ? startX : transform.position.x;
        float y = transform.position.y + 0.4f;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(new Vector3(x - patrolDistance, y), new Vector3(x + patrolDistance, y));
        if (!chases) return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.4f, new Vector3(detectRange * 2f, verticalRange * 2f, 0f));

        float dir = Application.isPlaying ? direction : 1f;
        Gizmos.color = Application.isPlaying && state == State.Attack ? Color.red : new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireCube(transform.position + new Vector3(attackBoxOffset.x * dir, attackBoxOffset.y), attackBoxSize);
    }
}
