using UnityEngine;

/// <summary>
/// Quái cận chiến (Lính Đất Nung, Lính Khiên) — lính thường của beat 'em up:
/// - Thấy người chơi trong detectRange thì xáp lại. Có lượt tấn công thì tới đứng ngang hàng người chơi (cùng làn,
///   cách attackRange), không có lượt thì đứng vòng ngoài, giữ làn riêng, chờ.
/// - Vào vị trí thì gồng đòn (windupTime, thân bẹp xuống báo hiệu) → lao tới đánh (attackActiveTime) → nghỉ (recoverTime).
///   Né bằng cách đổi làn trong lúc nó gồng, lăn, hoặc đánh ngắt trước.
/// - Đánh xong trả lượt để quái khác vào.
/// - Mất đồ (giáo văng, khiên vỡ): thành lính tay không — đánh yếu và ngắn hơn nhưng nhanh nhẹn hơn.
/// </summary>
public class EnemyBrawler : EnemyBase
{
    enum State { Idle, Approach, Windup, Attack, Recover }

    [Header("Di chuyển")]
    [Min(0f)] public float moveSpeed = 2.6f;
    [Tooltip("Thấy người chơi khi khoảng cách ≤ giá trị này.")]
    [Min(0f)] public float detectRange = 9f;
    [Tooltip("Khoảng đứng chờ lượt quanh người chơi.")]
    [Min(0f)] public float waitDistance = 3.6f;

    [Header("Tấn công")]
    [Tooltip("Đứng cách người chơi bao xa (theo X) để ra đòn.")]
    [Min(0f)] public float attackRange = 1.2f;
    [Tooltip("Lệch làn (Z) tối đa vẫn ra đòn.")]
    [Min(0f)] public float laneTolerance = 0.4f;
    [Tooltip("Thời gian gồng đòn trước khi lao tới (giây) — người chơi có thời gian né.")]
    [Min(0f)] public float windupTime = 0.45f;
    [Min(0.02f)] public float attackActiveTime = 0.15f;
    public float lungeSpeed = 5f;
    [Min(0)] public int attackDamage = ZodiacBalance.Unit;
    [Tooltip("Đòn này quật ngã người chơi.")]
    public bool attackKnockdown;
    [Tooltip("Đứng nghỉ sau đòn (giây).")]
    [Min(0f)] public float recoverTime = 0.5f;
    [Tooltip("Từ lúc kết thúc đòn tới khi được gồng đòn tiếp (giây).")]
    [Min(0f)] public float attackCooldown = 1.2f;
    [Tooltip("Hộp đòn đánh (tâm tính theo hướng nhìn, gốc ở chân quái).")]
    public Vector3 attackBoxOffset = new Vector3(0.75f, 0.7f, 0f);
    public Vector3 attackBoxSize = new Vector3(0.9f, 0.9f, 0.9f);

    State state;
    float stateEndTime, nextAttackTime;
    bool attackLanded;

    /// <summary>Trạng thái AI hiện tại (để kiểm tra/debug).</summary>
    public string CurrentState => state.ToString();

    protected override void Start()
    {
        base.Start();
        nextAttackTime = Time.time + Random.Range(0.3f, 1f);   // vào trận không đánh cùng lúc
    }

    protected override void Think()
    {
        switch (state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Approach: UpdateApproach(); break;
            case State.Windup: UpdateWindup(); break;
            case State.Attack: UpdateAttack(); break;
            case State.Recover: UpdateRecover(); break;
        }
    }

    void UpdateIdle()
    {
        SetPlanarVelocity(Vector3.zero);
        if (Detects(detectRange)) { state = State.Approach; Alert(); }
    }

    void UpdateApproach()
    {
        if (!PlayerAvailable) { ReleaseToken(); state = State.Idle; return; }
        FacePlayer();
        if (Time.time >= nextAttackTime && TryTakeToken())
        {
            MoveTo(EngagePoint(attackRange * 0.9f), moveSpeed);
            Vector3 d = ToPlayer;
            if (Mathf.Abs(d.x) <= attackRange + 0.15f && Mathf.Abs(d.z) <= laneTolerance)
            {
                SetPlanarVelocity(Vector3.zero);
                state = State.Windup;
                stateEndTime = Time.time + windupTime;
            }
        }
        else
        {
            MoveTo(WaitPoint(waitDistance), moveSpeed * 0.6f, 0.4f);
            FacePlayer();
        }
    }

    void UpdateWindup()
    {
        SetPlanarVelocity(Vector3.zero);
        if (Time.time < stateEndTime) return;
        state = State.Attack;
        stateEndTime = Time.time + attackActiveTime;
        attackLanded = false;
    }

    void UpdateAttack()
    {
        Vector3 forward = Vector3.right * direction;
        // Lao tới nhưng dừng trước mặt người chơi (không xuyên qua đứng đè lên)
        bool closeEnough = player != null && Mathf.Abs(ToPlayer.x) <= 0.85f;
        SetPlanarVelocity(BlockedAhead(forward) || closeEnough ? Vector3.zero : forward * lungeSpeed);
        if (!attackLanded && attackDamage > 0)
        {
            Vector3 center = transform.position + new Vector3(attackBoxOffset.x * direction, attackBoxOffset.y, attackBoxOffset.z);
            var hit = new Hit(attackDamage, transform.position) { knockdown = attackKnockdown };
            if (StrikeBox(center, attackBoxSize * 0.5f, hit)) attackLanded = true;   // mỗi đòn trúng tối đa 1 lần
        }
        if (Time.time >= stateEndTime) EndAttack();
    }

    void UpdateRecover()
    {
        SetPlanarVelocity(Vector3.zero);
        if (Time.time >= stateEndTime) state = State.Approach;
    }

    void EndAttack()
    {
        state = State.Recover;
        stateEndTime = Time.time + recoverTime;
        nextAttackTime = Time.time + attackCooldown;
        ReleaseToken();
    }

    protected override void OnGearLost()
    {
        attackDamage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * 0.6f));
        attackKnockdown = false;
        attackRange *= 0.8f;
        attackBoxOffset.x *= 0.8f;
        attackBoxSize *= 0.85f;
        windupTime *= 0.85f;
        moveSpeed *= 1.15f;
    }

    protected override void Interrupt()
    {
        if (state == State.Windup || state == State.Attack) EndAttack();   // bị đánh thì huỷ đòn đang ra
    }

    protected override void UpdateVisual()
    {
        if (visual == null) return;
        // Gồng đòn: thân bẹp xuống, phình ngang — báo hiệu sắp tấn công (thay cho animation chưa có).
        SetVisualScale(state == State.Windup ? new Vector3(1.15f, 0.85f, 1f) : Vector3.one);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, detectRange);
        float dir = Application.isPlaying ? direction : 1f;
        Gizmos.color = Application.isPlaying && state == State.Attack ? Color.red : new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireCube(transform.position + new Vector3(attackBoxOffset.x * dir, attackBoxOffset.y, attackBoxOffset.z), attackBoxSize);
    }
}
