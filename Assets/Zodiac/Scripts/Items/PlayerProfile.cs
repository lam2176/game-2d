using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Một ô đồ: mã vật phẩm + số lượng (id rỗng = ô trống). Đồ trang bị là từng món riêng (count = 1) mang theo
/// cấp đập (<see cref="level"/>, +0 → +10 ở Lò rèn) và các dòng chỉ số phụ tung ngẫu nhiên lúc rơi (<see cref="affixes"/>).
/// </summary>
[Serializable]
public class ItemStack
{
    public string id = "";
    public int count;
    [Tooltip("Cấp đập +0 → +10 (chỉ đồ trang bị).")]
    public int level;
    [Tooltip("Dòng chỉ số phụ ngẫu nhiên (chỉ đồ trang bị).")]
    public List<StatModifier> affixes = new List<StatModifier>();
    [Tooltip("Số lần đập hỏng liên tiếp (cộng tỉ lệ cho lần sau).")]
    public int failStreak;

    public bool IsEmpty => string.IsNullOrEmpty(id) || count <= 0;
    public ItemDefinition Def => IsEmpty ? null : ItemDatabase.Get(id);

    /// <summary>Mỗi cấp đập tăng 8% chỉ số gốc của món (+10 = gấp đôi).</summary>
    public const float LevelBonus = 0.08f;

    public string DisplayName
    {
        get
        {
            var def = Def;
            if (def == null) return "";
            return level > 0 ? $"{def.displayName} +{level}" : def.displayName;
        }
    }

    /// <summary>Chỉ số món này cộng cho người mặc: chỉ số gốc × (1 + 8% × cấp) + các dòng phụ.</summary>
    public IEnumerable<StatModifier> EffectiveModifiers()
    {
        var def = Def;
        if (def == null) yield break;
        float scale = 1f + LevelBonus * level;
        foreach (var m in def.modifiers) yield return new StatModifier(m.stat, m.kind, m.value * scale);
        if (affixes != null) foreach (var a in affixes) yield return a;
    }

    public ItemStack Clone() => new ItemStack
    {
        id = id, count = count, level = level, failStreak = failStreak,
        affixes = affixes != null ? new List<StatModifier>(affixes) : new List<StatModifier>(),
    };

    public void Clear()
    {
        id = "";
        count = 0;
        level = 0;
        failStreak = 0;
        affixes = new List<StatModifier>();
    }

    /// <summary>Hai chồng gộp được với nhau (cùng loại đồ chồng được, không có cấp / dòng phụ riêng).</summary>
    public bool StacksWith(ItemDefinition def) => !IsEmpty && id == def.id && def.Stackable;
}

/// <summary>Tiến trình riêng của một con giáp: cấp, XP, điểm tu luyện đã cộng, Huyết/Khí Đan đã luyện.</summary>
[Serializable]
public class CharacterProgress
{
    public const int MaxLevel = 30;
    public const int PointsPerLevel = 3;

    public string character = "";
    public int level = 1;
    public int xp;
    public int points;
    [Tooltip("Điểm đã cộng: Sinh lực, Nội khí, Sức mạnh, Thân pháp.")]
    public int vitality, spirit, strength, agility;
    [Tooltip("Số viên Huyết Đan / Khí Đan đã luyện (mỗi viên +20 máu / +20 Khí tối đa).")]
    public int bloodPills, energyPills;

    /// <summary>XP cần để lên cấp kế.</summary>
    public int XpToNext => level >= MaxLevel ? 0 : 40 + (level - 1) * 30;

    public const int PillBonus = 20;
}

/// <summary>Số quái đã hạ của một loại (sổ ghi chép) — id theo tên prefab, vd. "Enemy_LinhKhien".</summary>
[Serializable]
public class KillCount
{
    public string id = "";
    public int count;
}

/// <summary>
/// Hồ sơ người chơi giữ qua các màn: túi đồ (<see cref="BagSize"/> ô), 5 ô trang bị (vũ khí, giáp, bùa, 2 ngọc),
/// 2 ô dùng nhanh (phím 1, 2 — trỏ tới loại đồ dùng trong túi), tiền đồng, tiến trình từng con giáp và màn đã mở.
/// Lưu JSON ở Application.persistentDataPath/zodiac_profile.json mỗi khi thay đổi.
/// <see cref="PlayerEquipment"/> áp chỉ số đồ đang mặc + tu luyện vào CharacterStats; <see cref="InventoryWindow"/> là giao diện.
/// </summary>
[Serializable]
public class PlayerProfile
{
    /// <summary>2: đồ trang bị là từng món (cấp đập, dòng phụ), tiến trình nhân vật, màn đã mở. 3: tặng phụ kiện khởi đầu.</summary>
    public const int CurrentVersion = 3;
    public const int BagSize = 30;
    public const int QuickSlots = 2;
    /// <summary>Thứ tự ô trang bị.</summary>
    public static readonly EquipSlot[] EquipSlots = { EquipSlot.Weapon, EquipSlot.Armor, EquipSlot.Charm, EquipSlot.Gem, EquipSlot.Gem };

    [Tooltip("0 = file lưu cũ chưa có trường này (JsonUtility để mặc định) → Repair nâng cấp.")]
    public int version;
    public ItemStack[] bag = NewStacks(BagSize);
    public ItemStack[] equipped = NewStacks(5);
    public string[] quick = { "", "" };
    public int coins;
    [Tooltip("Số màn đã mở (1 = chỉ màn 1).")]
    public int unlockedLevels = 1;
    public List<CharacterProgress> characters = new List<CharacterProgress>();
    [Tooltip("Độ khó đang chọn (ZodiacDifficulty): 0 Thường, 1 Khó, 2 Ác Mộng.")]
    public int difficulty;
    [Tooltip("Đã phá đảo màn cuối → mở Ác Mộng.")]
    public bool nightmareUnlocked;
    [Tooltip("Đoạn truyện đã xem (tranh mở đầu, thoại từng khu theo con giáp).")]
    public List<string> storySeen = new List<string>();
    [Tooltip("Con giáp đã phá đảo (đã xem kết thúc riêng) — đủ 12 thì có kết thúc thật.")]
    public List<string> endingsSeen = new List<string>();
    [Tooltip("Số quái đã hạ theo loại — trang Sổ ghi chép.")]
    public List<KillCount> kills = new List<KillCount>();

    static PlayerProfile current;

    /// <summary>Đồ / tiến trình thay đổi — giao diện và chỉ số cập nhật theo.</summary>
    public static event Action Changed;
    /// <summary>Nhặt được đồ: (món, số lượng) — để hiện thông báo.</summary>
    public static event Action<ItemStack, int> Picked;
    /// <summary>Lên cấp: (tiến trình của con giáp vừa lên).</summary>
    public static event Action<CharacterProgress> LeveledUp;

    static string FilePath => Path.Combine(Application.persistentDataPath, "zodiac_profile.json");

    public static bool HasSave => File.Exists(FilePath);

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
        var p = new PlayerProfile { version = CurrentVersion };
        p.bag[0] = new ItemStack { id = "binh_mau_nho", count = 3 };
        p.quick[0] = "binh_mau_nho";
        if (!p.GiveStarterAccessories()) p.version = 2;   // chưa có dữ liệu đồ: tặng ở lần mở sau
        return p;
    }

    /// <summary>File lưu cũ / sửa tay: đủ số ô, bỏ đồ không còn tồn tại, nâng phiên bản.</summary>
    void Repair()
    {
        if (bag == null || bag.Length != BagSize) Array.Resize(ref bag, BagSize);
        if (equipped == null || equipped.Length != EquipSlots.Length) Array.Resize(ref equipped, EquipSlots.Length);
        if (quick == null || quick.Length != QuickSlots) Array.Resize(ref quick, QuickSlots);
        characters ??= new List<CharacterProgress>();
        storySeen ??= new List<string>();
        endingsSeen ??= new List<string>();
        kills ??= new List<KillCount>();
        foreach (var arr in new[] { bag, equipped })
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] ??= new ItemStack();
                arr[i].affixes ??= new List<StatModifier>();
                if (!arr[i].IsEmpty && arr[i].Def == null) arr[i].Clear();
            }
        for (int i = 0; i < quick.Length; i++) quick[i] ??= "";
        unlockedLevels = Mathf.Max(1, unlockedLevels);
        if (!ZodiacDifficulty.IsUnlocked(difficulty, this)) difficulty = ZodiacDifficulty.Normal;
        if (version < 2)
        {
            // Phiên bản 1: đồ trang bị có thể nằm chồng — tách thành từng món riêng
            var extra = new List<ItemStack>();
            foreach (var s in bag)
            {
                var def = s.Def;
                if (def == null || def.Stackable || s.count <= 1) continue;
                for (int k = 1; k < s.count; k++) extra.Add(new ItemStack { id = s.id, count = 1 });
                s.count = 1;
            }
            foreach (var e in extra) PlaceSilently(e);
        }
        bool starterOk = version >= 3 || GiveStarterAccessories();
        version = starterOk ? CurrentVersion : 2;
    }

    /// <summary>
    /// Mặc sẵn Vòng bạc trừ tà (ô Bùa) và Ngọc trai (ô Ngọc) nếu các ô đó đang trống — để ô phụ kiện có đồ dùng ngay.
    /// Trả về false nếu cơ sở dữ liệu đồ chưa có hai món này (chưa dựng lại tài nguyên).
    /// </summary>
    bool GiveStarterAccessories()
    {
        if (ItemDatabase.Get("vong_bac") == null || ItemDatabase.Get("ngoc_trai") == null) return false;
        (string id, EquipSlot slot)[] gifts = { ("vong_bac", EquipSlot.Charm), ("ngoc_trai", EquipSlot.Gem) };
        foreach (var (id, slot) in gifts)
        {
            int index = Array.IndexOf(EquipSlots, slot);
            if (index < 0) continue;
            if (equipped[index] == null || equipped[index].IsEmpty) equipped[index] = new ItemStack { id = id, count = 1 };
            else PlaceSilently(new ItemStack { id = id, count = 1 });
        }
        return true;
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

    /// <summary>Lưu và báo giao diện cập nhật.</summary>
    public void Commit()
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

    public int FreeSlots
    {
        get
        {
            int n = 0;
            foreach (var s in bag) if (s.IsEmpty) n++;
            return n;
        }
    }

    /// <summary>Thêm count món loại def (đồ trang bị: món trơn, không dòng phụ). Trả về số KHÔNG cất được.</summary>
    public int Add(ItemDefinition def, int count = 1)
    {
        if (def == null || count <= 0) return count;
        return Add(new ItemStack { id = def.id, count = count });
    }

    /// <summary>Thêm một chồng / một món (giữ cấp đập, dòng phụ). Trả về số lượng KHÔNG cất được (túi đầy).</summary>
    public int Add(ItemStack stack)
    {
        var def = stack.Def;
        if (def == null || stack.count <= 0) return stack.count;
        int before = stack.count;
        int left = PlaceSilently(stack.Clone());
        if (left < before)
        {
            var picked = stack.Clone();
            picked.count = before - left;
            Picked?.Invoke(picked, before - left);
            Commit();
        }
        return left;
    }

    /// <summary>Cất vào túi không báo sự kiện: dồn chồng cùng loại trước, rồi ô trống. Trả về số không cất được.</summary>
    int PlaceSilently(ItemStack stack)
    {
        var def = stack.Def;
        int left = stack.count;
        if (def.Stackable)
            foreach (var s in bag)
            {
                if (left <= 0) break;
                if (!s.StacksWith(def) || s.count >= def.maxStack) continue;
                int put = Mathf.Min(left, def.maxStack - s.count);
                s.count += put;
                left -= put;
            }
        for (int i = 0; i < bag.Length && left > 0; i++)
        {
            if (!bag[i].IsEmpty) continue;
            int put = Mathf.Min(left, def.maxStack);
            var placed = stack.Clone();
            placed.count = put;
            bag[i] = placed;
            left -= put;
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

    /// <summary>Bớt count món loại id ở bất kỳ ô nào. Trả về false (không bớt gì) nếu không đủ.</summary>
    public bool Consume(string id, int count)
    {
        if (CountOf(id) < count) return false;
        for (int i = 0; i < bag.Length && count > 0; i++)
        {
            var s = bag[i];
            if (s.IsEmpty || s.id != id) continue;
            int take = Mathf.Min(count, s.count);
            s.count -= take;
            count -= take;
            if (s.count <= 0) s.Clear();
        }
        Commit();
        return true;
    }

    /// <summary>Đổi chỗ hai ô túi (sắp xếp).</summary>
    public void Swap(int a, int b)
    {
        (bag[a], bag[b]) = (bag[b], bag[a]);
        Commit();
    }

    /// <summary>Gom đồ: dồn chồng cùng loại, xếp theo loại → độ hiếm → cấp đập → tên.</summary>
    public void Sort()
    {
        var all = new List<ItemStack>();
        foreach (var s in bag) if (!s.IsEmpty) all.Add(s.Clone());
        for (int i = 0; i < bag.Length; i++) bag[i] = new ItemStack();
        all.Sort((x, y) =>
        {
            var a = x.Def;
            var b = y.Def;
            int c = a.type.CompareTo(b.type);
            if (c != 0) return c;
            c = b.rarity.CompareTo(a.rarity);
            if (c != 0) return c;
            c = y.level.CompareTo(x.level);
            return c != 0 ? c : string.CompareOrdinal(a.displayName, b.displayName);
        });
        foreach (var s in all) PlaceSilently(s);
        Commit();
    }

    // ───────── Trang bị ─────────

    /// <summary>Mặc đồ ở ô túi index: vào ô đúng loại (ngọc: ô ngọc còn trống, không thì ô ngọc đầu); đồ đang mặc trả về đúng ô đó.</summary>
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
        equipped[target] = s.Clone();
        equipped[target].count = 1;
        bag[bagIndex] = old.IsEmpty ? new ItemStack() : old;   // đồ trang bị luôn là một món nên đổi chỗ thẳng
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
        bag[free] = e;
        equipped[equipIndex] = new ItemStack();
        Commit();
        return true;
    }

    // ───────── Dùng đồ ─────────

    /// <summary>Gán loại đồ dùng vào ô dùng nhanh (phím 1 / 2).</summary>
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

    // ───────── Tiền, màn ─────────

    public void AddCoins(int amount)
    {
        if (amount == 0) return;
        coins = Mathf.Max(0, coins + amount);
        Commit();
    }

    /// <summary>Thắng màn index (0 = màn 1) → mở màn kế; thắng màn cuối (final) → mở độ khó Ác Mộng.</summary>
    public void CompleteLevel(int index, bool final = false)
    {
        unlockedLevels = Mathf.Max(unlockedLevels, index + 2);
        if (final) nightmareUnlocked = true;
        Commit();
    }

    public bool HasSeen(string key) => storySeen.Contains(key);

    /// <summary>Đánh dấu đã xem một đoạn truyện (lưu ngay).</summary>
    public void MarkSeen(string key)
    {
        if (string.IsNullOrEmpty(key) || storySeen.Contains(key)) return;
        storySeen.Add(key);
        Commit();
    }

    /// <summary>Ghi nhận hạ một quái (không lưu ngay — lần cộng XP / nhặt đồ kế tiếp sẽ lưu cùng).</summary>
    public void RecordKill(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        var k = kills.Find(x => x.id == id);
        if (k == null) kills.Add(k = new KillCount { id = id });
        k.count++;
    }

    public int KillsOf(string id) => kills.Find(x => x.id == id)?.count ?? 0;

    public int TotalKills
    {
        get
        {
            int n = 0;
            foreach (var k in kills) n += k.count;
            return n;
        }
    }

    /// <summary>Ghi nhận phá đảo với con giáp này. Trả về true nếu giờ đã đủ cả 12 (lần đầu đủ hoặc xem lại).</summary>
    public bool AddEnding(string character, int total)
    {
        if (!string.IsNullOrEmpty(character) && !endingsSeen.Contains(character))
        {
            endingsSeen.Add(character);
            Commit();
        }
        return endingsSeen.Count >= total;
    }

    /// <summary>Đổi độ khó (Ác Mộng phải mở trước).</summary>
    public bool SetDifficulty(int level)
    {
        if (level < ZodiacDifficulty.Normal || level > ZodiacDifficulty.Nightmare || !ZodiacDifficulty.IsUnlocked(level, this)) return false;
        difficulty = level;
        Commit();
        return true;
    }

    // ───────── Tiến trình con giáp ─────────

    /// <summary>Tiến trình của con giáp (theo tên asset nhân vật), tạo mới nếu chưa có.</summary>
    public CharacterProgress Progress(string character)
    {
        var p = characters.Find(c => c.character == character);
        if (p != null) return p;
        p = new CharacterProgress { character = character };
        characters.Add(p);
        return p;
    }

    /// <summary>Cộng XP cho con giáp; lên cấp thì được điểm tu luyện. Trả về số cấp vừa lên.</summary>
    public int AddXp(string character, int amount)
    {
        if (amount <= 0 || string.IsNullOrEmpty(character)) return 0;
        var p = Progress(character);
        if (p.level >= CharacterProgress.MaxLevel) return 0;
        p.xp += amount;
        int gained = 0;
        while (p.level < CharacterProgress.MaxLevel && p.xp >= p.XpToNext)
        {
            p.xp -= p.XpToNext;
            p.level++;
            p.points += CharacterProgress.PointsPerLevel;
            gained++;
        }
        if (p.level >= CharacterProgress.MaxLevel) p.xp = 0;
        Commit();
        if (gained > 0) LeveledUp?.Invoke(p);
        return gained;
    }

    /// <summary>Cộng một điểm tu luyện: 0 Sinh lực, 1 Nội khí, 2 Sức mạnh, 3 Thân pháp.</summary>
    public bool SpendPoint(string character, int attribute)
    {
        var p = Progress(character);
        if (p.points <= 0) return false;
        switch (attribute)
        {
            case 0: p.vitality++; break;
            case 1: p.spirit++; break;
            case 2: p.strength++; break;
            case 3: p.agility++; break;
            default: return false;
        }
        p.points--;
        Commit();
        return true;
    }

    public const int ShardsPerPill = 3;

    /// <summary>Luyện 3 mảnh Huyết Đan (blood = true) / Khí Đan thành một viên — tăng vĩnh viễn cho con giáp đang chơi.</summary>
    public bool RefinePill(string character, bool blood)
    {
        string shard = blood ? "manh_huyet_dan" : "manh_khi_dan";
        if (CountOf(shard) < ShardsPerPill) return false;
        var p = Progress(character);
        if (blood) p.bloodPills++;
        else p.energyPills++;
        Consume(shard, ShardsPerPill);   // Consume đã Commit
        return true;
    }
}
