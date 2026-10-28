using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đạn dùng chung cho chiêu người chơi (sóng âm, cầu lửa, chũm chọe) và đạn quái (lửa hồ ly, đạn tượng đá, sóng chấn).
/// Không dùng Rigidbody: tự dời bằng transform trong không gian 3D và quét Physics.OverlapSphere mỗi bước vật lý.
/// Mỗi mục tiêu chỉ trúng một lần; pierce quyết định xuyên được bao nhiêu mục tiêu.
/// Boomerang: bay hết nửa tầm (hoặc chạm tường) thì quay về chủ.
/// Sóng sát đất (<see cref="groundWave"/>): chỉ trúng mục tiêu có chân thấp hơn <see cref="groundWaveHeight"/> — nhảy qua để né.
/// Đạn bay ngang qua mọi làn nên muốn né thì đổi làn (↑ ↓) hoặc nhảy.
/// </summary>
public class Projectile : MonoBehaviour
{
    /// <summary>Mọi đạn đang bay — chiêu Kèn Hiệu dùng để xoá đạn địch.</summary>
    public static readonly List<Projectile> Active = new List<Projectile>();

    public Vector3 velocity;
    public int damage = ZodiacBalance.Unit;
    public float radius = 0.3f;
    public float range = 10f;
    [Tooltip("-1 = xuyên tất cả.")]
    public int pierce;
    public bool boomerang;
    public bool passWalls;
    public float knockback = 1f;
    public float stun;
    public bool knockdown;
    [Tooltip("Sóng chạy sát mặt đất: mục tiêu đang nhảy cao hơn groundWaveHeight thì không trúng.")]
    public bool groundWave;
    public float groundWaveHeight = 0.5f;
    public LayerMask targetLayers;
    public LayerMask wallLayers;
    [Tooltip("Đạn của quái (bị chiêu xoá đạn ảnh hưởng).")]
    public bool hostile;
    public Transform owner;
    [Tooltip("Chỉ số người bắn (đạn người chơi): mỗi lần trúng tung chí mạng riêng. Null = sát thương cố định.")]
    public CharacterStats attacker;
    public float spin;
    [Tooltip("Đuổi theo mục tiêu này (chưởng của người chơi). Null = bay thẳng.")]
    public Collider homing;
    [Tooltip("Tốc độ bẻ hướng khi đuổi (radian/giây).")]
    public float homingTurn = 10f;

    readonly HashSet<Health> hits = new HashSet<Health>();
    readonly Collider[] overlaps = new Collider[16];
    float travelled;
    bool returning;

    /// <summary>Tạo đạn hình tròn (có lõi sáng, tự phát sáng soi mặt đất) tại vị trí cho trước.</summary>
    public static Projectile Spawn(Vector3 position, Vector3 velocity, float size, Color color, LayerMask targets,
                                   LayerMask walls, int damage, bool hostile)
    {
        var sr = ZodiacFx.Shape(hostile ? "EnemyShot" : "PlayerShot", true, position, Vector2.one * size, color, 16);
        var core = ZodiacFx.Shape("Core", true, position, Vector2.one * 0.5f, new Color(1f, 1f, 0.9f, 0.9f), 17, sr.transform);
        core.transform.localScale = Vector3.one * 0.5f;
        core.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        ZodiacLights.AddPoint(sr.gameObject, color, 2.5f + size * 2f, 1.5f);
        var p = sr.gameObject.AddComponent<Projectile>();
        p.velocity = velocity;
        p.radius = size * 0.45f;
        p.targetLayers = targets;
        p.wallLayers = walls;
        p.damage = damage;
        p.hostile = hostile;
        return p;
    }

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 pos = transform.position;

        if (returning && owner != null)
        {
            Vector3 toOwner = owner.position + Vector3.up - pos;
            if (toOwner.magnitude < 0.6f) { Destroy(gameObject); return; }
            velocity = toOwner.normalized * velocity.magnitude;
        }

        if (homing != null && homing.enabled && !returning)
        {
            Vector3 desired = homing.bounds.center - pos;
            if (desired.sqrMagnitude > 0.0001f)
                velocity = Vector3.RotateTowards(velocity, desired.normalized * velocity.magnitude, homingTurn * dt, 0f);
        }

        Vector3 step = velocity * dt;
        if (!passWalls && !returning && step.sqrMagnitude > 0f &&
            Physics.SphereCast(pos, radius * 0.5f, step.normalized, out _, step.magnitude, wallLayers, QueryTriggerInteraction.Ignore))
        {
            if (boomerang) { returning = true; hits.Clear(); }
            else { Pop(); return; }
        }

        transform.position = pos + step;
        if (spin != 0f) transform.Rotate(0f, 0f, spin * dt, Space.Self);
        travelled += step.magnitude;

        if (boomerang && !returning && travelled >= range * 0.5f) { returning = true; hits.Clear(); }
        else if (!boomerang && travelled >= range) { Pop(); return; }
        else if (boomerang && travelled >= range * 3f) { Destroy(gameObject); return; }

        DetectHits();
    }

    void DetectHits()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, overlaps, targetLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var col = overlaps[i];
            var target = col.GetComponentInParent<Health>();
            if (target == null || target.IsDead || hits.Contains(target)) continue;
            if (groundWave && target.transform.position.y > transform.position.y - radius + groundWaveHeight) continue;   // nhảy qua
            hits.Add(target);
            var hit = CharacterStats.CreateHit(attacker, damage, transform.position - velocity.normalized, knockback, stun);
            hit.knockdown = knockdown;
            if (target.TakeDamage(hit) && !hostile)
            {
                var sr = GetComponent<SpriteRenderer>();
                ZodiacParticles.Sparks(col.ClosestPoint(transform.position), sr != null ? sr.color : Color.white, 7);
                var res = ZodiacResources.Get;
                if (res != null) ZodiacAudio.Play(res.hit, 0.6f, 0.1f, transform.position);
            }
            if (pierce >= 0 && hits.Count > pierce)
            {
                if (boomerang) { returning = true; continue; }
                Pop();
                return;
            }
        }
    }

    /// <summary>Đạn tan (chạm tường, hết tầm, bị xoá).</summary>
    public void Pop()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) ZodiacFx.Puff(transform.position, sr.color, transform.localScale.x);
        Destroy(gameObject);
    }
}
