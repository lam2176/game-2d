using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đạn dùng chung cho chiêu người chơi (sóng âm, cầu lửa, chũm chọe) và đạn quái (lửa hồ ly, đạn tượng đá).
/// Không dùng Rigidbody: tự dời bằng transform và quét Physics2D.OverlapCircle mỗi bước vật lý.
/// Mỗi mục tiêu chỉ trúng một lần; pierce quyết định xuyên được bao nhiêu mục tiêu.
/// Boomerang: bay hết nửa tầm (hoặc chạm tường) thì quay về chủ.
/// </summary>
public class Projectile : MonoBehaviour
{
    /// <summary>Mọi đạn đang bay — chiêu Kèn Hiệu dùng để xoá đạn địch.</summary>
    public static readonly List<Projectile> Active = new List<Projectile>();

    public Vector2 velocity;
    public int damage = ZodiacBalance.Unit;
    public float radius = 0.3f;
    public float range = 10f;
    [Tooltip("-1 = xuyên tất cả.")]
    public int pierce;
    public bool boomerang;
    public bool passWalls;
    public float knockback = 1f;
    public float stun;
    public LayerMask targetLayers;
    public LayerMask wallLayers;
    [Tooltip("Đạn của quái (bị chiêu xoá đạn ảnh hưởng).")]
    public bool hostile;
    public Transform owner;
    [Tooltip("Chỉ số người bắn (đạn người chơi): mỗi lần trúng tung chí mạng riêng. Null = sát thương cố định.")]
    public CharacterStats attacker;
    public float spin;

    readonly HashSet<Health> hits = new HashSet<Health>();
    readonly List<Collider2D> overlaps = new List<Collider2D>();
    float travelled;
    bool returning;

    /// <summary>Tạo đạn hình tròn (có lõi sáng) tại vị trí cho trước.</summary>
    public static Projectile Spawn(Vector2 position, Vector2 velocity, float size, Color color, LayerMask targets,
                                   LayerMask walls, int damage, bool hostile)
    {
        var sr = ZodiacFx.Shape(hostile ? "EnemyShot" : "PlayerShot", true, position, Vector2.one * size, color, 16);
        var core = ZodiacFx.Shape("Core", true, position, Vector2.one * 0.5f, new Color(1f, 1f, 0.9f, 0.9f), 17, sr.transform);
        core.transform.localScale = Vector3.one * 0.5f;
        // Đạn tự phát sáng soi địa hình xung quanh (cần URP 2D Renderer)
        ZodiacLights.AddPoint(sr.gameObject, color, 1.6f + size, 0.9f);
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
        Vector2 pos = transform.position;

        if (returning && owner != null)
        {
            Vector2 toOwner = (Vector2)owner.position + Vector2.up - pos;
            if (toOwner.magnitude < 0.6f) { Destroy(gameObject); return; }
            velocity = toOwner.normalized * velocity.magnitude;
        }

        Vector2 step = velocity * dt;
        if (!passWalls && !returning &&
            Physics2D.CircleCast(pos, radius * 0.5f, step.normalized, step.magnitude, wallLayers))
        {
            if (boomerang) { returning = true; hits.Clear(); }
            else { Pop(); return; }
        }

        transform.position = pos + step;
        if (spin != 0f) transform.Rotate(0f, 0f, spin * dt);
        travelled += step.magnitude;

        if (boomerang && !returning && travelled >= range * 0.5f) { returning = true; hits.Clear(); }
        else if (!boomerang && travelled >= range) { Pop(); return; }
        else if (boomerang && travelled >= range * 3f) { Destroy(gameObject); return; }

        DetectHits();
    }

    void DetectHits()
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(targetLayers);
        Physics2D.OverlapCircle(transform.position, radius, filter, overlaps);
        foreach (var col in overlaps)
        {
            var target = col.GetComponentInParent<Health>();
            if (target == null || target.IsDead || !hits.Add(target)) continue;
            var hit = CharacterStats.CreateHit(attacker, damage, transform.position - (Vector3)velocity.normalized, knockback, stun);
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
