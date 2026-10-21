using UnityEngine;

/// <summary>
/// Thạch Sư phun lửa — tượng đá đứng yên (thường đặt ở làn trong cùng): thấy người chơi trong tầm thì miệng sáng dần
/// (chargeTime) rồi phun cầu lửa bay ngang tầm ngực về phía người chơi, quét qua các làn.
/// Né: đổi làn, nhảy, hoặc chém tan cầu lửa. Đứng vững, không bị đẩy lùi / quật ngã. Không cần xin lượt — là "áp lực nền".
/// </summary>
public class EnemyTurret : EnemyBase
{
    [Min(0f)] public float range = 12f;
    [Min(0.1f)] public float fireInterval = 2.4f;
    [Min(0f)] public float chargeTime = 0.6f;
    [Min(0f)] public float shotSpeed = 6.5f;
    [Min(0.1f)] public float shotRange = 16f;
    [Min(0)] public int shotDamage = ZodiacBalance.Unit;
    public float shotSize = 0.55f;
    public Color shotColor = new Color(1f, 0.45f, 0.1f);
    [Tooltip("Miệng tượng — vị trí ra đạn, sáng lên khi nạp.")]
    public SpriteRenderer mouth;

    float nextFire, chargeEnd;
    bool charging;
    Color mouthIdle;
    Vector3 mouthScale;

    protected override void Awake()
    {
        base.Awake();
        usesAttackToken = false;
        canBeKnockedDown = false;
        if (mouth != null)
        {
            mouthIdle = mouth.color;
            mouthScale = mouth.transform.localScale;
        }
    }

    protected override void Start()
    {
        base.Start();
        nextFire = Time.time + fireInterval * Random.Range(0.4f, 0.8f);
    }

    protected override void Think()
    {
        bool sees = CanSeePlayer(range);
        if (sees) FacePlayer();

        if (!charging && sees && Time.time >= nextFire)
        {
            charging = true;
            chargeEnd = Time.time + chargeTime;
            Alert();
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
        Vector3 origin = mouth != null ? mouth.transform.position : body.bounds.center;
        Vector3 aim = PlayerCenter - origin;
        aim.y = 0f;   // bay ngang tầm ngực, không cắm xuống đất
        if (aim.sqrMagnitude < 0.01f) aim = Vector3.right * direction;
        var shot = Projectile.Spawn(origin, aim.normalized * shotSpeed, shotSize, shotColor, playerLayer, groundLayer, shotDamage, true);
        shot.range = shotRange;
        shot.owner = transform;
        shot.passWalls = true;   // tượng đặt sát nhà/vách đá phía sau — đạn không bị chính chỗ đứng chặn
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
        mouth.transform.localScale = mouthScale * (1f + t * 0.5f);
    }
}
