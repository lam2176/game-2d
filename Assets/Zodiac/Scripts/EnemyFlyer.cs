using UnityEngine;

/// <summary>
/// Ma Trơi — quái bay lơ lửng ngang tầm đầu (đánh thường trên đất khó với tới, phải đá bay):
/// lượn ở một làn cố định cạnh người chơi (vài giây mới đổi làn — đổi làn ↑ ↓ là đứng thẳng hàng với nó được);
/// có lượt thì sáng rực lên báo hiệu (windupTime) rồi lao chéo xuống chỗ người chơi đứng lúc đó, lao xong lảo đảo
/// sát đất một lúc (dazeTime — đánh thường với tới) rồi mới bay lên lại.
/// Không chịu trọng lực, không bị quật ngã (chỉ bị đánh dạt ra).
/// </summary>
public class EnemyFlyer : EnemyBase
{
    enum State { Hover, Windup, Swoop, Dazed, Return }

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
    [Tooltip("Lảo đảo sát đất sau cú lao (giây) — cơ hội đánh trả.")]
    [Min(0f)] public float dazeTime = 1.1f;
    [Tooltip("Đổi làn lượn sau mỗi khoảng này (giây).")]
    [Min(0.5f)] public float laneChangeInterval = 3.5f;
    [Min(0)] public int swoopDamage = ZodiacBalance.Unit;
    [Tooltip("Hình sáng lên khi gồng đòn (không bắt buộc).")]
    public SpriteRenderer glow;

    State state;
    float stateEnd, nextSwoop;
    Vector3 swoopDir;
    bool landed;
    float hoverZ, nextLaneChange;

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
                // Người chơi sát tường thì lượn sang phía còn chỗ (không lơ lửng ngay trên đầu)
                if (ClampRoom(player.transform.position.x + side * hoverDistance) != player.transform.position.x + side * hoverDistance)
                    side = -side;
                if (Time.time >= nextLaneChange)
                {
                    nextLaneChange = Time.time + laneChangeInterval * Random.Range(0.8f, 1.2f);
                    hoverZ = ClampLane(player.transform.position.z + Random.Range(-1.2f, 1.2f));
                }
                Vector3 anchor = new Vector3(player.transform.position.x + side * hoverDistance,
                                             player.transform.position.y + hoverHeight + Mathf.Sin(Time.time * 2.3f + laneOffset * 4f) * bobAmplitude,
                                             hoverZ);
                anchor.x = ClampRoom(anchor.x);
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
                if (Time.time >= stateEnd || pos.y < 0.9f || landed ||
                    Physics.Raycast(pos, swoopDir, 0.6f, groundLayer, QueryTriggerInteraction.Ignore))
                {
                    state = State.Dazed;
                    stateEnd = Time.time + dazeTime;
                    ReleaseToken();
                }
                break;
            case State.Dazed:
                // Lơ lửng thấp, lắc lư — ngang tầm đòn đánh thường
                rb.linearVelocity = new Vector3(0f, (1f - pos.y) * 3f, 0f);
                if (Time.time >= stateEnd) StartReturn();
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
        else if (state == State.Dazed) stateEnd = Mathf.Max(stateEnd, Time.time + 0.3f);   // đang bị đánh hội đồng thì chưa bay lên ngay
    }

    protected override void UpdateVisual()
    {
        base.UpdateVisual();
        if (glow == null) return;
        float pulse = state == State.Windup ? 1.4f + Mathf.Sin(Time.time * 30f) * 0.2f
                    : state == State.Dazed ? 0.7f : 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
        glow.transform.localScale = Vector3.one * 1.5f * pulse;
        var c = glow.color;
        c.a = state == State.Windup || state == State.Swoop ? 0.75f : 0.35f;
        glow.color = c;
    }
}
