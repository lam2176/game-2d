using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quái trùm giữa màn (mã "!" trước mã quái trong <c>ZodiacLevelData</c>, vd. "!W"): một quái thường được nâng thành tinh anh
/// mang tên riêng (Đội trưởng Đất Nung, Cóc Chúa…), to hơn, máu ×2.5 trên nền tinh anh, khó choáng, không túm được.
/// Có thanh máu riêng giữa trên màn hình; chết thì chắc chắn rơi mảnh Đan, nhiều xu và một món đồ.
/// </summary>
public class MiniBoss : MonoBehaviour
{
    static readonly Dictionary<string, string> Titles = new Dictionary<string, string>
    {
        { "Enemy_LinhDatNung", "Đội trưởng Đất Nung" },
        { "Enemy_LinhKhien", "Tướng Khiên Đồng" },
        { "Enemy_BoGaiLan", "Bọ Gai Chúa" },
        { "Enemy_MaTroi", "Ma Trơi Già" },
        { "Enemy_CocTia", "Cóc Chúa" },
        { "Enemy_ThachSu", "Sư Đá Canh Đền" },
    };

    static readonly string[] Rewards = { "bua_bao_ho", "ngoc_may_man", "binh_khi", "binh_mau_nho" };

    EnemyBase enemy;
    GameObject bar;
    RectTransform fill;

    public string Title { get; private set; }

    /// <summary>Biến quái vừa sinh thành trùm. Phân thân (tinh anh Phân thân) vẫn được báo về khu qua spawned.</summary>
    public static MiniBoss Make(EnemyBase enemy, GameObject source, Action<Health> spawned)
    {
        // Không lấy đặc tính Phân thân: trùm chết mà tách đôi thì mất cảm giác "hạ được trùm"
        var kind = (EliteAffix.Kind)UnityEngine.Random.Range(0, (int)EliteAffix.Kind.Split);
        var elite = enemy.GetComponent<EliteAffix>();
        if (elite == null) elite = EliteAffix.Make(enemy, kind, source, spawned);
        var m = enemy.gameObject.AddComponent<MiniBoss>();
        m.enemy = enemy;
        m.Title = Titles.TryGetValue(source != null ? source.name : "", out var t) ? t : "Trùm";
        if (elite.Label != null) elite.Label.text = $"{m.Title} · {EliteAffix.Names[(int)elite.kind]}";
        enemy.ApplyScaling(2.5f, 1.15f, 2f);
        enemy.transform.localScale *= 1.25f;
        enemy.knockbackTaken *= 0.5f;
        enemy.stunMultiplier *= 0.6f;
        enemy.grabImmune = true;
        enemy.Health.Died += m.OnDied;
        enemy.Health.Changed += m.Refresh;
        m.BuildBar();
        return m;
    }

    void OnDestroy()
    {
        if (bar != null) Destroy(bar);
    }

    void Refresh()
    {
        if (fill == null) return;
        var h = enemy.Health;
        fill.localScale = new Vector3(Mathf.Clamp01((float)h.Current / Mathf.Max(1, h.maxHealth)), 1f, 1f);
    }

    void OnDied()
    {
        Vector3 p = transform.position + Vector3.up * 0.5f;
        var shard = ItemDatabase.Get(UnityEngine.Random.value < 0.5f ? "manh_huyet_dan" : "manh_khi_dan");
        if (shard != null) LootPickup.Spawn(ItemAffixes.Roll(shard), 0, p);
        var reward = ItemDatabase.Get(Rewards[UnityEngine.Random.Range(0, Rewards.Length)]);
        if (reward != null) LootPickup.Spawn(ItemAffixes.Roll(reward), 0, p + Vector3.right * 0.6f);
        LootPickup.DropFrom(null, new Vector2Int(12, 20), p, ZodiacDifficulty.LootScale);
        PickupToast.Show($"Hạ {Title}!", new Color(1f, 0.82f, 0.3f));
        GameFeel.Shake(0.3f);
        if (bar != null) Destroy(bar);
    }

    void BuildBar()
    {
        var canvas = ZodiacUi.Canvas(null, "MiniBossBar", 90);
        bar = canvas.gameObject;
        var back = ZodiacUi.MakeImage(canvas, "Back", new Color(0.15f, 0.1f, 0.1f, 0.85f));
        back.raycastTarget = false;
        ZodiacUi.Place(back.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(700f, 28f));
        var f = ZodiacUi.MakeImage(back.transform, "Fill", new Color(1f, 0.55f, 0.2f));
        f.raycastTarget = false;
        fill = f.rectTransform;
        ZodiacUi.Stretch(fill);
        fill.pivot = new Vector2(0f, 0.5f);
        fill.offsetMin = new Vector2(3f, 3f);
        fill.offsetMax = new Vector2(-3f, -3f);
        ZodiacUi.MakeText(back.transform, Title, 28, new Color(1f, 0.85f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, 20f), new Vector2(700f, 40f));
        Refresh();
    }
}
