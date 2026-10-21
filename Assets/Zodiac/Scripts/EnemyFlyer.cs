using UnityEngine;

/// <summary>
/// Ma Trơi — quái bay lơ lửng ngang tầm đầu (đánh thường trên đất khó với tới, phải đá bay):
/// lượn vòng quanh người chơi ở khoảng cách an toàn; có lượt thì sáng rực lên báo hiệu (windupTime)
/// rồi lao chéo xuống chỗ người chơi đứng lúc đó, sau đó bay lên lại và nghỉ cooldown.
/// Lúc lao xuống là lúc dễ đánh trả nhất. Không chịu trọng lực, không bị quật ngã (chỉ bị đánh dạt ra).
/// </summary>
public class EnemyFlyer : EnemyBase
{
    enum State { Hover, Windup, Swoop, Return }

    [Header("Bay")]
    [Min(0f)] public float hoverHeight = 2.1f;
    [Min(0f)] public float hoverDistance = 3.5f;
    [Min(0f)] public float hoverSpeed = 3f;
    public float bobAmplitude = 0.3f;

    [Header("Lao tới")]
    [Min(0f)] public float detectRadius = 9f;
    [Min(0f)] public float windupTime = 0.55f;
    [Min(0f)] public float swoopSpeed = 9f;
    [Min(0.1f)] public float swoopMaxTime = 0.9f;
    [Min(0f)] public float cooldown = 1.4f;
    [Min(0)] public int swoopDamage = ZodiacBalance.Unit;
    [Tooltip("Hình sáng lên khi gồng đòn (không bắt buộc).")]
    public SpriteRenderer glow;

    State state;
    float stateEnd, nextSwoop;
    Vector3 swoopDir;
    bool landed;

    public string CurrentState => state.ToString();

    protected override void Awake()
    {
        base.Awake();
        rb.useGravity = false;
        rb.linearDamping = 3f;   // bị đánh văng thì chậm lại nhanh
        canBeKnockedDown = false;
        nextSwoop = Time.time + Random.Range(1f, 2f);
    }

    protected override void Think()
    {
        Vector3 pos = rb.position;
        switch (state)
        {
            case State.Hover:
            {
                if (player == null) break;
                float side = pos.x >= player.transform.position.x ? 1f : -1f;
                Vector3 anchor = player.transform.position +
                                 new Vector3(side * hoverDistance, hoverHeight + Mathf.Sin(Time.time * 2.3f + laneOffset * 4f) * bobAmplitude,
                                             laneOffset * 2f);
                anchor.z = ClampLane(anchor.z);
                rb.linearVelocity = Vector3.ClampMagnitude((anchor - pos) * 3f, hoverSpeed);
                FacePlayer();
                if (Time.time >= nextSwoop && Detects(detectRadius) && TryTakeToken())
                {
                    state = State.Windup;
                    stateEnd = Time.time + windupTime;
                    Alert();
                }
                break;
            }
            case State.Windup:
                rb.linearVelocity = Vector3.zero;
                FacePlayer();
                if (Time.time >= stateEnd)
                {
                    swoopDir = player != null ? (PlayerCenter - pos).normalized : Vector3.right * direction;
                    state = State.Swoop;
                    stateEnd = Time.time + swoopMaxTime;
                    landed = false;
                }
                break;
            case State.Swoop:
                rb.linearVelocity = swoopDir * swoopSpeed;
                if (!landed)
                {
                    Bounds b = body.bounds;
                    landed = StrikeBox(b.center, b.extents + Vector3.one * 0.1f, new Hit(swoopDamage, pos));
                }
                if (Time.time >= stateEnd || pos.y < 0.5f || landed ||
                    Physics.Raycast(pos, swoopDir, 0.6f, groundLayer, QueryTriggerInteraction.Ignore))
                    StartReturn();
                break;
            case State.Return:
                rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.9f, hoverSpeed, rb.linearVelocity.z * 0.9f);
                if (player == null || pos.y >= player.transform.position.y + hoverHeight - 0.2f) state = State.Hover;
                break;
        }
    }

    void StartReturn()
    {
        state = State.Return;
        nextSwoop = Time.time + cooldown;
        ReleaseToken();
    }

    protected override void Interrupt()
    {
        if (state == State.Windup || state == State.Swoop) StartReturn();
    }

    protected override void UpdateVisual()
    {
        base.UpdateVisual();
        if (glow == null) return;
        float pulse = state == State.Windup ? 1.4f + Mathf.Sin(Time.time * 30f) * 0.2f : 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
        glow.transform.localScale = Vector3.one * 1.5f * pulse;
        var c = glow.color;
        c.a = state == State.Windup || state == State.Swoop ? 0.75f : 0.35f;
        glow.color = c;
    }
}
