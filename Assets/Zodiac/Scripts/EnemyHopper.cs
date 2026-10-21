using UnityEngine;

/// <summary>
/// Cóc Tía — nhảy cóc đè người: ngồi thụp xuống báo hiệu (crouchTime), một vòng đỏ hiện trên mặt đất chỗ sẽ đáp
/// (chính là chỗ người chơi đang đứng), rồi bật nhảy vòng cung và đáp xuống tạo sóng chấn tròn (slamRadius).
/// Né: rời khỏi vòng đỏ trước khi nó đáp, hoặc đang nhảy lúc nó đáp (sóng chấn chỉ trúng người đứng trên đất).
/// </summary>
public class EnemyHopper : EnemyBase
{
    enum State { Rest, Crouch, Air }

    [Min(0f)] public float restTime = 1.3f;
    [Min(0f)] public float crouchTime = 0.45f;
    [Tooltip("Thời gian bay mỗi cú nhảy (giây).")]
    [Min(0.2f)] public float airTime = 0.75f;
    [Tooltip("Nhảy xa tối đa (unit).")]
    [Min(0f)] public float maxLeap = 6f;
    [Min(0f)] public float detectRange = 10f;
    [Min(0f)] public float slamRadius = 1.6f;
    [Min(0)] public int slamDamage = ZodiacBalance.Unit;
    public Color dustColor = new Color(0.6f, 0.5f, 0.35f, 0.7f);
    public Color warningColor = new Color(1f, 0.25f, 0.2f, 0.6f);

    State state;
    float stateEnd, airSince;
    Vector3 landing;
    SpriteRenderer marker;

    public string CurrentState => state.ToString();

    protected override void Start()
    {
        base.Start();
        stateEnd = Time.time + restTime * Random.Range(0.5f, 1f);
    }

    protected override void Think()
    {
        switch (state)
        {
            case State.Rest:
                SetPlanarVelocity(Vector3.zero);
                FacePlayer();
                if (Time.time >= stateEnd && IsGrounded() && Detects(detectRange) && TryTakeToken())
                {
                    Alert();
                    Vector3 to = Vector3.ClampMagnitude(ToPlayer, maxLeap);
                    landing = transform.position + to;
                    landing.z = ClampLane(landing.z);
                    landing.x = ClampRoom(landing.x);
                    ShowMarker(true);
                    state = State.Crouch;
                    stateEnd = Time.time + crouchTime;
                }
                break;
            case State.Crouch:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd) Leap();
                break;
            case State.Air:
                if (Time.time > airSince + 0.2f && IsGrounded() && rb.linearVelocity.y <= 0.01f) Land();
                break;
        }
        UpdateMarker();
    }

    void Leap()
    {
        // Vận tốc ném xiên để đáp đúng điểm sau airTime giây (trọng lực thật = gravity × gravityMultiplier)
        float g = -Physics.gravity.y * Mathf.Max(1f, gravityMultiplier);
        Vector3 d = landing - transform.position;
        rb.linearVelocity = new Vector3(d.x / airTime, g * airTime * 0.5f, d.z / airTime);
        state = State.Air;
        airSince = Time.time;
    }

    void Land()
    {
        ShowMarker(false);
        SetPlanarVelocity(Vector3.zero);
        ZodiacFx.Ring(transform.position, slamRadius, dustColor, 0.35f, flat: true);
        ZodiacParticles.Dust(transform.position + Vector3.up * 0.1f, Vector3.left, 6, dustColor);
        ZodiacParticles.Dust(transform.position + Vector3.up * 0.1f, Vector3.right, 6, dustColor);
        GameFeel.Shake(0.15f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.stomp, 0.8f, 0.1f, transform.position);
        // Sóng chấn: chỉ trúng người chơi đang đứng trên đất trong bán kính
        if (PlayerAvailable && player.IsGrounded && ToPlayer.magnitude <= slamRadius)
            playerHealth?.TakeDamage(new Hit(slamDamage, transform.position, 1.2f));
        state = State.Rest;
        stateEnd = Time.time + restTime;
        ReleaseToken();
    }

    void ShowMarker(bool show)
    {
        if (show && marker == null)
        {
            marker = ZodiacFx.Shape("LandingMarker", true, landing, Vector2.one * slamRadius * 2f, warningColor, 3);
            marker.sprite = ZodiacFx.RingSprite;
        }
        if (marker == null) return;
        marker.gameObject.SetActive(show);
        if (show) marker.transform.SetPositionAndRotation(landing + Vector3.up * 0.04f, Quaternion.Euler(90f, 0f, 0f));
    }

    void UpdateMarker()
    {
        if (marker == null || !marker.gameObject.activeSelf) return;
        float k = 0.85f + Mathf.Sin(Time.time * 18f) * 0.15f;
        marker.transform.localScale = Vector3.one * slamRadius * 2f * k;
    }

    protected override void Interrupt()
    {
        if (state == State.Crouch)
        {
            ShowMarker(false);
            state = State.Rest;
            stateEnd = Time.time + restTime;
            ReleaseToken();
        }
    }

    protected override void OnDied()
    {
        if (marker != null) Destroy(marker.gameObject);
        base.OnDied();
    }

    protected override void OnDamaged(Hit hit)
    {
        base.OnDamaged(hit);
        if (!IsDown || state == State.Rest) return;
        // Bị quật ngã giữa chừng: huỷ cú nhảy, đứng dậy rồi mới tính tiếp (không đáp "ma" tạo sóng chấn)
        ShowMarker(false);
        state = State.Rest;
        stateEnd = Time.time + restTime;
        ReleaseToken();
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
        SetVisualScale(s);
    }
}
