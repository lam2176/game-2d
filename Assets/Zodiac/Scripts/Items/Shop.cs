using System.Collections.Generic;

/// <summary>
/// Tiệm thuốc bắc trong làng: bán đồ dùng + Bùa bảo hộ / Ngọc may mắn, mua lại đồ (nửa giá), và <b>luyện đan</b> —
/// ghép nguyên liệu quái rơi thành bình thuốc hoặc mảnh Đan.
/// </summary>
public static class Shop
{
    public struct Recipe
    {
        public string result;
        public int count;
        public (string id, int count)[] inputs;
    }

    /// <summary>Hàng bán: (mã vật phẩm, giá).</summary>
    public static readonly (string id, int price)[] Stock =
    {
        ("binh_mau_nho", 15),
        ("binh_mau_lon", 40),
        ("binh_khi", 20),
        (Forge.LuckId, 80),
        (Forge.CharmId, 150),
    };

    /// <summary>Quầy phụ kiện: bùa (ô Bùa) và ngọc (hai ô Ngọc) mặc lên người — (mã, giá).</summary>
    public static readonly (string id, int price)[] Accessories =
    {
        ("vong_bac", 45),
        ("ngoc_trai", 40),
        ("bua_ho_phu", 90),
        ("bua_ho_menh", 95),
        ("ngoc_do", 75),
        ("ngoc_lam", 75),
        ("ngoc_luc", 115),
        ("bua_khi", 160),
    };

    public static readonly Recipe[] Recipes =
    {
        new Recipe { result = "binh_mau_lon", count = 1, inputs = new[] { ("binh_mau_nho", 2), ("da_coc", 2) } },
        new Recipe { result = "binh_khi", count = 2, inputs = new[] { ("lua_ma_troi", 2) } },
        new Recipe { result = "manh_huyet_dan", count = 1, inputs = new[] { ("da_thach_su", 4), ("manh_gom", 6) } },
        new Recipe { result = "manh_khi_dan", count = 1, inputs = new[] { ("lua_ma_troi", 5), ("vay_bo_gai", 4) } },
        new Recipe { result = Forge.LuckId, count = 1, inputs = new[] { ("ngoc_lam", 1), ("lua_ma_troi", 3) } },
    };

    public static bool Buy(PlayerProfile p, string id, int price)
    {
        var def = ItemDatabase.Get(id);
        if (def == null || p.coins < price || p.FreeSlots == 0 && p.CountOf(id) == 0) return false;
        p.coins -= price;
        if (p.Add(def) > 0)
        {
            p.coins += price;   // túi đầy: hoàn tiền
            p.Commit();
            return false;
        }
        return true;
    }

    public static bool Sell(PlayerProfile p, int bagIndex)
    {
        var s = p.bag[bagIndex];
        if (s.IsEmpty) return false;
        p.coins += Forge.SellPrice(s);
        p.RemoveAt(bagIndex);   // RemoveAt đã Commit
        return true;
    }

    public static bool CanCraft(PlayerProfile p, Recipe r)
    {
        foreach (var (id, count) in r.inputs) if (p.CountOf(id) < count) return false;
        return true;
    }

    public static bool Craft(PlayerProfile p, Recipe r)
    {
        if (!CanCraft(p, r)) return false;
        foreach (var (id, count) in r.inputs) p.Consume(id, count);
        if (p.Add(ItemDatabase.Get(r.result), r.count) > 0)
        {
            // túi đầy: trả lại nguyên liệu
            foreach (var (id, count) in r.inputs) p.Add(ItemDatabase.Get(id), count);
            return false;
        }
        return true;
    }

    public static string Describe(IEnumerable<(string id, int count)> parts, PlayerProfile p)
    {
        var list = new List<string>();
        foreach (var (id, count) in parts)
        {
            var def = ItemDatabase.Get(id);
            int have = p.CountOf(id);
            string color = have >= count ? "#9fe39a" : "#ff8a7a";
            list.Add($"<color={color}>{(def != null ? def.displayName : id)} {have}/{count}</color>");
        }
        return string.Join(" · ", list);
    }
}
