using System;
using System.IO;
using UnityEngine;

/// <summary>Một ô đồ: mã vật phẩm + số lượng (id rỗng = ô trống).</summary>
[Serializable]
public class ItemStack
{
    public string id = "";
    public int count;

    public bool IsEmpty => string.IsNullOrEmpty(id) || count <= 0;
    public ItemDefinition Def => IsEmpty ? null : ItemDatabase.Get(id);

    public void Clear()
    {
        id = "";
        count = 0;
    }
}

/// <summary>
/// Hồ sơ người chơi giữ qua các màn: túi đồ (<see cref="BagSize"/> ô), 5 ô trang bị (vũ khí, giáp, bùa, 2 ngọc),
/// 2 ô dùng nhanh (phím 1, 2 — trỏ tới loại đồ dùng trong túi) và tiền đồng.
/// Lưu JSON ở Application.persistentDataPath/zodiac_profile.json mỗi khi thay đổi (ghi gộp cuối frame qua <see cref="Save"/>).
/// <see cref="PlayerEquipment"/> áp chỉ số đồ đang mặc vào CharacterStats; <see cref="InventoryWindow"/> là giao diện.
/// </summary>
[Serializable]
public class PlayerProfile
{
    public const int BagSize = 30;
    public const int QuickSlots = 2;
    /// <summary>Thứ tự ô trang bị.</summary>
    public static readonly EquipSlot[] EquipSlots = { EquipSlot.Weapon, EquipSlot.Armor, EquipSlot.Charm, EquipSlot.Gem, EquipSlot.Gem };

    public ItemStack[] bag = NewStacks(BagSize);
    public ItemStack[] equipped = NewStacks(5);
    public string[] quick = { "", "" };
    public int coins;

    static PlayerProfile current;

    /// <summary>Đồ thay đổi (thêm/bớt/mặc/tháo) — giao diện và chỉ số cập nhật theo.</summary>
    public static event Action Changed;
    /// <summary>Nhặt được đồ: (vật phẩm, số lượng) — để hiện thông báo.</summary>
    public static event Action<ItemDefinition, int> Picked;

    static string FilePath => Path.Combine(Application.persistentDataPath, "zodiac_profile.json");

    public static PlayerProfile Current
    {
        get
        {
            if (current != null) return current;
            try
            {
                if (File.Exists(FilePath)) current = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Không đọc được file lưu, tạo hồ sơ mới: " + e.Message);
            }
            current ??= NewProfile();
            current.Repair();
            return current;
        }
    }

    static ItemStack[] NewStacks(int n)
    {
        var a = new ItemStack[n];
        for (int i = 0; i < n; i++) a[i] = new ItemStack();
        return a;
    }

    /// <summary>Hồ sơ mới: 3 bình máu nhỏ gán sẵn vào phím 1.</summary>
    static PlayerProfile NewProfile()
    {
        var p = new PlayerProfile();
        p.bag[0] = new ItemStack { id = "binh_mau_nho", count = 3 };
        p.quick[0] = "binh_mau_nho";
        return p;
    }

    /// <summary>File lưu cũ / sửa tay: đủ số ô, bỏ đồ không còn tồn tại.</summary>
    void Repair()
    {
        if (bag == null || bag.Length != BagSize) Array.Resize(ref bag, BagSize);
        if (equipped == null || equipped.Length != EquipSlots.Length) Array.Resize(ref equipped, EquipSlots.Length);
        if (quick == null || quick.Length != QuickSlots) Array.Resize(ref quick, QuickSlots);
        for (int i = 0; i < bag.Length; i++)
        {
            bag[i] ??= new ItemStack();
            if (!bag[i].IsEmpty && bag[i].Def == null) bag[i].Clear();
        }
        for (int i = 0; i < equipped.Length; i++)
        {
            equipped[i] ??= new ItemStack();
            if (!equipped[i].IsEmpty && equipped[i].Def == null) equipped[i].Clear();
        }
        for (int i = 0; i < quick.Length; i++) quick[i] ??= "";
    }

    /// <summary>Bỏ hồ sơ đang có, bắt đầu lại từ đầu (chơi mới).</summary>
    public static void ResetProfile()
    {
        current = NewProfile();
        current.Save();
        Changed?.Invoke();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(this, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Không lưu được hồ sơ: " + e.Message);
        }
    }

    void Commit()
    {
        Save();
        Changed?.Invoke();
    }

    // ───────── Túi đồ ─────────

    /// <summary>Số lượng một loại đồ trong túi.</summary>
    public int CountOf(string id)
    {
        int n = 0;
        foreach (var s in bag) if (!s.IsEmpty && s.id == id) n += s.count;
        return n;
    }

    /// <summary>Thêm đồ: dồn vào chồng cùng loại trước, rồi ô trống. Trả về số lượng KHÔNG cất được (túi đầy).</summary>
    public int Add(ItemDefinition def, int count = 1)
    {
        if (def == null || count <= 0) return count;
        int left = count;
        if (def.Stackable)
            foreach (var s in bag)
            {
                if (left <= 0) break;
                if (s.IsEmpty || s.id != def.id || s.count >= def.maxStack) continue;
                int put = Mathf.Min(left, def.maxStack - s.count);
                s.count += put;
                left -= put;
            }
        foreach (var s in bag)
        {
            if (left <= 0) break;
            if (!s.IsEmpty) continue;
            int put = Mathf.Min(left, def.maxStack);
            s.id = def.id;
            s.count = put;
            left -= put;
        }
        if (left < count)
        {
            Picked?.Invoke(def, count - left);
            Commit();
        }
        return left;
    }

    /// <summary>Bớt count món ở ô túi index.</summary>
    public void RemoveAt(int index, int count = 1)
    {
        var s = bag[index];
        if (s.IsEmpty) return;
        s.count -= count;
        if (s.count <= 0) s.Clear();
        Commit();
    }

    /// <summary>Đổi chỗ hai ô túi (sắp xếp).</summary>
    public void Swap(int a, int b)
    {
        (bag[a], bag[b]) = (bag[b], bag[a]);
        Commit();
    }

    /// <summary>Gom đồ: dồn chồng cùng loại, xếp theo loại → độ hiếm → tên.</summary>
    public void Sort()
    {
        var all = new System.Collections.Generic.List<(ItemDefinition def, int count)>();
        foreach (var s in bag) if (!s.IsEmpty) all.Add((s.Def, s.count));
        foreach (var s in bag) s.Clear();
        all.Sort((x, y) =>
        {
            int c = x.def.type.CompareTo(y.def.type);
            if (c != 0) return c;
            c = y.def.rarity.CompareTo(x.def.rarity);
            return c != 0 ? c : string.CompareOrdinal(x.def.displayName, y.def.displayName);
        });
        var events = Picked;
        Picked = null;   // gom lại không phải "nhặt được"
        foreach (var (def, count) in all) AddSilently(def, count);
        Picked = events;
        Commit();
    }

    void AddSilently(ItemDefinition def, int count)
    {
        int left = count;
        foreach (var s in bag)
        {
            if (left <= 0) break;
            if (!s.IsEmpty && (s.id != def.id || s.count >= def.maxStack)) continue;
            if (s.IsEmpty) { s.id = def.id; s.count = 0; }
            int put = Mathf.Min(left, def.maxStack - s.count);
            s.count += put;
            left -= put;
        }
    }

    // ───────── Trang bị ─────────

    /// <summary>Mặc đồ ở ô túi index: vào ô đúng loại (ngọc: ô ngọc còn trống, không thì ô ngọc đầu); đồ đang mặc trả về túi.</summary>
    public bool Equip(int bagIndex)
    {
        var s = bag[bagIndex];
        var def = s.Def;
        if (def == null || def.type != ItemType.Equipment) return false;
        int target = -1;
        for (int i = 0; i < EquipSlots.Length; i++)
        {
            if (EquipSlots[i] != def.slot) continue;
            if (target < 0) target = i;
            if (equipped[i].IsEmpty) { target = i; break; }
        }
        if (target < 0) return false;
        var old = equipped[target];
        equipped[target] = new ItemStack { id = def.id, count = 1 };
        if (s.count > 1) s.count--;
        else s.Clear();
        if (!old.IsEmpty)
        {
            // trả đồ cũ vào đúng ô vừa trống (hoặc ô trống bất kỳ)
            if (bag[bagIndex].IsEmpty) bag[bagIndex] = old;
            else AddSilently(old.Def, 1);
        }
        Commit();
        return true;
    }

    /// <summary>Tháo đồ ở ô trang bị index về túi (túi đầy thì không tháo được).</summary>
    public bool Unequip(int equipIndex)
    {
        var e = equipped[equipIndex];
        if (e.IsEmpty) return false;
        int free = Array.FindIndex(bag, s => s.IsEmpty);
        if (free < 0) return false;
        bag[free] = new ItemStack { id = e.id, count = 1 };
        equipped[equipIndex] = new ItemStack();
        Commit();
        return true;
    }

    // ───────── Dùng đồ ─────────

    /// <summary>Gán loại đồ dùng ở ô túi vào ô dùng nhanh (phím 1 / 2).</summary>
    public void AssignQuick(int quickIndex, string id)
    {
        for (int i = 0; i < quick.Length; i++) if (quick[i] == id) quick[i] = "";
        quick[quickIndex] = id;
        Commit();
    }

    /// <summary>Dùng một món đồ dùng loại id cho người chơi (lấy từ chồng đầu tiên). Trả về false nếu hết hoặc không cần dùng.</summary>
    public bool Use(string id, Health health, PlayerEnergy energy)
    {
        int index = Array.FindIndex(bag, s => !s.IsEmpty && s.id == id);
        return index >= 0 && UseAt(index, health, energy);
    }

    public bool UseAt(int bagIndex, Health health, PlayerEnergy energy)
    {
        var def = bag[bagIndex].Def;
        if (def == null || def.type != ItemType.Consumable) return false;
        bool needHeal = def.heal > 0 && health != null && !health.IsDead && health.Current < health.maxHealth;
        bool needEnergy = def.energy > 0 && energy != null && energy.Current < energy.max;
        if (!needHeal && !needEnergy) return false;
        if (needHeal) health.Heal(def.heal);
        if (needEnergy) energy.Gain(def.energy);
        RemoveAt(bagIndex);
        return true;
    }

    // ───────── Tiền ─────────

    public void AddCoins(int amount)
    {
        if (amount == 0) return;
        coins = Mathf.Max(0, coins + amount);
        Commit();
    }
}
