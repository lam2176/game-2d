using UnityEngine;

/// <summary>
/// Bọ Gai — cuộn tròn lăn thẳng theo một làn (kiểu bowling): bò về làn của người chơi, rung người báo hiệu,
/// rồi lăn vút dọc trục X tới khi đụng tường / hết đà. Ai đứng trên đường lăn bị đâm (quật ngã).
/// Né: đổi làn (↑ ↓) khi thấy nó rung, hoặc nhảy qua. Lăn xong bị choáng váng một lúc — cơ hội đánh trả.
/// </summary>
public class EnemyRoller : EnemyBase
{
    enum State { Idle, Align, Windup, Roll, Dizzy }

    [Min(0f)] public float moveSpeed = 2f;
    [Min(0f)] public float detectRange = 11f;
    [Tooltip("Lệch làn (Z) tối đa để bắt đầu gồng lăn.")]
    [Min(0f)] public float laneTolerance = 0.35f;
    [Min(0f)] public float windupTime = 0.6f;
    [Min(0f)] public float rollSpeed = 10f;
    [Min(0.1f)] public float rollTime = 1.4f;
    [Min(0f)] public float dizzyTime = 1.1f;
    [Min(0f)] public float cooldown = 1.5f;
    [Min(0)] public int rollDamage = ZodiacBalance.Unit;

    State state;
    float stateEnd, nextRoll;
    bool landed;

    public string CurrentState => state.ToString();

    protected override void Start()
    {
        base.Start();
        nextRoll = Time.time + Random.Range(0.5f, 1.5f);
    }

    protected override void Think()
    {
        switch (state)
        {
            case State.Idle:
                SetPlanarVelocity(Vector3.zero);
                if (Detects(detectRange)) { state = State.Align; Alert(); }
                break;
            case State.Align:
            {
                if (!PlayerAvailable) { ReleaseToken(); state = State.Idle; break; }
                FacePlayer();
                Vector3 d = ToPlayer;
                // Giữ khoảng cách vừa đủ để có đà lăn, chỉ canh cho đúng làn
                float keepX = Mathf.Abs(d.x) < 4f ? -Mathf.Sign(d.x) * moveSpeed : 0f;
                float vz = Mathf.Abs(d.z) > 0.05f ? Mathf.Sign(d.z) * moveSpeed * 0.8f : 0f;
                SetPlanarVelocity(new Vector3(keepX, 0f, vz));
                if (Time.time >= nextRoll && Mathf.Abs(d.z) <= laneTolerance && TryTakeToken())
                {
                    SetPlanarVelocity(Vector3.zero);
                    state = State.Windup;
                    stateEnd = Time.time + windupTime;
                }
                break;
            }
            case State.Windup:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd)
                {
                    state = State.Roll;
                    stateEnd = Time.time + rollTime;
                    landed = false;
                    var res = ZodiacResources.Get;
                    if (res != null) ZodiacAudio.Play(res.swingHeavy, 0.7f, 0.1f, transform.position);
                }
                break;
            case State.Roll:
            {
                Vector3 forward = Vector3.right * direction;
                SetPlanarVelocity(forward * rollSpeed);
                if (!landed)
                {
                    Bounds b = body.bounds;
                    var hit = new Hit(rollDamage, transform.position - forward, 1.2f) { knockdown = true };
                    landed = StrikeBox(b.center, b.extents + new Vector3(0.1f, 0f, 0.05f), hit);
                }
                if (Time.time >= stateEnd || BlockedAhead(forward, 0.2f)) StartDizzy();
                break;
            }
            case State.Dizzy:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd) state = State.Align;
                break;
        }
    }

    void StartDizzy()
    {
        state = State.Dizzy;
        stateEnd = Time.time + dizzyTime;
        nextRoll = Time.time + dizzyTime + cooldown;
        ReleaseToken();
        ZodiacParticles.Dust(transform.position + Vector3.up * 0.1f, -Vector3.right * direction, 6);
    }

    protected override void Interrupt()
    {
        if (state == State.Windup) { state = State.Align; nextRoll = Time.time + cooldown * 0.5f; ReleaseToken(); }
    }

    protected override void UpdateVisual()
    {
        if (visual == null) return;
        Vector3 s = state switch
        {
            State.Windup => new Vector3(1.1f + Mathf.Sin(Time.time * 40f) * 0.06f, 0.88f, 1f),
            State.Roll => new Vector3(0.9f, 0.9f, 1f),
            State.Dizzy => new Vector3(1f, 0.95f + Mathf.Sin(Time.time * 8f) * 0.05f, 1f),
            _ => Vector3.one,
        };
        SetVisualScale(s);
    }
}
