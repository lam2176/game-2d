using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quái tinh anh: to hơn, máu ×2.2, có vòng hào quang màu dưới chân và tên tiền tố trên đầu, mang một đặc tính:
///   • Cuồng nộ — chạy nhanh ×1.45, đánh đau hơn, choáng ngắn.
///   • Giáp đá — +80 giáp, khó bị đẩy lùi.
///   • Độc — đòn trúng gây độc (3 lần trừ máu).
///   • Hồi máu — không bị đánh 2 giây thì hồi 4% máu mỗi giây.
///   • Phân thân — chết thì tách làm 2 con nhỏ.
/// Rơi gấp đôi xu, có thêm cơ hội rơi Bùa bảo hộ / Ngọc may mắn / mảnh Đan; XP ×3.
/// <see cref="EncounterZone"/> tung tinh anh lúc sinh quái theo <see cref="ZodiacDifficulty.EliteChance"/>.
/// </summary>
public class EliteAffix : MonoBehaviour
{
    public enum Kind { Frenzy, Stone, Venom, Regen, Split }

    public static readonly string[] Names = { "Cuồng nộ", "Giáp đá", "Độc", "Hồi máu", "Phân thân" };
    public static readonly Color[] Colors =
    {
        new Color(1f, 0.35f, 0.2f), new Color(0.75f, 0.75f, 0.8f), new Color(0.45f, 1f, 0.35f),
        new Color(1f, 0.55f, 0.75f), new Color(0.6f, 0.5f, 1f),
    };

    static readonly string[] BonusDrops = { "bua_bao_ho", "ngoc_may_man", "manh_huyet_dan", "manh_khi_dan" };

    public Kind kind;
    /// <summary>Prefab gốc (để phân thân).</summary>
    public GameObject source;
    /// <summary>Báo khu đang đánh có thêm quái (phân thân) để khu chưa coi là dọn xong.</summary>
    public Action<Health> spawned;

    EnemyBase enemy;
    Health health;
    SpriteRenderer aura;
    float lastHurt, regenCarry;

    /// <summary>Biến quái vừa sinh thành tinh anh.</summary>
    public static EliteAffix Make(EnemyBase enemy, Kind kind, GameObject source, Action<Health> spawned)
    {
        var e = enemy.gameObject.AddComponent<EliteAffix>();
        e.kind = kind;
        e.source = source;
        e.spawned = spawned;
        e.Setup(enemy);
        return e;
    }

    public static Kind RandomKind() => (Kind)UnityEngine.Random.Range(0, Names.Length);

    void Setup(EnemyBase e)
    {
        enemy = e;
        health = e.Health;
        enemy.Elite = this;
        enemy.ApplyScaling(2.2f, kind == Kind.Frenzy ? 1.25f : 1.1f, 3f);
        switch (kind)
        {
            case Kind.Frenzy:
                enemy.speedScale *= 1.45f;
                enemy.stunTime *= 0.5f;
                break;
            case Kind.Stone:
                health.defense += 80;
                enemy.knockbackTaken *= 0.4f;
                break;
        }
        health.Damaged += _ => lastHurt = Time.time;
        health.Died += OnDied;
        enemy.transform.localScale *= 1.18f;
        BuildLook();
    }

    void BuildLook()
    {
        Color c = Colors[(int)kind];
        // Vòng hào quang nằm phẳng dưới chân
        aura = ZodiacFx.Shape("EliteAura", true, transform.position + Vector3.up * 0.04f, Vector2.one * 1.6f, new Color(c.r, c.g, c.b, 0.55f), 2, transform);
        aura.sprite = ZodiacFx.RingSprite;
        aura.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        aura.transform.localPosition = Vector3.up * 0.04f;
        aura.transform.localScale = Vector3.one * 1.6f / transform.localScale.x;

        // Tên tinh anh trên đầu (canvas nhỏ trong thế giới, quay về camera)
        var body = GetComponent<Collider>();
        float top = body != null ? body.bounds.max.y - transform.position.y + 0.55f : 2f;
        var label = new GameObject("EliteLabel", typeof(RectTransform));
        label.transform.SetParent(transform, false);
        label.transform.localPosition = Vector3.up * top / transform.localScale.y;
        label.transform.localScale = Vector3.one * 0.01f / transform.localScale.x;
        var canvas = label.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 40;
        ((RectTransform)label.transform).sizeDelta = new Vector2(400f, 60f);
        label.AddComponent<Billboard>().full = true;
        var t = ZodiacUi.MakeText(label.transform, $"Tinh anh · {Names[(int)kind]}", 34, c, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 60f));
        t.fontStyle = FontStyle.Bold;
    }

    void Update()
    {
        if (health == null || health.IsDead) return;
        if (aura != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
            aura.transform.localScale = Vector3.one * 1.6f * pulse / transform.localScale.x;
        }
        if (kind == Kind.Regen && Time.time > lastHurt + 2f && health.Current < health.maxHealth)
        {
            regenCarry += health.maxHealth * 0.04f * Time.deltaTime;
            int n = Mathf.FloorToInt(regenCarry);
            if (n > 0)
            {
                regenCarry -= n;
                health.Heal(n);
                if (UnityEngine.Random.value < 0.3f) ZodiacParticles.Sparkle(transform.position + Vector3.up * 0.8f, Colors[(int)Kind.Regen], 2);
            }
        }
    }

    /// <summary>Đòn của tinh anh vừa trúng người chơi.</summary>
    public void OnHitPlayer(Health target)
    {
        if (kind == Kind.Venom) Poison.Apply(target, 3, Mathf.Max(4, ZodiacBalance.Unit / 3));
    }

    /// <summary>Rơi thêm khi chết: một món hiếm (40%).</summary>
    public void DropBonus(Vector3 position)
    {
        if (UnityEngine.Random.value > 0.4f * ZodiacDifficulty.LootScale) return;
        var def = ItemDatabase.Get(BonusDrops[UnityEngine.Random.Range(0, BonusDrops.Length)]);
        if (def != null) LootPickup.Spawn(ItemAffixes.Roll(def), 0, position);
    }

    void OnDied()
    {
        if (kind != Kind.Split || source == null) return;
        for (int i = 0; i < 2; i++)
        {
            Vector3 p = transform.position + new Vector3(i == 0 ? -0.8f : 0.8f, 0.3f, UnityEngine.Random.Range(-0.4f, 0.4f));
            var go = Instantiate(source, p, Quaternion.identity);
            go.transform.localScale *= 0.75f;
            if (!go.TryGetComponent(out EnemyBase child)) continue;
            child.alwaysAggro = true;
            child.ApplyScaling(0.4f * ZodiacDifficulty.HealthScale, ZodiacDifficulty.DamageScale * 0.7f, 0.3f);
            ZodiacFx.Puff(p + Vector3.up * 0.5f, Colors[(int)Kind.Split], 1.2f);
            spawned?.Invoke(child.Health);
        }
    }
}
