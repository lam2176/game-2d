using UnityEngine;

/// <summary>
/// Thạch Sư phun lửa — tượng đá đứng yên (kiểu tháp pháo Mega Man / Castlevania):
/// thấy người chơi trong tầm thì miệng sáng dần (chargeTime) rồi phun cầu lửa bay thẳng về phía người chơi.
/// Đạn bay chậm, tường chặn được — nấp sau địa hình hoặc phá tượng. Đứng vững, không bị đẩy lùi.
/// </summary>
public class EnemyTurret : EnemyBase
{
    [Min(0f)] public float rangeX = 11f;
    [Min(0f)] public float rangeY = 6f;
    [Min(0.1f)] public float fireInterval = 2.2f;
    [Min(0f)] public float chargeTime = 0.6f;
    [Min(0f)] public float shotSpeed = 6.5f;
    [Min(0.1f)] public float shotRange = 14f;
    [Min(0)] public int shotDamage = 1;
    public float shotSize = 0.55f;
    public Color shotColor = new Color(1f, 0.45f, 0.1f);
    [Tooltip("Chỉ bắn ngang (không ngắm lên/xuống).")]
    public bool horizontalOnly;
    [Tooltip("Miệng tượng — vị trí ra đạn, sáng lên khi nạp.")]
    public SpriteRenderer mouth;

    float nextFire, chargeEnd;
    bool charging;
    Color mouthIdle;

    protected override void Awake()
    {
        base.Awake();
        if (mouth != null) mouthIdle = mouth.color;
    }

    protected override void Start()
    {
        base.Start();
        nextFire = Time.time + fireInterval * 0.5f;
    }

    protected override void Think()
    {
        bool sees = CanSeePlayer(rangeX, rangeY);
        if (sees) direction = Mathf.Sign(player.transform.position.x - transform.position.x);

        if (!charging && sees && Time.time >= nextFire)
        {
            charging = true;
            chargeEnd = Time.time + chargeTime;
        }
        if (charging && Time.time >= chargeEnd)
        {
            charging = false;
            nextFire = Time.time + fireInterval;
            if (sees) Fire();
        }
    }

    void Fire()
    {
        Vector2 origin = mouth != null ? (Vector2)mouth.transform.position : (Vector2)body.bounds.center;
        Vector2 dir = horizontalOnly ? new Vector2(direction, 0f) : (PlayerCenter - origin).normalized;
        var shot = Projectile.Spawn(origin, dir * shotSpeed, shotSize, shotColor, playerLayer, groundLayer, shotDamage, true);
        shot.range = shotRange;
        shot.owner = transform;
        ZodiacFx.Ring(origin, 0.6f, shotColor, 0.2f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.fireball, 0.7f, 0.1f, origin);
    }

    protected override void Interrupt()
    {
        charging = false;
        nextFire = Mathf.Max(nextFire, Time.time + fireInterval * 0.5f);
    }

    protected override void UpdateVisual()
    {
        base.UpdateVisual();
        if (mouth == null) return;
        float t = charging ? 1f - Mathf.Clamp01((chargeEnd - Time.time) / Mathf.Max(0.01f, chargeTime)) : 0f;
        mouth.color = Color.Lerp(mouthIdle, new Color(1f, 0.9f, 0.3f), t);
        mouth.transform.localScale = Vector3.one * (0.35f + t * 0.2f);
    }
}
