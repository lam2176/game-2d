using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chợ phiên trong làng (mở sau khi qua màn 3): thương nhân bày 6 món ngẫu nhiên — 4 đồ trang bị đã tung dòng phụ
/// (có món đã đập sẵn +1…+3) và 2 nguyên liệu quý. Mỗi lần về làng là một phiên mới (<see cref="NewVisit"/>);
/// hàng của phiên sinh lại được từ <see cref="PlayerProfile.marketSeed"/> nên mở / đóng cửa sổ không đổi hàng.
/// </summary>
public static class Market
{
    /// <summary>Đã mở tới màn 4 (qua Rừng Trúc).</summary>
    public const int UnlockLevels = 4;

    public struct Offer
    {
        public ItemStack item;
        public int price;
    }

    static readonly string[] Gear =
    {
        "kiem_tre", "giao_dong", "ao_giap_tre", "khien_dong", "vong_bac", "bua_ho_phu", "bua_ho_menh", "bua_khi",
        "ngoc_trai", "ho_phach", "ngoc_do", "ngoc_lam", "ngoc_luc", "ngoc_bich",
    };

    static readonly (string id, int min, int max)[] Goods =
    {
        ("manh_huyet_dan", 1, 1), ("manh_khi_dan", 1, 1), (Forge.CharmId, 1, 2), (Forge.LuckId, 1, 3),
        ("da_thach_su", 3, 6), ("lua_ma_troi", 4, 8), ("da_coc", 4, 8), ("vay_bo_gai", 4, 8),
    };

    static Offer[] cache;
    static int cacheSeed;

    public static bool IsUnlocked(PlayerProfile p) => p.unlockedLevels >= UnlockLevels;

    /// <summary>Về làng: thương nhân bày một phiên hàng mới.</summary>
    public static void NewVisit(PlayerProfile p)
    {
        p.marketSeed = Random.Range(1, int.MaxValue);
        p.marketSold.Clear();
        p.Save();
    }

    public static Offer[] Offers(PlayerProfile p)
    {
        if (p.marketSeed == 0) NewVisit(p);
        if (cache != null && cacheSeed == p.marketSeed) return cache;
        var saved = Random.state;
        Random.InitState(p.marketSeed);
        var list = new List<Offer>();
        var gear = new List<string>(Gear);
        while (list.Count < 4 && gear.Count > 0)
        {
            int k = Random.Range(0, gear.Count);
            var def = ItemDatabase.Get(gear[k]);
            gear.RemoveAt(k);
            if (def == null) continue;
            var stack = ItemAffixes.Roll(def);
            float r = Random.value;
            stack.level = r < 0.1f ? 3 : r < 0.3f ? 2 : r < 0.6f ? 1 : 0;
            int price = Mathf.RoundToInt(def.price * (1.5f + 0.25f * stack.affixes.Count + 0.4f * stack.level) / 5f) * 5;
            list.Add(new Offer { item = stack, price = Mathf.Max(20, price) });
        }
        var goods = new List<(string id, int min, int max)>(Goods);
        while (list.Count < 6 && goods.Count > 0)
        {
            int k = Random.Range(0, goods.Count);
            var (id, min, max) = goods[k];
            goods.RemoveAt(k);
            var def = ItemDatabase.Get(id);
            if (def == null) continue;
            int count = Random.Range(min, max + 1);
            int price = Mathf.Max(10, Mathf.RoundToInt(def.price * count * 1.8f / 5f) * 5);
            list.Add(new Offer { item = new ItemStack { id = id, count = count }, price = price });
        }
        Random.state = saved;
        cacheSeed = p.marketSeed;
        cache = list.ToArray();
        return cache;
    }

    public static bool IsSold(PlayerProfile p, int index) => p.marketSold.Contains(index);

    /// <summary>Mua món thứ index của phiên này (mỗi món chỉ một lần). Túi đầy hoặc thiếu đồng thì không mua.</summary>
    public static bool Buy(PlayerProfile p, int index)
    {
        var offers = Offers(p);
        if (index < 0 || index >= offers.Length || IsSold(p, index)) return false;
        var o = offers[index];
        if (p.coins < o.price || p.FreeSlots == 0) return false;
        p.coins -= o.price;
        if (p.Add(o.item.Clone()) > 0)
        {
            p.coins += o.price;   // túi đầy: hoàn tiền (Add không cất gì vì món trang bị chiếm cả ô)
            p.Commit();
            return false;
        }
        p.marketSold.Add(index);
        p.Commit();
        return true;
    }
}
