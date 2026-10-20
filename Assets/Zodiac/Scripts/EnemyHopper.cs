using UnityEngine;

/// <summary>
/// Cóc Tía — quái nhảy cóc: đứng trên đất nghỉ hopInterval, ngồi thụp xuống báo hiệu (crouchTime) rồi bật nhảy
/// theo đường vòng cung. Thấy người chơi thì nhảy về phía người chơi, không thì nhảy qua lại.
/// Không nhảy về phía có vực sâu. Chạm đất tạo bụi. Đạp lên được.
/// </summary>
public class EnemyHopper : EnemyBase
{
    enum State { Rest, Crouch, Air }

    [Min(0f)] public float hopInterval = 1.1f;
    [Min(0f)] public float crouchTime = 0.3f;
    public Vector2 hopVelocity = new Vector2(4f, 11f);
    [Min(0f)] public float detectRange = 7f;
    [Min(0f)] public float verticalRange = 4f;
    public Color dustColor = new Color(0.6f, 0.5f, 0.35f, 0.7f);

    State state;
    float stateEnd;
    float airSince;

    public string CurrentState => state.ToString();

    protected override void Start()
    {
        base.Start();
        stateEnd = Time.time + hopInterval * Random.Range(0.5f, 1f);
    }

    protected override void Think()
    {
        bool grounded = IsGrounded();
        switch (state)
        {
            case State.Rest:
                SetHorizontalVelocity(0f);
                if (Time.time >= stateEnd && grounded)
                {
                    if (CanSeePlayer(detectRange, verticalRange)) direction = Mathf.Sign(player.transform.position.x - transform.position.x);
                    if (!SafeToHop(direction)) direction = -direction;
                    state = State.Crouch;
                    stateEnd = Time.time + crouchTime;
                }
                break;
            case State.Crouch:
                SetHorizontalVelocity(0f);
                if (Time.time >= stateEnd)
                {
                    rb.linearVelocity = SafeToHop(direction) ? new Vector2(direction * hopVelocity.x, hopVelocity.y)
                                                              : new Vector2(0f, hopVelocity.y * 0.7f);
                    state = State.Air;
                    airSince = Time.time;
                }
                break;
            case State.Air:
                if (grounded && Time.time > airSince + 0.15f && rb.linearVelocity.y <= 0.01f)
                {
                    ZodiacFx.Puff(new Vector2(transform.position.x, transform.position.y + 0.1f), dustColor);
                    state = State.Rest;
                    stateEnd = Time.time + hopInterval;
                }
                break;
        }
    }

    /// <summary>Điểm đáp dự kiến có đất trong vòng 4 unit bên dưới.</summary>
    bool SafeToHop(float dir)
    {
        float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        float airTime = 2f * hopVelocity.y / Mathf.Max(0.01f, g);
        Vector2 land = new Vector2(transform.position.x + dir * hopVelocity.x * airTime, transform.position.y + 1f);
        if (Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y + 0.5f), Vector2.right * dir,
                              Mathf.Abs(land.x - transform.position.x), groundLayer))
            return false;   // tường chắn ngang thân
        return Physics2D.Raycast(land, Vector2.down, 5f, groundLayer);
    }

    protected override void Interrupt()
    {
        if (state == State.Crouch) { state = State.Rest; stateEnd = Time.time + hopInterval; }
    }

    protected override void OnStomped()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        state = State.Rest;
        stateEnd = Time.time + hopInterval;
    }

    protected override void UpdateVisual()
    {
        if (visual == null) return;
        Vector3 s = state switch
        {
            State.Crouch => new Vector3(1.25f, 0.7f, 1f),
            State.Air => new Vector3(0.85f, 1.2f, 1f),
            _ => Vector3.one,
        };
        visual.localScale = new Vector3(s.x * direction, s.y, 1f);
    }
}
