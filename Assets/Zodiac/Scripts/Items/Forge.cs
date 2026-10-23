using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Luật Lò rèn: đập đồ trang bị +0 → +10, phân giải đồ thừa ra nguyên liệu.
///   • Tỉ lệ: +1…+3 chắc chắn, rồi giảm dần còn 12% ở +10. Mỗi lần hỏng liên tiếp cộng ẩn +5% cho món đó (đập được thì xoá).
///   • Hỏng khi lên +1…+5: giữ nguyên cấp. Hỏng khi lên +6 trở lên: tụt 1 cấp — trừ khi dùng <b>Bùa bảo hộ</b>.
///   • <b>Ngọc may mắn</b>: +10% tỉ lệ lần này.
///   • Tốn: đồng + nguyên liệu theo ô đồ (vũ khí: Mảnh gốm · giáp: Vảy bọ gai · bùa / ngọc: Lửa ma trơi), từ +6 thêm Đá thạch sư.
/// Món đồ được chỉ định theo vị trí (túi hoặc ô trang bị) nên đập cả đồ đang mặc được.
/// </summary>
public static class Forge
{
    public const int MaxLevel = 10;
    public const string CharmId = "bua_bao_ho";
    public const string LuckId = "ngoc_may_man";

    static readonly float[] Rates = { 1f, 1f, 1f, 0.85f, 0.7f, 0.55f, 0.4f, 0.3f, 0.2f, 0.12f };
    const float PityPerFail = 0.05f;
    const float LuckBonus = 0.1f;

    public enum Result { Success, FailKeep, FailDrop, Protected, NotEnough, MaxLevel, Invalid }

    public struct Cost
    {
        public int coins;
        public List<(string id, int count)> materials;
    }

    /// <summary>Tỉ lệ thành công khi đập lên cấp kế (đã tính cộng dồn do hỏng, chưa tính Ngọc may mắn).</summary>
    public static float Chance(ItemStack s)
    {
        if (s == null || s.level >= MaxLevel) return 0f;
        return Mathf.Clamp01(Rates[s.level] + PityPerFail * s.failStreak);
    }

    /// <summary>Hỏng khi đập lên cấp kế có bị tụt cấp không.</summary>
    public static bool DropsOnFail(ItemStack s) => s.level + 1 >= 6;

    // ───────── Khảm ngọc ─────────

    public const int SocketCost = 20, UnsocketCost = 40;

    /// <summary>Số ô khảm: vũ khí / giáp — thường, tốt 1 ô; hiếm 2; sử thi trở lên 3; đập tới +7 thêm 1 ô (tối đa 3).</summary>
    public static int SocketsOf(ItemStack s)
    {
        var def = s?.Def;
        if (def == null || def.type != ItemType.Equipment || (def.slot != EquipSlot.Weapon && def.slot != EquipSlot.Armor)) return 0;
        int n = def.rarity <= ItemRarity.Good ? 1 : def.rarity == ItemRarity.Rare ? 2 : 3;
        if (s.level >= 7) n++;
        return Mathf.Min(3, n);
    }

    static ItemStack Target(PlayerProfile p, bool equipped, int index) =>
        index < 0 ? null : equipped ? (index < p.equipped.Length ? p.equipped[index] : null) : (index < p.bag.Length ? p.bag[index] : null);

    /// <summary>Khảm viên ngọc ở ô túi gemBagIndex vào món đồ (tốn đồng, viên ngọc mất khỏi túi).</summary>
    public static bool Socket(PlayerProfile p, bool equipped, int index, int gemBagIndex)
    {
        var s = Target(p, equipped, index);
        if (s == null || s.IsEmpty || gemBagIndex < 0 || gemBagIndex >= p.bag.Length) return false;
        if (!equipped && gemBagIndex == index) return false;
        var gem = p.bag[gemBagIndex];
        var gemDef = gem.Def;
        if (gemDef == null || gemDef.type != ItemType.Equipment || gemDef.slot != EquipSlot.Gem) return false;
        s.gems ??= new List<string>();
        if (s.gems.Count >= SocketsOf(s) || p.coins < SocketCost) return false;
        p.coins -= SocketCost;
        s.gems.Add(gem.id);
        p.RemoveAt(gemBagIndex);   // RemoveAt đã Commit
        return true;
    }

    /// <summary>Tháo viên ngọc thứ gemSlot trả về túi (tốn đồng, cần ô trống).</summary>
    public static bool Unsocket(PlayerProfile p, bool equipped, int index, int gemSlot)
    {
        var s = Target(p, equipped, index);
        if (s == null || s.IsEmpty || s.gems == null || gemSlot < 0 || gemSlot >= s.gems.Count) return false;
        if (p.coins < UnsocketCost || p.FreeSlots == 0) return false;
        string id = s.gems[gemSlot];
        p.coins -= UnsocketCost;
        s.gems.RemoveAt(gemSlot);
        p.Add(new ItemStack { id = id, count = 1 });   // Add đã Commit
        return true;
    }

    static string MaterialFor(EquipSlot slot) => slot switch
    {
        EquipSlot.Weapon => "manh_gom",
        EquipSlot.Armor => "vay_bo_gai",
        _ => "lua_ma_troi",
    };

    public static Cost CostOf(ItemStack s)
    {
        int target = s.level + 1;
        var cost = new Cost { coins = 10 * target, materials = new List<(string, int)> { (MaterialFor(s.Def.slot), target) } };
        if (target >= 6) cost.materials.Add(("da_thach_su", target - 4));
        return cost;
    }

    public static bool CanAfford(PlayerProfile p, Cost c)
    {
        if (p.coins < c.coins) return false;
        foreach (var (id, count) in c.materials) if (p.CountOf(id) < count) return false;
        return true;
    }

    /// <summary>Đập món ở vị trí (equipped = ô trang bị, không thì ô túi).</summary>
    public static Result Upgrade(PlayerProfile p, bool equipped, int index, bool useCharm, bool useLuck)
    {
        var s = equipped ? p.equipped[index] : p.bag[index];
        var def = s.Def;
        if (def == null || def.type != ItemType.Equipment) return Result.Invalid;
        if (s.level >= MaxLevel) return Result.MaxLevel;
        var cost = CostOf(s);
        if (!CanAfford(p, cost)) return Result.NotEnough;
        useCharm &= DropsOnFail(s) && p.CountOf(CharmId) > 0;
        useLuck &= p.CountOf(LuckId) > 0;

        p.coins -= cost.coins;
        foreach (var (id, count) in cost.materials) p.Consume(id, count);
        if (useCharm) p.Consume(CharmId, 1);
        if (useLuck) p.Consume(LuckId, 1);

        // Consume có thể dồn lại túi: lấy lại món theo vị trí (đồ trang bị không bị dời chỗ khi tiêu nguyên liệu)
        s = equipped ? p.equipped[index] : p.bag[index];
        float chance = Chance(s) + (useLuck ? LuckBonus : 0f);
        Result result;
        if (Random.value < chance)
        {
            s.level++;
            s.failStreak = 0;
            result = Result.Success;
        }
        else
        {
            s.failStreak++;
            if (!DropsOnFail(s)) result = Result.FailKeep;
            else if (useCharm) result = Result.Protected;
            else
            {
                s.level = Mathf.Max(0, s.level - 1);
                result = Result.FailDrop;
                ReturnGems(p, s, SocketsOf(s));   // tụt dưới +7 mất một ô khảm: ngọc thừa về túi
            }
        }
        p.Commit();
        return result;
    }

    /// <summary>Nguyên liệu + đồng nhận được khi phân giải món (cấp càng cao trả càng nhiều).</summary>
    public static (string id, int count, int coins) SalvageOf(ItemStack s)
    {
        var def = s.Def;
        return (MaterialFor(def.slot), 1 + (int)def.rarity + s.level, def.price / 4 + s.level * 5);
    }

    /// <summary>Phân giải món trong túi ra nguyên liệu + đồng.</summary>
    public static bool Salvage(PlayerProfile p, int bagIndex)
    {
        var s = p.bag[bagIndex];
        var def = s.Def;
        if (def == null || def.type != ItemType.Equipment) return false;
        var (id, count, coins) = SalvageOf(s);
        ReturnGems(p, s, 0);
        p.bag[bagIndex] = new ItemStack();
        p.coins += coins;
        p.Add(ItemDatabase.Get(id), count);   // Add đã Commit
        return true;
    }

    /// <summary>Trả các viên ngọc vượt quá keep ô về túi (bán / phân giải: keep = 0). Túi đầy thì ngọc rơi xuống đất.</summary>
    public static void ReturnGems(PlayerProfile p, ItemStack s, int keep)
    {
        if (s?.gems == null) return;
        while (s.gems.Count > keep)
        {
            string id = s.gems[s.gems.Count - 1];
            s.gems.RemoveAt(s.gems.Count - 1);
            var left = p.Add(new ItemStack { id = id, count = 1 });
            var player = ZodiacPlayerController.Current;
            if (left > 0 && player != null) LootPickup.Spawn(new ItemStack { id = id, count = 1 }, 0, player.transform.position);
        }
    }

    /// <summary>Giá bán cho tiệm (nửa giá, đồ đập cao bán đắt hơn).</summary>
    public static int SellPrice(ItemStack s) => Mathf.Max(1, s.Def.price / 2 + s.level * 10);
}
