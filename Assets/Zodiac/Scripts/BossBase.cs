using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Nền chung của boss cuối khu (<see cref="EncounterZone.boss"/>): tên hiện trên thanh máu, đứng im tới khi
/// <see cref="Activate"/>, không bị quật ngã / túm, không dùng lượt vây đánh. Kèm các tiện ích ra đòn có báo hiệu:
/// ném vòng cung có vòng báo chỗ rơi (<see cref="Lob"/>), vùng nổ báo trước (<see cref="Blast"/>), gọi lính (<see cref="Summon"/>).
/// </summary>
public abstract class BossBase : EnemyBase
{
    public string bossName = "Boss";

    /// <summary>Đã vào trận (sau <see cref="Activate"/>).</summary>
    public abstract bool IsActive { get; }

    /// <summary>Người chơi bước vào đấu trường — boss gầm lên và bắt đầu đánh.</summary>
    public abstract void Activate();

    protected override void Awake()
    {
        base.Awake();
        usesAttackToken = false;
        canBeKnockedDown = false;
        grabImmune = true;
    }

    /// <summary>Sát thương đòn đánh trực tiếp (HitPlayer tự nhân độ khó).</summary>
    protected int UnitDamage(float units) => Mathf.RoundToInt(ZodiacBalance.Unit * units);
    /// <summary>Sát thương đạn / sóng (Projectile không tự nhân độ khó).</summary>
    protected int ShotDamage(float units) => Mathf.RoundToInt(ZodiacBalance.Unit * units * damageScale);

    protected void Roar(float shake = 0.4f)
    {
        GameFeel.Shake(shake);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.bossRoar, 1f, 0f, transform.position);
    }

    /// <summary>Chỗ người chơi đang đứng (trên mặt đường, trong khu).</summary>
    protected Vector3 PlayerGround()
    {
        Vector3 p = player != null ? player.transform.position : transform.position;
        return new Vector3(ClampRoom(p.x), transform.position.y, ClampLane(p.z));
    }

    /// <summary>Người chơi còn sát đất trong bán kính quanh điểm (nhảy lên thì tránh được đòn sát đất).</summary>
    protected bool PlayerInside(Vector3 center, float radius, bool groundOnly = true)
    {
        if (!PlayerAvailable) return false;
        Vector3 p = player.transform.position;
        if (groundOnly && !player.IsGrounded) return false;
        return new Vector2(p.x - center.x, p.z - center.z).sqrMagnitude <= radius * radius;
    }

    /// <summary>
    /// Ném một vật theo đường vòng cung từ <paramref name="from"/> tới <paramref name="to"/> trong <paramref name="time"/> giây;
    /// vòng báo hiện ở chỗ rơi suốt lúc bay. Rơi xuống thì gọi onLand(chỗ rơi).
    /// </summary>
    protected void Lob(Vector3 from, Vector3 to, float time, float size, Color color, float markRadius, Action<Vector3> onLand) =>
        StartCoroutine(LobRoutine(from, to, time, size, color, markRadius, onLand));

    IEnumerator LobRoutine(Vector3 from, Vector3 to, float time, float size, Color color, float markRadius, Action<Vector3> onLand)
    {
        var shot = ZodiacFx.Shape("BossLob", true, from, Vector2.one * size, color, 16);
        ZodiacFx.Ring(to + Vector3.up * 0.03f, markRadius * 2f, new Color(color.r, color.g, color.b, 0.8f), time, flat: true);
        float height = Mathf.Max(2.5f, Vector3.Distance(from, to) * 0.45f);
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;
            if (shot != null)
            {
                shot.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * (4f * height * k * (1f - k));
                shot.transform.Rotate(0f, 0f, 540f * Time.deltaTime);
            }
            yield return null;
        }
        if (shot != null) Destroy(shot.gameObject);
        if (!health.IsDead) onLand?.Invoke(to);
    }

    /// <summary>Vùng nổ: vòng báo <paramref name="warn"/> giây rồi đánh người chơi đứng trong vùng (sát đất).</summary>
    protected void Blast(Vector3 center, float radius, float warn, int damage, Color color, bool knockdown = false, Action onBlast = null) =>
        StartCoroutine(BlastRoutine(center, radius, warn, damage, color, knockdown, onBlast));

    IEnumerator BlastRoutine(Vector3 center, float radius, float warn, int damage, Color color, bool knockdown, Action onBlast)
    {
        if (warn > 0f)
        {
            ZodiacFx.Ring(center + Vector3.up * 0.03f, radius * 2f, new Color(color.r, color.g, color.b, 0.75f), warn, flat: true);
            yield return new WaitForSeconds(warn);
        }
        if (health.IsDead) yield break;
        Impact(center, radius, damage, color, knockdown);
        onBlast?.Invoke();
    }

    /// <summary>Đòn chạm đất tức thì: bụi, rung, trừ máu người chơi trong vùng.</summary>
    protected void Impact(Vector3 center, float radius, int damage, Color color, bool knockdown = false, bool groundOnly = true)
    {
        ZodiacFx.Ring(center, radius, color, 0.3f, flat: true);
        ZodiacParticles.Debris(center + Vector3.up * 0.3f, DebrisColor, 8, radius * 0.5f);
        ZodiacParticles.Dust(center + Vector3.up * 0.2f, Vector3.left, 6);
        ZodiacParticles.Dust(center + Vector3.up * 0.2f, Vector3.right, 6);
        GameFeel.Shake(0.25f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.bossSlam, 0.8f, 0.08f, center);
        if (PlayerInside(center, radius, groundOnly))
            HitPlayer(new Hit(damage, center, 1.4f) { knockdown = knockdown });
    }

    /// <summary>Gọi lính ra hai bên boss (có vòng khói báo), tính vào khu để phải hạ hết.</summary>
    protected void Summon(GameObject prefab, int count, Color color)
    {
        if (prefab == null) return;
        var zone = ZodiacGameManager.Instance != null ? ZodiacGameManager.Instance.Room : null;
        for (int i = 0; i < count; i++)
        {
            Vector3 at = transform.position + new Vector3((i % 2 == 0 ? -1f : 1f) * (2.5f + i), 0.05f, i % 2 == 0 ? 1f : -1f);
            at.z = ClampLane(at.z);
            at.x = ClampRoom(at.x);
            var go = Instantiate(prefab, at, Quaternion.identity);
            if (go.TryGetComponent(out EnemyBase e))
            {
                e.alwaysAggro = true;
                e.ApplyScaling(ZodiacDifficulty.HealthScale * 0.6f, ZodiacDifficulty.DamageScale, 0.3f);
                if (zone != null) zone.Track(e.Health);
            }
            ZodiacFx.Ring(at, 1.2f, color, 0.3f, flat: true);
            ZodiacFx.Puff(at + Vector3.up * 0.6f, color, 1.4f);
        }
    }

    /// <summary>Một đợt sóng sát đất chạy theo hướng (nhảy qua để tránh).</summary>
    protected void GroundWave(Vector3 from, Vector3 dir, float speed, float range, Color color, float scale = 1f)
    {
        var wave = Projectile.Spawn(from + Vector3.up * 0.35f, dir.normalized * speed, 0.7f, color, playerLayer, groundLayer, ShotDamage(1f), true);
        wave.range = range;
        wave.groundWave = true;
        wave.passWalls = true;
        wave.transform.localScale = new Vector3(0.6f, 0.9f, 1f) * scale;
    }
}
