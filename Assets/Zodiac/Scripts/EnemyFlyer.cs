using UnityEngine;

/// <summary>
/// Ma Trơi — quái bay (kiểu Vengefly của Hollow Knight / Boo của Mario):
/// lơ lửng nhấp nhô quanh chỗ ở; thấy người chơi trong detectRadius thì sáng rực lên báo hiệu (windupTime)
/// rồi lao thẳng tới vị trí người chơi lúc đó, sau đó bay về chỗ cũ và nghỉ cooldown.
/// Không chịu trọng lực, bị tường chặn. Đạp lên được (nảy như pogo).
/// </summary>
public class EnemyFlyer : EnemyBase
{
    enum State { Hover, Windup, Swoop, Return }

    [Header("Bay")]
    [Min(0f)] public float hoverSpeed = 1.5f;
    [Tooltip("Lượn ngang mỗi phía quanh chỗ ở.")]
    [Min(0f)] public float hoverRange = 2f;
    public float bobAmplitude = 0.4f;

    [Header("Lao tới")]
    [Min(0f)] public float detectRadius = 6f;
    [Min(0f)] public float windupTime = 0.55f;
    [Min(0f)] public float swoopSpeed = 9f;
    [Min(0.1f)] public float swoopMaxTime = 0.9f;
    [Min(0f)] public float returnSpeed = 3f;
    [Min(0f)] public float cooldown = 1.4f;
    [Tooltip("Hình sáng lên khi gồng đòn (không bắt buộc).")]
    public SpriteRenderer glow;

    Vector2 home;
    State state;
    float stateEnd, nextSwoop;
    Vector2 swoopDir;

    public string CurrentState => state.ToString();

    protected override void Awake()
    {
        base.Awake();
        home = transform.position;
        rb.gravityScale = 0f;
        rb.linearDamping = 3f;   // bị đánh văng thì chậm lại nhanh
    }

    protected override void Think()
    {
        Vector2 pos = rb.position;
        switch (state)
        {
            case State.Hover:
            {
                float x = home.x + Mathf.Sin(Time.time * hoverSpeed / Mathf.Max(0.1f, hoverRange)) * hoverRange;
                float y = home.y + Mathf.Sin(Time.time * 2.3f + home.x) * bobAmplitude;
                rb.linearVelocity = (new Vector2(x, y) - pos) * 4f;
                if (Mathf.Abs(rb.linearVelocity.x) > 0.1f) direction = Mathf.Sign(rb.linearVelocity.x);
                if (Time.time >= nextSwoop && player != null && CanSeePlayer(detectRadius, detectRadius) &&
                    Vector2.Distance(PlayerCenter, pos) <= detectRadius)
                {
                    state = State.Windup;
                    stateEnd = Time.time + windupTime;
                    Alert();
                }
                break;
            }
            case State.Windup:
                rb.linearVelocity = Vector2.zero;
                if (player != null) direction = Mathf.Sign(PlayerCenter.x - pos.x);
                if (Time.time >= stateEnd)
                {
                    swoopDir = player != null ? (PlayerCenter - pos).normalized : Vector2.right * direction;
                    state = State.Swoop;
                    stateEnd = Time.time + swoopMaxTime;
                }
                break;
            case State.Swoop:
                rb.linearVelocity = swoopDir * swoopSpeed;
                if (Time.time >= stateEnd || Physics2D.Raycast(pos, swoopDir, 0.6f, groundLayer)) StartReturn();
                break;
            case State.Return:
            {
                Vector2 to = home - pos;
                rb.linearVelocity = to.normalized * returnSpeed;
                if (to.magnitude < 0.3f) state = State.Hover;
                break;
            }
        }
    }

    void StartReturn()
    {
        state = State.Return;
        nextSwoop = Time.time + cooldown;
    }

    protected override void Interrupt()
    {
        if (state == State.Windup || state == State.Swoop) StartReturn();
    }

    protected override void OnStomped() => StartReturn();

    protected override void UpdateVisual()
    {
        base.UpdateVisual();
        if (glow == null) return;
        float pulse = state == State.Windup ? 1.4f + Mathf.Sin(Time.time * 30f) * 0.2f : 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
        glow.transform.localScale = Vector3.one * pulse;
        var c = glow.color;
        c.a = state == State.Windup || state == State.Swoop ? 0.75f : 0.35f;
        glow.color = c;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)home : transform.position, detectRadius);
    }
}
