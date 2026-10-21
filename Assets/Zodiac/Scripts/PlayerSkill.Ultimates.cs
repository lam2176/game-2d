using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tuyệt kỹ riêng của 12 con giáp — mỗi con một chiêu khác hẳn (không còn là bản phóng to của chiêu thường).
/// Tra theo tên asset nhân vật (<see cref="Moves"/>); con nào chưa có thì quay về <see cref="SkillConfig.Empowered"/>.
/// Sát thương tính theo <see cref="ZodiacBalance.Unit"/> × hệ số công (trang bị, tu luyện).
/// Phạm vi "cả khu" = mọi quái còn sống trong khu đang đánh.
/// </summary>
public partial class PlayerSkill
{
    static readonly Dictionary<string, Func<PlayerSkill, IEnumerator>> Moves = new Dictionary<string, Func<PlayerSkill, IEnumerator>>
    {
        { "Rat_Ty", s => s.RatShadows() },
        { "Ox_Suu", s => s.OxBell() },
        { "Tiger_Dan", s => s.TigerLeap() },
        { "Cat_Mao", s => s.CatNineLives() },
        { "Dragon_Thin", s => s.DragonBreath() },
        { "Snake_Ty", s => s.SnakeDrain() },
        { "Horse_Ngo", s => s.HorseCharge() },
        { "Goat_Mui", s => s.GoatSpring() },
        { "Monkey_Than", s => s.MonkeyClones() },
        { "Rooster_Dau", s => s.RoosterDawn() },
        { "Dog_Tuat", s => s.DogHowl() },
        { "Pig_Hoi", s => s.PigQuake() },
    };

    /// <summary>Con giáp này có tuyệt kỹ riêng.</summary>
    bool HasUniqueUltimate => controller.Character != null && Moves.ContainsKey(controller.Character.name);

    void RunUniqueUltimate(SkillConfig cfg)
    {
        readyTime = float.PositiveInfinity;
        hitThisSkill.Clear();
        if (controller.Character != null) ZodiacAudio.Play(controller.Character.skillSound, 1f, 0.03f);
        Used?.Invoke(cfg.type);
        StartCoroutine(RunMove(Moves[controller.Character.name](this), cfg));
    }

    IEnumerator RunMove(IEnumerator move, SkillConfig cfg)
    {
        IsBusy = true;
        yield return move;
        if (controller != null) controller.SetOverride(false);
        Finish(cfg);
    }

    // ───────── Tiện ích ─────────

    /// <summary>Quái còn sống trong khu đang đánh (không có khu thì trong 10 đơn vị quanh người chơi).</summary>
    List<EnemyBase> RoomEnemies()
    {
        var list = new List<EnemyBase>();
        float min = ZodiacGameManager.RoomMinX, max = ZodiacGameManager.RoomMaxX;
        bool noRoom = min < -999f;
        foreach (var e in FindObjectsByType<EnemyBase>())
        {
            if (!e.isActiveAndEnabled || e.Health.IsDead) continue;
            float x = e.transform.position.x;
            if (noRoom ? Mathf.Abs(x - transform.position.x) > 10f : x < min - 0.5f || x > max + 0.5f) continue;
            list.Add(e);
        }
        return list;
    }

    /// <summary>Đánh một quái: units × Unit × hệ số công.</summary>
    bool Strike(Health target, float units, Vector3 source, float knockback = 1f, float stun = 0f, bool knockdown = false)
    {
        if (target == null || target.IsDead) return false;
        var hit = CharacterStats.CreateHit(Stats, ZodiacBalance.Unit * units * AttackScale, source, knockback, stun);
        hit.knockdown = knockdown;
        if (!target.TakeDamage(hit)) return false;
        var col = target.GetComponent<Collider>();
        GameFeel.Impact(col != null ? col.bounds.center : target.transform.position + Vector3.up, Theme, knockdown || hit.crit);
        return true;
    }

    /// <summary>Đánh mọi quái trong bán kính (trên mặt đường) quanh tâm.</summary>
    int StrikeRadius(Vector3 center, float radius, float units, float knockback, float stun, bool knockdown)
    {
        int n = 0;
        foreach (var e in RoomEnemies())
        {
            Vector3 d = e.transform.position - center;
            d.y = 0f;
            if (d.sqrMagnitude <= radius * radius && Strike(e.Health, units, center, knockback, stun, knockdown)) n++;
        }
        return n;
    }

    /// <summary>Chỉ số cộng thêm trong một lúc (tăng công, giáp).</summary>
    IEnumerator TimedBuff(StatModifier modifier, float seconds)
    {
        if (Stats == null) yield break;
        var source = new object();
        Stats.AddModifier(modifier, source);
        yield return new WaitForSeconds(seconds);
        if (Stats != null) Stats.RemoveModifiersFrom(source);
    }

    void Buff(StatType stat, ModifierKind kind, float value, float seconds, string toast)
    {
        StartCoroutine(TimedBuff(new StatModifier(stat, kind, value), seconds));
        if (!string.IsNullOrEmpty(toast)) PickupToast.Show(toast, Theme);
    }

    void FlatRing(Vector3 center, float radius, Color color, float duration = 0.45f) =>
        ZodiacFx.Ring(new Vector3(center.x, transform.position.y, center.z), radius, color, duration, flat: true);

    Color Alpha(float a) => new Color(Theme.r, Theme.g, Theme.b, a);

    // ───────── 12 tuyệt kỹ ─────────

    /// <summary>Chuột — Vạn Ảnh Thử: chớp tới từng quái trong khu chém một nhát, nhát cuối quật ngã cả đám.</summary>
    IEnumerator RatShadows()
    {
        var targets = RoomEnemies();
        targets.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        if (targets.Count > 8) targets.RemoveRange(8, targets.Count - 8);
        controller.SetOverride(true, 0f);
        health?.SetInvulnerable(0.4f + targets.Count * 0.12f);
        foreach (var e in targets)
        {
            if (e == null || e.Health.IsDead) continue;
            ZodiacFx.Afterimage(controller.spriteRenderer, Alpha(0.6f));
            float side = transform.position.x <= e.transform.position.x ? -1f : 1f;
            Vector3 to = new Vector3(ZodiacGameManager.RoomMinX > -999f
                                         ? Mathf.Clamp(e.transform.position.x + side * 0.9f, ZodiacGameManager.RoomMinX + 0.4f, ZodiacGameManager.RoomMaxX - 0.4f)
                                         : e.transform.position.x + side * 0.9f,
                                     transform.position.y, e.transform.position.z);
            controller.Teleport(to);
            controller.SetFacing(-side);
            Strike(e.Health, 2f, to, 0.4f, 0.5f);
            yield return new WaitForSeconds(0.11f);
        }
        foreach (var e in targets)
            if (e != null) Strike(e.Health, 1f, transform.position, 1.2f, 0f, true);
        ZodiacParticles.Sparkle(BodyCenter, Theme, 20, 1.5f);
    }

    /// <summary>Trâu — Đại Hồng Chung: ba hồi chuông, mỗi hồi một vòng sóng chấn rộng; giáp +100 trong 6 giây.</summary>
    IEnumerator OxBell()
    {
        controller.SetOverride(true, controller.gravityScale);
        Buff(StatType.Defense, ModifierKind.Flat, 100f, 6f, "Đại Hồng Chung: +100 giáp trong 6 giây");
        for (int i = 0; i < 3; i++)
        {
            bool last = i == 2;
            FlatRing(transform.position, 6f, Alpha(0.7f));
            ZodiacFx.Ring(BodyCenter + Vector3.up * 1.2f, 1.4f, new Color(1f, 0.85f, 0.4f, 0.8f), 0.35f);
            GameFeel.Shake(0.3f);
            StrikeRadius(transform.position, 6f, last ? 2f : 1.2f, last ? 1.5f : 0.3f, 0.8f, last);
            yield return new WaitForSeconds(0.38f);
        }
    }

    /// <summary>Hổ — Mãnh Hổ Hạ Sơn: vồ lên cao, đáp xuống giữa đám quái đông nhất, rồi gầm: choáng cả khu, công +30% 8 giây.</summary>
    IEnumerator TigerLeap()
    {
        var enemies = RoomEnemies();
        Vector3 start = transform.position, land = start + Vector3.right * controller.FacingDirection * 3f;
        int bestCount = -1;
        foreach (var e in enemies)
        {
            int n = 0;
            foreach (var o in enemies) if ((o.transform.position - e.transform.position).sqrMagnitude < 9f) n++;
            if (n > bestCount) { bestCount = n; land = e.transform.position; }
        }
        land = new Vector3(land.x, start.y, Mathf.Clamp(land.z, ZodiacGameManager.LaneMinZ + 0.3f, ZodiacGameManager.LaneMaxZ - 0.3f));
        if (Mathf.Abs(land.x - start.x) > 0.1f) controller.SetFacing(land.x - start.x);
        controller.SetOverride(true, 0f);
        health?.SetInvulnerable(0.8f);
        const float time = 0.45f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;
            controller.Teleport(Vector3.Lerp(start, land, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 3f);
            if (Time.frameCount % 2 == 0) ZodiacFx.Afterimage(controller.spriteRenderer, Alpha(0.5f));
            yield return null;
        }
        controller.Teleport(land);
        GameFeel.HitStop(0.06f);
        GameFeel.Shake(0.5f);
        ZodiacFx.Puff(land + Vector3.up * 0.3f, new Color(0.7f, 0.55f, 0.4f, 0.8f), 3f);
        FlatRing(land, 3.5f, Alpha(0.8f));
        StrikeRadius(land, 3.5f, 4f, 1.5f, 0f, true);
        yield return new WaitForSeconds(0.25f);
        FlatRing(land, 9f, new Color(1f, 0.6f, 0.2f, 0.5f), 0.6f);
        foreach (var e in RoomEnemies()) e.Stun(1.5f);
        Buff(StatType.Attack, ModifierKind.Percent, 0.3f, 8f, "Mãnh Hổ: +30% công trong 8 giây");
    }

    /// <summary>Mèo — Cửu Mệnh Huyền Âm: hồi 30% máu, thêm một mạng giữ lại, tiếng nhị làm cả khu choáng 2.5 giây.</summary>
    IEnumerator CatNineLives()
    {
        controller.SetOverride(true, controller.gravityScale);
        if (health != null)
        {
            health.Heal(Mathf.RoundToInt(health.maxHealth * 0.3f));
            health.lastStandCharges = Mathf.Min(health.lastStandCharges + 1, 9);
        }
        ZodiacParticles.Sparkle(BodyCenter, new Color(0.8f, 0.6f, 1f), 24, 1f);
        for (int i = 0; i < 3; i++)
        {
            FlatRing(transform.position, 3f + i * 3f, new Color(0.75f, 0.55f, 1f, 0.6f));
            yield return new WaitForSeconds(0.12f);
        }
        foreach (var e in RoomEnemies()) Strike(e.Health, 2f, transform.position, 0.3f, 2.5f);
        PickupToast.Show("Cửu Mệnh: thêm một mạng", new Color(0.8f, 0.6f, 1f));
        yield return new WaitForSeconds(0.2f);
    }

    /// <summary>Rồng — Long Thần Hoả: đứng phun lửa 1.6 giây, chùm cầu lửa xuyên thấu toả theo chiều sâu.</summary>
    IEnumerator DragonBreath()
    {
        controller.SetOverride(true, controller.gravityScale);
        float d = controller.FacingDirection;
        var fire = new Color(1f, 0.5f, 0.15f);
        for (int i = 0; i < 22 && controller.ControlEnabled; i++)
        {
            float a = Mathf.Sin(i * 0.9f) * 24f;
            Vector3 dir = Quaternion.Euler(0f, a * d, 0f) * new Vector3(d, 0f, 0f);
            Vector3 origin = BodyCenter + new Vector3(d * 0.6f, 0.2f, 0f);
            var shot = Projectile.Spawn(origin, dir * 13f, 0.55f, fire, targetLayers, groundLayer,
                                        Mathf.RoundToInt(ZodiacBalance.Unit * AttackScale), false);
            shot.attacker = Stats;
            shot.range = 10f;
            shot.pierce = -1;   // xuyên tất cả
            shot.owner = transform;
            shot.knockback = 0.4f;
            if (i % 4 == 0) GameFeel.Shake(0.1f);
            yield return new WaitForSeconds(0.07f);
        }
    }

    /// <summary>Rắn — Vạn Xà Ẩn Hình: quấn con khoẻ nhất hút máu 2 giây (hồi cho mình), cả khu nhiễm độc.</summary>
    IEnumerator SnakeDrain()
    {
        controller.SetOverride(true, controller.gravityScale);
        var enemies = RoomEnemies();
        EnemyBase victim = null;
        foreach (var e in enemies)
        {
            Poison.Apply(e.Health, 5);
            if (victim == null || e.Health.Current > victim.Health.Current) victim = e;
        }
        if (victim == null) yield break;
        health?.SetInvulnerable(2.2f);
        var green = new Color(0.4f, 1f, 0.5f);
        for (int i = 0; i < 8 && victim != null && !victim.Health.IsDead; i++)
        {
            victim.Stun(0.4f);
            int before = victim.Health.Current;
            Strike(victim.Health, 1.2f, victim.transform.position, 0f);
            int dealt = before - victim.Health.Current;
            if (health != null && dealt > 0) health.Heal(Mathf.Max(1, dealt / 2));
            Vector3 a = victim.transform.position + Vector3.up, b = BodyCenter;
            for (int k = 0; k < 5; k++) ZodiacParticles.Sparkle(Vector3.Lerp(a, b, k / 4f), green, 2, 0.1f);
            yield return new WaitForSeconds(0.25f);
        }
    }

    /// <summary>Ngựa — Thiên Mã Hành Không: phi qua lại hết khu ba lượt, húc mọi quái trên đường, lượt cuối quật ngã.</summary>
    IEnumerator HorseCharge()
    {
        var rb = controller.Body;
        controller.SetOverride(true, 0f);
        health?.SetInvulnerable(1.6f);
        float min = ZodiacGameManager.RoomMinX > -999f ? ZodiacGameManager.RoomMinX + 0.8f : transform.position.x - 8f;
        float max = ZodiacGameManager.RoomMaxX < 999f ? ZodiacGameManager.RoomMaxX - 0.8f : transform.position.x + 8f;
        float dir = controller.FacingDirection;
        for (int pass = 0; pass < 3; pass++)
        {
            hitThisSkill.Clear();
            controller.SetFacing(dir);
            bool last = pass == 2;
            for (float t = 0f; t < 0.6f; t += Time.fixedDeltaTime)
            {
                rb.linearVelocity = new Vector3(dir * 26f, 0f, 0f);
                if (Time.frameCount % 2 == 0) ZodiacFx.Afterimage(controller.spriteRenderer, Alpha(0.5f));
                foreach (var e in RoomEnemies())
                {
                    Vector3 d = e.transform.position - transform.position;
                    if (Mathf.Abs(d.x) < 1.1f && Mathf.Abs(d.z) < 1.2f && hitThisSkill.Add(e.Health))
                        Strike(e.Health, last ? 2.5f : 1.5f, transform.position - Vector3.right * dir, 1.2f, 0.3f, last);
                }
                yield return new WaitForFixedUpdate();
                if ((dir > 0f && transform.position.x >= max) || (dir < 0f && transform.position.x <= min)) break;
            }
            rb.linearVelocity = Vector3.zero;
            dir = -dir;
            yield return new WaitForSeconds(0.05f);
        }
    }

    /// <summary>Dê — Xuân Hồi Đại Địa: hồi đầy máu, +60 giáp và hồi máu dần 10 giây, đẩy lùi quái quanh mình.</summary>
    IEnumerator GoatSpring()
    {
        controller.SetOverride(true, controller.gravityScale);
        var green = new Color(0.5f, 1f, 0.6f);
        health?.Heal(health.maxHealth);
        ZodiacParticles.Sparkle(BodyCenter, green, 30, 1.2f);
        FlatRing(transform.position, 4f, new Color(0.4f, 1f, 0.5f, 0.7f));
        StrikeRadius(transform.position, 4f, 0.5f, 2f, 0.6f, false);
        Buff(StatType.Defense, ModifierKind.Flat, 60f, 10f, "Xuân Hồi: +60 giáp, hồi máu dần 10 giây");
        StartCoroutine(Regenerate(10f, 0.02f, green));
        yield return new WaitForSeconds(0.35f);
    }

    IEnumerator Regenerate(float seconds, float perHalfSecond, Color color)
    {
        for (float t = 0f; t < seconds && health != null && !health.IsDead; t += 0.5f)
        {
            if (health.Heal(Mathf.Max(1, Mathf.RoundToInt(health.maxHealth * perHalfSecond))) > 0)
                ZodiacParticles.Sparkle(BodyCenter, color, 3, 0.5f);
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>Khỉ — Tề Thiên Đại Thánh: nhổ lông thổi ra 3 phân thân vàng tự đánh quái 8 giây.</summary>
    IEnumerator MonkeyClones()
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = transform.position + new Vector3((i - 1) * 1.2f, 0f, (i - 1) * 0.6f);
            ZodiacFx.Puff(p + Vector3.up * 0.8f, new Color(1f, 0.85f, 0.3f, 0.8f), 1.4f);
            StartCoroutine(Clone(p, 8f, i * 0.2f));
        }
        yield return new WaitForSeconds(0.15f);   // khỉ chơi tiếp ngay, phân thân tự đánh
    }

    IEnumerator Clone(Vector3 position, float lifetime, float delay)
    {
        var src = controller.spriteRenderer;
        var go = new GameObject("MonkeyClone");
        go.transform.position = position;
        var tilt = new GameObject("Tilt").transform;
        tilt.SetParent(go.transform, false);
        tilt.gameObject.AddComponent<Billboard>();
        var sr = new GameObject("Sprite").AddComponent<SpriteRenderer>();
        sr.transform.SetParent(tilt, false);
        sr.transform.localPosition = src.transform.parent != null ? src.transform.localPosition : Vector3.zero;
        sr.transform.localScale = src.transform.lossyScale;
        sr.sprite = src.sprite;
        sr.sharedMaterial = src.sharedMaterial;
        sr.color = new Color(1f, 0.85f, 0.4f, 0.8f);
        float nextStrike = Time.time + delay;
        float end = Time.time + lifetime;
        while (Time.time < end && this != null && health != null && !health.IsDead)
        {
            sr.sprite = src.sprite;
            EnemyBase target = null;
            float best = float.MaxValue;
            foreach (var e in RoomEnemies())
            {
                float d = (e.transform.position - go.transform.position).sqrMagnitude;
                if (d < best) { best = d; target = e; }
            }
            if (target != null)
            {
                Vector3 d = target.transform.position - go.transform.position;
                d.y = 0f;
                sr.flipX = d.x < 0f;
                if (d.magnitude > 1.1f) go.transform.position += d.normalized * 7f * Time.deltaTime;
                else if (Time.time >= nextStrike)
                {
                    nextStrike = Time.time + 0.5f;
                    Strike(target.Health, 0.8f, go.transform.position, 0.6f);
                    ZodiacFx.Ring(go.transform.position + Vector3.up * 0.8f + Vector3.right * Mathf.Sign(d.x) * 0.6f, 0.6f, new Color(1f, 0.85f, 0.3f, 0.7f), 0.15f);
                }
            }
            yield return null;
        }
        ZodiacFx.Puff(go.transform.position + Vector3.up * 0.8f, new Color(1f, 0.85f, 0.3f, 0.8f), 1.2f);
        Destroy(go);
    }

    /// <summary>Gà — Kim Kê Báo Hiệu: cả khu bừng sáng — choáng 3.5 giây, xoá đạn, quái nhận thêm 50% sát thương 6 giây.</summary>
    IEnumerator RoosterDawn()
    {
        controller.SetOverride(true, controller.gravityScale);
        ScreenFader.Flash(new Color(1f, 0.95f, 0.7f, 0.6f), 0.5f);
        FlatRing(transform.position, 10f, new Color(1f, 0.85f, 0.3f, 0.6f), 0.7f);
        foreach (var e in RoomEnemies())
        {
            e.Stun(3.5f);
            e.Health.MakeVulnerable(6f, 1.5f);
            ZodiacParticles.Sparkle(e.transform.position + Vector3.up, new Color(1f, 0.9f, 0.4f), 6, 0.4f);
        }
        for (int i = Projectile.Active.Count - 1; i >= 0; i--)
            if (Projectile.Active[i].hostile) Projectile.Active[i].Pop();
        PickupToast.Show("Bình minh: quái nhận thêm 50% sát thương", new Color(1f, 0.85f, 0.3f));
        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>Chó — Thiên Cẩu Hống: tiếng hống quật ngã cả khu, công +25% trong 8 giây.</summary>
    IEnumerator DogHowl()
    {
        controller.SetOverride(true, controller.gravityScale);
        for (int i = 0; i < 2; i++)
        {
            FlatRing(transform.position, 5f + i * 4f, Alpha(0.6f));
            GameFeel.Shake(0.3f);
            yield return new WaitForSeconds(0.12f);
        }
        foreach (var e in RoomEnemies()) Strike(e.Health, 2.5f, transform.position, 2f, 0f, true);
        Buff(StatType.Attack, ModifierKind.Percent, 0.25f, 8f, "Thiên Cẩu: +25% công trong 8 giây");
    }

    /// <summary>Heo — Bát Giới Trấn Sơn: hút cả khu về trước mặt rồi dậm đất, sát thương lớn và quật ngã.</summary>
    IEnumerator PigQuake()
    {
        controller.SetOverride(true, controller.gravityScale);
        var enemies = RoomEnemies();
        Vector3 center = transform.position + Vector3.right * controller.FacingDirection * 1.6f;
        foreach (var e in enemies) e.Stun(1.2f);
        for (float t = 0f; t < 0.55f; t += Time.fixedDeltaTime)
        {
            foreach (var e in enemies)
            {
                if (e == null || e.Health.IsDead || !e.TryGetComponent(out Rigidbody erb) || erb.isKinematic) continue;
                Vector3 d = center - e.transform.position;
                d.y = 0f;
                erb.linearVelocity = new Vector3(d.x * 7f, erb.linearVelocity.y, d.z * 7f);
            }
            if (Time.frameCount % 3 == 0) FlatRing(center, 3f, Alpha(0.4f), 0.2f);
            yield return new WaitForFixedUpdate();
        }
        var rb = controller.Body;
        for (float t = 0f; t < 0.2f; t += Time.fixedDeltaTime)
        {
            rb.linearVelocity = new Vector3(0f, 9f, 0f);
            yield return new WaitForFixedUpdate();
        }
        for (float t = 0f; t < 1f && !controller.IsGrounded; t += Time.fixedDeltaTime)
        {
            rb.linearVelocity = new Vector3(0f, -22f, 0f);
            yield return new WaitForFixedUpdate();
        }
        rb.linearVelocity = Vector3.zero;
        Vector3 feet = transform.position + Vector3.up * 0.3f;
        ZodiacFx.Puff(feet, new Color(0.7f, 0.55f, 0.4f, 0.8f), 3f);
        ZodiacParticles.Debris(feet, new Color(0.55f, 0.42f, 0.3f), 16, 1.2f);
        GameFeel.HitStop(0.08f);
        GameFeel.Shake(0.6f);
        FlatRing(transform.position, 4f, Alpha(0.8f));
        StrikeRadius(transform.position, 4f, 4.5f, 1.5f, 0f, true);
    }
}
