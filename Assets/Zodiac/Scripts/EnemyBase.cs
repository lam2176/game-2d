using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phần chung của mọi quái trong beat 'em up 2.5D: bị đánh (đẩy lùi, choáng, quật ngã, tung hứng trên không),
/// chết (văng lên lộn vòng rồi mờ dần), và các tiện ích di chuyển trên mặt đường (trục X dọc đường, Z là làn).
/// Lớp con chỉ viết AI trong <see cref="Think"/> — chỉ chạy khi không choáng / không nằm đất.
///
/// Quái không gây sát thương khi chạm — mọi sát thương đến từ đòn đánh có báo hiệu (gồng → ra đòn), để người chơi
/// luôn đọc được và né được (đổi làn, lăn, nhảy). Nhiều quái cùng vây: chỉ <see cref="MaxAttackers"/> con được
/// "lượt tấn công" cùng lúc (<see cref="TryTakeToken"/>), số còn lại đứng vòng ngoài chờ — như Streets of Rage / Sifu,
/// tránh cảnh bị cả đám đánh cùng một lúc không kịp phản ứng.
///
/// Phản hồi: khựng/bẹp người khi trúng đòn, dấu "!" khi phát hiện người chơi, sao quay khi bị choáng do chiêu.
/// Quái có khiên (guardFront) chặn mọi đòn từ phía trước — vòng ra sau lưng (đổi làn đi vòng), đá bay từ trên xuống,
/// hoặc làm choáng bằng chiêu để hạ khiên.
/// Quái cầm đồ (<see cref="gear"/>: khiên, giáo): đỡ đủ <see cref="gearDurability"/> đòn hoặc máu tụt dưới
/// <see cref="gearLostAtHealth"/> thì đồ văng ra (<see cref="GearDebris"/>) và quái thành lính thường (<see cref="OnGearLost"/>).
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(Health))]
public abstract class EnemyBase : MonoBehaviour
{
    /// <summary>Số quái được vây đánh cùng lúc.</summary>
    public static int MaxAttackers = 2;
    static readonly HashSet<EnemyBase> attackers = new HashSet<EnemyBase>();

    [Header("Chung")]
    public LayerMask groundLayer;
    public LayerMask playerLayer;
    [Tooltip("Object con chứa hình, được lật theo hướng nhìn.")]
    public Transform visual;
    [Tooltip("Trọng lực nhân thêm (rơi nhanh, gọn như nhân vật).")]
    [Min(0f)] public float gravityMultiplier = 2.5f;

    [Header("Bị đánh")]
    [Tooltip("Vận tốc bị đẩy ra xa (x: ngang mặt đường, y: bật lên).")]
    public Vector2 knockback = new Vector2(4f, 3f);
    [Tooltip("Hệ số nhận đẩy lùi (0 = đứng vững như boss, tượng đá).")]
    [Min(0f)] public float knockbackTaken = 1f;
    [Min(0f)] public float stunTime = 0.3f;
    [Tooltip("Nhân thời gian choáng do chiêu (boss < 1).")]
    [Min(0f)] public float stunMultiplier = 1f;
    [Tooltip("Bị đòn quật ngã làm ngã nằm đất được không.")]
    public bool canBeKnockedDown = true;
    [Tooltip("Thời gian nằm đất (giây) — bất tử trong lúc nằm và đứng dậy.")]
    [Min(0f)] public float downTime = 0.8f;
    [Min(0f)] public float deathFadeTime = 0.4f;
    [Tooltip("Màu mảnh vỡ khi chết.")]
    public Color debrisColor = Color.gray;

    [Header("Khiên")]
    [Tooltip("Chặn đòn đánh từ phía trước (theo hướng nhìn). Đòn từ trên xuống / sau lưng vẫn trúng.")]
    public bool guardFront;
    [Tooltip("Hình khiên (nảy lên khi đỡ đòn).")]
    public Transform shield;

    [Header("Đồ cầm tay")]
    [Tooltip("Khiên / vũ khí — văng ra khi đỡ đủ đòn hoặc máu tụt dưới ngưỡng; mất đồ thì thành quái thường.")]
    public Transform gear;
    [Tooltip("Số đòn đỡ được trước khi đồ vỡ (khiên). 0 = không vỡ vì đỡ đòn.")]
    [Min(0)] public int gearDurability;
    [Tooltip("Máu còn dưới tỉ lệ này thì đồ văng ra.")]
    [Range(0f, 1f)] public float gearLostAtHealth = 0.5f;
    [Tooltip("Đồ văng ra thành vật phẩm nhặt được (giáo đồng, khiên trống đồng) với tỉ lệ gearItemChance.")]
    public ItemDefinition gearItem;
    [Range(0f, 1f)] public float gearItemChance = 0.2f;

    [Header("Rơi đồ khi chết")]
    public LootEntry[] loot = System.Array.Empty<LootEntry>();
    [Tooltip("Số đồng xu rơi (thấp nhất, cao nhất).")]
    public Vector2Int coinDrop = new Vector2Int(1, 3);
    [Tooltip("XP cho con giáp đang chơi khi hạ quái này. 0 = tự tính theo máu tối đa.")]
    [Min(0)] public int xpReward;

    [Header("Vây đánh")]
    [Tooltip("Phải xin lượt mới được tấn công (tối đa MaxAttackers con cùng lúc).")]
    public bool usesAttackToken = true;
    [Tooltip("Biết người chơi ở đâu ngay từ đầu (quái ra theo đợt từ mép màn hình đi vào).")]
    public bool alwaysAggro;

    [Tooltip("Nhân tốc độ di chuyển (quái tinh anh Cuồng nộ).")]
    [HideInInspector] public float speedScale = 1f;
    [Tooltip("Nhân sát thương đòn đánh (độ khó, tinh anh).")]
    [HideInInspector] public float damageScale = 1f;
    [Tooltip("Nhân đồ rơi và XP khi chết (độ khó, tinh anh).")]
    [HideInInspector] public float rewardScale = 1f;
    /// <summary>Tinh anh (null = quái thường).</summary>
    public EliteAffix Elite { get; set; }

    protected Rigidbody rb;
    protected Collider body;
    protected Health health;
    protected ZodiacPlayerController player;
    protected Health playerHealth;
    protected float direction = 1f;
    /// <summary>Lệch làn riêng của con này khi đứng vây (để cả đám không chồng lên một chỗ).</summary>
    protected float laneOffset;
    float stunnedUntil = float.NegativeInfinity;
    float downUntil = float.NegativeInfinity;
    bool knockedDown, lying;
    float launchedAt;
    Transform stunIcon;
    Vector3 visualScale = Vector3.one;
    float punch, shieldPunch, nextAlert, downPose;
    int blocksTaken;
    bool gearLost;
    readonly Collider[] hits = new Collider[4];
    // Bị túm / bị ném (PlayerGrab)
    bool held, thrown;
    int thrownDamage;
    CharacterStats thrower;
    readonly System.Collections.Generic.HashSet<Health> thrownHits = new System.Collections.Generic.HashSet<Health>();
    readonly Collider[] thrownOverlaps = new Collider[8];

    public bool IsStunned => Time.time < stunnedUntil;
    /// <summary>Đang bị quật ngã (đang bay hoặc nằm đất) — không hành động.</summary>
    public bool IsDown => knockedDown;
    public Health Health => health;
    /// <summary>Còn cầm đồ (khiên / vũ khí).</summary>
    public bool HasGear => gear != null && !gearLost;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        body = GetComponent<Collider>();
        health = GetComponent<Health>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        health.Damaged += OnDamaged;
        health.Died += OnDied;
        if (guardFront)
        {
            health.Blocks = BlocksHit;
            health.Blocked += OnBlocked;
        }
        laneOffset = Random.Range(-0.6f, 0.6f);
    }

    protected virtual void Start()
    {
        player = ZodiacPlayerController.Current != null ? ZodiacPlayerController.Current : FindAnyObjectByType<ZodiacPlayerController>();
        if (player != null) playerHealth = player.GetComponent<Health>();
    }

    void OnDisable() => attackers.Remove(this);

    void FixedUpdate()
    {
        if (health.IsDead) return;
        if (held) return;
        if (rb.useGravity && !rb.isKinematic && gravityMultiplier > 1f)
            rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
        UpdateStunIcon();
        if (knockedDown)
        {
            if (thrown && !lying) ThrownImpact();
            UpdateKnockdown();
            return;
        }
        if (IsStunned) return;
        Think();
        KeepPersonalSpace();
        UpdateVisual();
    }

    /// <summary>
    /// Quái không va chạm vật lý với người chơi (để không đẩy nhau), nên tự giữ khoảng cách tối thiểu: không bao giờ đứng
    /// đè lên người chơi — người chơi luôn thấy rõ và đánh trúng được.
    /// </summary>
    void KeepPersonalSpace()
    {
        if (player == null || rb.isKinematic || !rb.useGravity) return;
        Vector3 d = transform.position - player.transform.position;
        if (Mathf.Abs(d.z) > 0.6f || Mathf.Abs(d.x) >= MinPlayerDistance) return;
        float side = Mathf.Abs(d.x) > 0.05f ? Mathf.Sign(d.x) : -direction;
        // Sát tường thì lùi sang phía kia của người chơi
        if (ClampRoom(transform.position.x + side) != transform.position.x + side) side = -side;
        var v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(side * 3f, v.y, v.z);
    }

    /// <summary>Khoảng cách ngang tối thiểu tới người chơi khi cùng làn.</summary>
    protected virtual float MinPlayerDistance => 0.75f;

    /// <summary>AI mỗi bước vật lý (không gọi khi đang choáng, nằm đất hoặc đã chết).</summary>
    protected abstract void Think();

    /// <summary>Huỷ đòn đang ra (bị đánh, bị choáng).</summary>
    protected virtual void Interrupt() { }

    /// <summary>Choáng thêm (chiêu sóng âm, sóng chấn).</summary>
    public void Stun(float seconds)
    {
        seconds *= stunMultiplier;
        if (seconds < 0.15f) return;   // choáng quá ngắn với boss thì bỏ qua — không ngắt được đòn
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds);
        Interrupt();
    }

    // ───────── Lượt vây đánh ─────────

    protected bool HasToken => !usesAttackToken || attackers.Contains(this);

    /// <summary>Xin lượt tấn công — false nếu đã đủ MaxAttackers quái đang vây đánh.</summary>
    protected bool TryTakeToken()
    {
        if (!usesAttackToken || attackers.Contains(this)) return true;
        attackers.RemoveWhere(e => e == null || !e.isActiveAndEnabled || e.health.IsDead);
        if (attackers.Count >= MaxAttackers) return false;
        attackers.Add(this);
        return true;
    }

    protected void ReleaseToken() => attackers.Remove(this);

    // ───────── Người chơi ─────────

    protected bool PlayerAvailable =>
        player != null && player.ControlEnabled && !player.IsHidden && (playerHealth == null || !playerHealth.IsDead);

    /// <summary>Vector từ quái tới người chơi trên mặt đường (bỏ chiều cao).</summary>
    protected Vector3 ToPlayer
    {
        get
        {
            if (player == null) return Vector3.zero;
            Vector3 d = player.transform.position - transform.position;
            d.y = 0f;
            return d;
        }
    }

    protected bool CanSeePlayer(float range) => PlayerAvailable && ToPlayer.sqrMagnitude <= range * range;

    /// <summary>Phát hiện người chơi: trong tầm, hoặc luôn biết nếu <see cref="alwaysAggro"/>.</summary>
    protected bool Detects(float range) => PlayerAvailable && (alwaysAggro || ToPlayer.sqrMagnitude <= range * range);

    protected Vector3 PlayerCenter => player != null ? player.BodyCenter : transform.position;

    /// <summary>Z kẹp trong mặt đường (chừa mép).</summary>
    protected static float ClampLane(float z) =>
        Mathf.Clamp(z, ZodiacGameManager.LaneMinZ + 0.3f, ZodiacGameManager.LaneMaxZ - 0.3f);

    /// <summary>X kẹp trong khu đang đánh (chừa mép) — quái không chạy ra khỏi khung hình.</summary>
    protected static float ClampRoom(float x) =>
        Mathf.Clamp(x, ZodiacGameManager.RoomMinX + 0.7f, ZodiacGameManager.RoomMaxX - 0.7f);

    /// <summary>Điểm đứng đánh: ngang hàng người chơi (cùng làn), cách attackRange về phía quái đang đứng.</summary>
    protected Vector3 EngagePoint(float attackRange)
    {
        float side = transform.position.x >= player.transform.position.x ? 1f : -1f;
        Vector3 p = player.transform.position;
        // Người chơi sát tường thì đứng đánh phía bên kia
        if (ClampRoom(p.x + side * attackRange) != p.x + side * attackRange) side = -side;
        return new Vector3(ClampRoom(p.x + side * attackRange), transform.position.y, ClampLane(p.z));
    }

    /// <summary>Điểm đứng chờ lượt: vòng ngoài, giữ làn riêng — trông như đang rình chứ không đứng xếp hàng.</summary>
    protected Vector3 WaitPoint(float distance)
    {
        float side = transform.position.x >= player.transform.position.x ? 1f : -1f;
        Vector3 p = player.transform.position;
        float sway = Mathf.Sin(Time.time * 0.9f + laneOffset * 5f) * 0.6f;
        return new Vector3(ClampRoom(p.x + side * (distance + sway)), transform.position.y, ClampLane(p.z + laneOffset * 2.5f));
    }

    /// <summary>Đi về điểm cho trước (chỉ trên mặt đường), trả về true khi đã tới.</summary>
    protected bool MoveTo(Vector3 point, float speed, float tolerance = 0.15f)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude <= tolerance * tolerance)
        {
            SetPlanarVelocity(Vector3.zero);
            return true;
        }
        Vector3 v = d.normalized * speed;
        v.z *= 0.75f;   // đổi làn chậm hơn đi ngang, giống người chơi
        SetPlanarVelocity(v);
        return false;
    }

    protected void SetPlanarVelocity(Vector3 v)
    {
        if (rb.isKinematic) return;
        rb.linearVelocity = new Vector3(v.x * speedScale, rb.linearVelocity.y, v.z * speedScale);
    }

    /// <summary>Đòn của quái sau khi nhân độ khó / tinh anh.</summary>
    protected Hit Scaled(Hit hit)
    {
        hit.amount = Mathf.Max(1, Mathf.RoundToInt(hit.amount * damageScale));
        return hit;
    }

    /// <summary>Đánh trúng người chơi trực tiếp (dậm đất, lao vào): nhân độ khó và áp hiệu ứng tinh anh.</summary>
    protected bool HitPlayer(Hit hit)
    {
        if (playerHealth == null || !playerHealth.TakeDamage(Scaled(hit))) return false;
        if (Elite != null) Elite.OnHitPlayer(playerHealth);
        return true;
    }

    protected void FacePlayer()
    {
        if (player == null) return;
        float dx = player.transform.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.05f) direction = Mathf.Sign(dx);
    }

    /// <summary>Quét hộp đòn đánh; trúng người chơi thì trừ máu. Trả về true nếu trúng.</summary>
    protected bool StrikeBox(Vector3 center, Vector3 halfExtents, Hit hit)
    {
        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, hits, Quaternion.identity, playerLayer, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var target = hits[i].GetComponentInParent<Health>();
            if (target == null || target == health) continue;
            if (!target.TakeDamage(Scaled(hit))) return false;
            if (Elite != null) Elite.OnHitPlayer(target);
            return true;
        }
        return false;
    }

    /// <summary>Dấu "!" vàng bật lên trên đầu khi phát hiện người chơi (tối đa mỗi 2.5 giây).</summary>
    protected void Alert()
    {
        if (Time.time < nextAlert) return;
        nextAlert = Time.time + 2.5f;
        Vector3 top = new Vector3(body.bounds.center.x, body.bounds.max.y + 0.6f, body.bounds.center.z);
        var mark = new GameObject("Alert");
        mark.transform.SetPositionAndRotation(top, ZodiacFx.FacingCamera);
        var color = new Color(1f, 0.85f, 0.2f);
        ZodiacFx.Shape("Bar", false, top + Vector3.up * 0.12f, new Vector2(0.12f, 0.38f), color, 30, mark.transform);
        ZodiacFx.Shape("Dot", false, top - Vector3.up * 0.18f, new Vector2(0.12f, 0.12f), color, 30, mark.transform);
        mark.AddComponent<AlertMark>();
    }

    // ───────── Khiên ─────────

    bool BlocksHit(Hit hit)
    {
        if (stunnedUntil - Time.time > 0.45f || knockedDown) return false;   // choáng do chiêu / đang ngã → hạ khiên
        Bounds b = body.bounds;
        Vector3 d = hit.source - b.center;
        if (d.y > b.extents.y) return false;                                  // đá bay từ trên xuống
        return Mathf.Abs(d.x) > 0.01f && Mathf.Sign(d.x) == direction;
    }

    void OnBlocked(Hit hit)
    {
        shieldPunch = 1f;
        Vector3 at = shield != null ? shield.position : body.bounds.center + Vector3.right * direction * 0.5f;
        ZodiacParticles.Sparks(at, new Color(1f, 0.95f, 0.7f), 10);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.block, 0.9f, 0.1f, at);
        GameFeel.HitStop(0.04f);
        GameFeel.Shake(0.1f);
        // Đánh vào khiên ở tầm gần: người chơi bị bật ngược lại
        if (player != null && Mathf.Abs(player.transform.position.x - transform.position.x) < 2.5f)
            player.ApplyKnockback(new Vector3(direction * 6f, 2f, 0f), 0.15f);
        if (gearDurability > 0 && ++blocksTaken >= gearDurability) LoseGear(hit.source);
    }

    /// <summary>Đồ văng ra: hết đỡ đòn, lớp con đổi chỉ số trong <see cref="OnGearLost"/>.</summary>
    void LoseGear(Vector3 source)
    {
        if (!HasGear) return;
        gearLost = true;
        if (guardFront)
        {
            guardFront = false;
            health.Blocks = null;
            health.Blocked -= OnBlocked;
        }
        if (shield == gear) shield = null;
        if (TryGetComponent(out HitFlash flash)) flash.Forget(gear);
        Vector3 at = gear.position;
        ZodiacParticles.Sparks(at, new Color(1f, 0.9f, 0.6f), 14);
        ZodiacParticles.Debris(at, new Color(0.7f, 0.55f, 0.3f), 6, 0.3f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.block, 1f, 0.15f, at);
        GameFeel.HitStop(0.06f);
        GameFeel.Shake(0.15f);
        if (gearItem != null && Random.value < gearItemChance) LootPickup.Spawn(ItemAffixes.Roll(gearItem), 0, gear.position);
        GearDebris.Launch(gear, transform.position - source);
        OnGearLost();
    }

    /// <summary>Vừa mất đồ — lớp con giảm sức đánh / tầm đánh, thành quái thường.</summary>
    protected virtual void OnGearLost() { }

    // ───────── Bị đánh ─────────

    protected virtual void OnDamaged(Hit hit)
    {
        punch = 1f;
        ComboCounter.Register();
        float k = hit.knockback * knockbackTaken;
        Vector3 away = transform.position - hit.source;
        away.y = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.right * -direction;
        if (Mathf.Abs(away.x) > 0.05f) direction = -Mathf.Sign(away.x);   // quay mặt về phía bị đánh

        if (k > 0f && !rb.isKinematic)
        {
            if ((hit.knockdown || knockedDown) && canBeKnockedDown)
            {
                // Quật ngã, hoặc đang bay mà trúng tiếp (tung hứng): văng lên tiếp
                bool juggle = knockedDown && !hit.knockdown;
                knockedDown = true;
                lying = false;
                launchedAt = Time.time;
                float up = juggle ? 4f : 7f;
                rb.linearVelocity = away * knockback.x * Mathf.Max(k, 0.6f) * 1.2f + Vector3.up * up;
                ReleaseToken();
            }
            else
            {
                rb.linearVelocity = away * knockback.x * k + Vector3.up * knockback.y * Mathf.Min(k, 1.5f) * 0.5f;
                stunnedUntil = Mathf.Max(stunnedUntil, Time.time + stunTime);
            }
            Interrupt();
        }
        if (hit.stun > 0f) Stun(hit.stun);
        if (HasGear && !health.IsDead && health.Normalized <= gearLostAtHealth) LoseGear(hit.source);
    }

    void UpdateKnockdown()
    {
        if (!lying)
        {
            // Đang bay: chạm đất thì nằm xuống, bụi tung
            if (Time.time > launchedAt + 0.15f && IsGrounded() && rb.linearVelocity.y <= 0.05f)
            {
                if (thrown)
                {
                    // Bị ném: đập xuống đất mới mất máu (không đẩy thêm)
                    thrown = false;
                    if (thrownDamage > 0) health.TakeDamage(CharacterStats.CreateHit(thrower, thrownDamage, transform.position, 0f));
                    if (health.IsDead) return;
                }
                lying = true;
                downUntil = Time.time + downTime;
                health.SetInvulnerable(downTime + 0.35f);
                SetPlanarVelocity(Vector3.zero);
                ZodiacParticles.Dust(transform.position + Vector3.up * 0.1f, Vector3.left, 5);
                ZodiacParticles.Dust(transform.position + Vector3.up * 0.1f, Vector3.right, 5);
                GameFeel.Shake(0.08f);
            }
            return;
        }
        SetPlanarVelocity(Vector3.zero);
        if (Time.time >= downUntil)
        {
            knockedDown = lying = false;
            stunnedUntil = Time.time + 0.25f;   // đứng dậy chưa đánh ngay
        }
    }

    /// <summary>Màu mảnh vỡ khi chết.</summary>
    protected Color DebrisColor => debrisColor;

    // ───────── Bị túm / ném ─────────

    /// <summary>Túm được không: đang choáng, đứng trên đất, quật ngã được, không phải boss.</summary>
    public bool CanBeGrabbed =>
        !health.IsDead && !knockedDown && !held && canBeKnockedDown && rb.useGravity && !rb.isKinematic &&
        IsStunned && !(this is BossFoxSpirit);

    /// <summary>Người chơi túm: đứng im trong tay, không tự hành động.</summary>
    public void Grab()
    {
        held = true;
        ReleaseToken();
        Interrupt();
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        punch = 1f;
    }

    /// <summary>Giữ quái ở vị trí cho trước (trước mặt người chơi).</summary>
    public void HoldAt(Vector3 position)
    {
        if (!held) return;
        rb.position = position;
        transform.position = position;
    }

    /// <summary>
    /// Ném (hoặc thả) quái đang bị túm: văng theo vận tốc, nằm đất khi chạm đất. damage &gt; 0: đâm phải ai thì người đó
    /// mất damage máu và bị quật ngã, bản thân mất damage máu khi đập đất.
    /// </summary>
    public void Throw(Vector3 velocity, int damage, CharacterStats by)
    {
        held = false;
        if (health.IsDead) return;
        rb.isKinematic = false;
        knockedDown = true;
        lying = false;
        launchedAt = Time.time;
        rb.linearVelocity = velocity;
        if (Mathf.Abs(velocity.x) > 0.05f) direction = -Mathf.Sign(velocity.x);
        thrown = true;
        thrownDamage = damage;
        thrower = by;
        thrownHits.Clear();
        thrownHits.Add(health);
    }

    /// <summary>Đang bay vì bị ném: đâm quái khác / hũ / rương thì gây sát thương và quật ngã.</summary>
    void ThrownImpact()
    {
        if (thrownDamage <= 0) return;
        var b = body.bounds;
        int count = Physics.OverlapBoxNonAlloc(b.center, b.extents + new Vector3(0.15f, 0.1f, 0.15f), thrownOverlaps, Quaternion.identity,
                                               1 << gameObject.layer, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var target = thrownOverlaps[i].GetComponentInParent<Health>();
            if (target == null || target.IsDead || !thrownHits.Add(target)) continue;
            var hit = CharacterStats.CreateHit(thrower, thrownDamage, transform.position, 1.3f);
            hit.knockdown = true;
            if (target.TakeDamage(hit)) GameFeel.Impact(thrownOverlaps[i].ClosestPoint(b.center), Color.white, true);
        }
    }

    float hpScaleApplied = 1f;

    /// <summary>
    /// Áp độ khó lúc quái xuất hiện: máu tối đa ×hp, đòn ×damage, đồ rơi / XP ×reward.
    /// Gọi một lần ngay sau khi sinh (hoặc khi boss bắt đầu).
    /// </summary>
    public void ApplyScaling(float hp, float damage, float reward)
    {
        hpScaleApplied *= hp;
        health.Configure(Mathf.Max(1, Mathf.RoundToInt(health.maxHealth * hp)));
        damageScale *= damage;
        rewardScale *= reward;
    }

    protected virtual void OnDied()
    {
        ReleaseToken();
        LootPickup.DropFrom(loot, coinDrop, transform.position, rewardScale);
        if (Elite != null) Elite.DropBonus(transform.position);
        if (player != null && player.Character != null)
        {
            // XP theo máu gốc (trước khi độ khó / tinh anh nhân lên) rồi nhân thưởng
            int baseXp = xpReward > 0 ? xpReward : Progression.XpFor(Mathf.RoundToInt(health.maxHealth / Mathf.Max(0.01f, hpScaleApplied)), this is BossFoxSpirit);
            PlayerProfile.Current.AddXp(player.Character.name, Mathf.RoundToInt(baseXp * rewardScale));
        }
        ZodiacParticles.Debris(body.bounds.center, DebrisColor, 14, body.bounds.extents.x);
        ZodiacParticles.Sparks(body.bounds.center, Color.white, 10);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.enemyDeath, 0.9f, 0.1f, transform.position);
        GameFeel.Shake(0.15f);
        if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        foreach (var c in GetComponents<Collider>()) c.enabled = false;
        SetVisualScale(new Vector3(direction, 1f, 1f));
        if (TryGetComponent(out HitFlash flash)) flash.enabled = false;
        if (stunIcon != null) stunIcon.gameObject.SetActive(false);
        StartCoroutine(DeathTumble());
    }

    /// <summary>Văng lên, lộn vòng theo hướng bị đánh rồi rơi xuống, mờ dần (thay vì biến mất tại chỗ).</summary>
    IEnumerator DeathTumble()
    {
        var root = visual != null ? visual : transform;
        var renderers = root.GetComponentsInChildren<SpriteRenderer>();
        var startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) startColors[i] = renderers[i].color;
        Vector3 start = root.localPosition;
        float away = player != null ? Mathf.Sign(transform.position.x - player.transform.position.x) : -direction;
        Vector2 velocity = new Vector2(away * 3f, 7f);
        Vector2 offset = Vector2.zero;
        float spin = 0f, time = Mathf.Max(0.5f, deathFadeTime + 0.3f);
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            velocity.y -= 25f * Time.deltaTime;
            offset += velocity * Time.deltaTime;
            spin += -away * 540f * Time.deltaTime;
            root.localPosition = start + (Vector3)offset;
            root.localRotation = Quaternion.Euler(0f, 0f, spin);
            float a = 1f - Mathf.Clamp01((t - time * 0.4f) / (time * 0.6f));
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].color = new Color(startColors[i].r, startColors[i].g, startColors[i].b, startColors[i].a * a);
            yield return null;
        }
        gameObject.SetActive(false);
    }

    // ───────── Hiển thị ─────────

    protected virtual void UpdateVisual() => SetVisualScale(new Vector3(direction, 1f, 1f));

    /// <summary>Tư thế (tỉ lệ) của hình do AI đặt — phần khựng khi trúng đòn được nhân thêm ở LateUpdate.</summary>
    protected void SetVisualScale(Vector3 scale) => visualScale = scale;

    void LateUpdate()
    {
        if (visual == null || health.IsDead) return;
        // Trúng đòn: bẹp ngang rồi nảy lại (chạy cả khi đang choáng)
        punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 6f);
        float p = Mathf.Sin(punch * Mathf.PI) * punch;
        Vector3 s = visualScale;
        s.x = Mathf.Abs(s.x) * direction;
        visual.localScale = Vector3.Scale(s, new Vector3(1f + 0.25f * p, 1f - 0.2f * p, 1f));
        // Ngã ngửa ra sau khi bị quật ngã, đứng dậy khi hết thời gian nằm
        downPose = Mathf.MoveTowards(downPose, knockedDown ? 1f : 0f, Time.deltaTime * (knockedDown ? 6f : 4f));
        visual.localRotation = Quaternion.Euler(0f, 0f, downPose * 85f * direction);
        if (shield != null)
        {
            shieldPunch = Mathf.MoveTowards(shieldPunch, 0f, Time.deltaTime * 5f);
            shield.localScale = Vector3.one * (1f + 0.3f * shieldPunch);
        }
    }

    /// <summary>Ba ngôi sao vàng quay trên đầu khi bị choáng do chiêu.</summary>
    void UpdateStunIcon()
    {
        bool show = stunnedUntil - Time.time > 0.45f;   // choáng ngắn khi trúng đòn thường thì không hiện
        if (show && stunIcon == null)
        {
            stunIcon = new GameObject("StunIcon").transform;
            stunIcon.SetParent(transform, false);
            stunIcon.gameObject.AddComponent<Billboard>().full = true;
            for (int i = 0; i < 3; i++)
            {
                var star = ZodiacFx.Shape("Star", true, Vector3.zero, Vector2.one * 0.18f, new Color(1f, 0.9f, 0.2f), 22, stunIcon);
                float a = i * Mathf.PI * 2f / 3f;
                star.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.35f, Mathf.Sin(a) * 0.12f, 0f);
            }
        }
        if (stunIcon == null) return;
        stunIcon.gameObject.SetActive(show);
        if (show)
        {
            Bounds b = body.bounds;
            stunIcon.position = new Vector3(b.center.x, b.max.y + 0.25f, b.center.z);
            for (int i = 0; i < stunIcon.childCount; i++)
            {
                float a = Time.time * 6f + i * Mathf.PI * 2f / 3f;
                stunIcon.GetChild(i).localPosition = new Vector3(Mathf.Cos(a) * 0.35f, Mathf.Sin(a) * 0.12f, 0f);
            }
        }
    }

    // ───────── Tiện ích ─────────

    protected bool IsGrounded()
    {
        Bounds b = body.bounds;
        return Physics.Raycast(new Vector3(b.center.x, b.min.y + 0.1f, b.center.z), Vector3.down, 0.25f, groundLayer,
                               QueryTriggerInteraction.Ignore);
    }

    /// <summary>Có vật cản (tường, nhà, thùng) ngay phía trước theo hướng cho trước trên mặt đường.</summary>
    protected bool BlockedAhead(Vector3 dir, float distance = 0.4f)
    {
        Bounds b = body.bounds;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return false;
        return Physics.Raycast(new Vector3(b.center.x, b.min.y + 0.4f, b.center.z), dir.normalized, b.extents.x + distance,
                               groundLayer, QueryTriggerInteraction.Ignore);
    }
}
