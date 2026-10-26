using System;
using System.Collections.Generic;

/// <summary>
/// Danh mục nhiệm vụ: ba nhóm (Cốt truyện · Săn quái · Rèn luyện), mỗi nhóm tối đa <see cref="PerCategory"/> nhiệm vụ.
/// Tiến độ tính thẳng từ <see cref="PlayerProfile"/> (màn đã mở, số quái đã hạ, cấp, tháp, đồ đã rèn…) nên không cần
/// ghi thêm gì lúc chơi — chỉ lưu id các nhiệm vụ đã nhận thưởng (<see cref="PlayerProfile.questsClaimed"/>).
/// Giao diện ở trang Nhiệm vụ của hành trang (phím J).
/// </summary>
public static class ZodiacQuests
{
    public const int PerCategory = 10;
    public static readonly string[] Categories = { "Cốt truyện", "Săn quái", "Rèn luyện" };

    public class Quest
    {
        public string id, title, text;
        public int category, target = 1;
        public Func<PlayerProfile, int> progress;
        public int coins, xp;
        public string item = "";
        public int itemCount;
    }

    public enum State { InProgress, Claimable, Claimed }

    static readonly string[] LevelNames =
    {
        "Làng Quê", "Cánh Đồng Lúa", "Rừng Trúc", "Đầm Sen", "Chợ Phiên Ma", "Hang Đá Vôi", "Bờ Hồ Tây", "Động Hồ Tinh",
    };

    public static readonly Quest[] All = Build();

    static Quest[] Build()
    {
        var list = new List<Quest>();

        // ───────── Cốt truyện: qua từng màn, rồi phá đảo với nhiều con giáp ─────────
        for (int i = 0; i < LevelNames.Length; i++)
        {
            int level = i + 1;
            bool last = level == LevelNames.Length;
            list.Add(new Quest
            {
                id = $"story_{level}", category = 0,
                title = $"Màn {level} — {LevelNames[i]}",
                text = last ? "Xuống động dưới đáy Hồ Tây, hạ Hồ Tinh và ghép lại Trống Đồng Thiên Âm."
                            : $"Dọn sạch quái ở {LevelNames[i]} và đi tới cuối màn.",
                progress = p => p.unlockedLevels > level ? 1 : 0,
                coins = 40 + i * 30, xp = 30 + i * 25,
                item = last ? "manh_huyet_dan" : i % 2 == 0 ? "binh_mau_nho" : "manh_khi_dan", itemCount = last ? 3 : 1,
            });
        }
        list.Add(new Quest
        {
            id = "story_endings_3", category = 0, title = "Ba con giáp", target = 3,
            text = "Phá đảo với ba con giáp khác nhau — mỗi con một kết thúc riêng.",
            progress = p => p.endingsSeen.Count, coins = 300, item = "manh_khi_dan", itemCount = 3,
        });
        list.Add(new Quest
        {
            id = "story_endings_12", category = 0, title = "Đủ mười hai canh giờ", target = 12,
            text = "Phá đảo với cả mười hai con giáp để thấy kết thúc thật.",
            progress = p => p.endingsSeen.Count, coins = 1200, item = "manh_huyet_dan", itemCount = 6,
        });

        // ───────── Săn quái: theo từng loại trong Sổ ghi chép ─────────
        (string id, int target, int coins)[] hunts =
        {
            ("Enemy_LinhDatNung", 30, 60), ("Enemy_LinhKhien", 15, 70), ("Enemy_BoGaiLan", 15, 70),
            ("Enemy_MaTroi", 15, 80), ("Enemy_CocTia", 10, 90), ("Enemy_ThachSu", 8, 100),
            ("Boss_ThuongLuong", 1, 150), ("Boss_OngBaBi", 1, 150), ("Boss_ChanTinh", 1, 200), ("Boss_HoTinh", 1, 300),
        };
        foreach (var (id, target, coins) in hunts)
        {
            var beast = Array.Find(ZodiacJournal.Beasts, b => b.id == id);
            string name = beast != null ? beast.name : id;
            bool boss = id.StartsWith("Boss_");
            list.Add(new Quest
            {
                id = "hunt_" + id, category = 1, target = target,
                title = boss ? $"Trừ {name}" : $"Hạ {target} {name}",
                text = (boss ? $"Đánh bại {name}" : $"Hạ {target} con {name}") + (beast != null ? $" — gặp ở {beast.where}." : "."),
                progress = p => p.KillsOf(id), coins = coins, xp = boss ? 120 : 40,
                item = boss ? "manh_huyet_dan" : "", itemCount = boss ? 2 : 0,
            });
        }

        // ───────── Rèn luyện: cấp, tháp, lò rèn, luyện đan, khảm ngọc ─────────
        foreach (int lv in new[] { 5, 10, 20 })
            list.Add(new Quest
            {
                id = $"train_level_{lv}", category = 2, target = lv, title = $"Lên cấp {lv}",
                text = $"Đưa một con giáp bất kỳ lên cấp {lv}.",
                progress = MaxLevel, coins = lv * 20, item = "manh_khi_dan", itemCount = lv / 5,
            });
        foreach (int floors in new[] { 5, 10, 20 })
            list.Add(new Quest
            {
                id = $"train_tower_{floors}", category = 2, target = floors, title = $"Tháp {floors} tầng",
                text = $"Vượt {floors} tầng Tháp Mười Hai Canh Giờ trong một lần leo.",
                progress = p => p.towerBest, coins = floors * 25, xp = floors * 10,
            });
        list.Add(new Quest
        {
            id = "train_forge_3", category = 2, target = 3, title = "Thợ rèn tập sự",
            text = "Rèn một món trang bị lên +3 ở lò rèn trong làng.",
            progress = BestForge, coins = 120, item = "binh_mau_nho", itemCount = 2,
        });
        list.Add(new Quest
        {
            id = "train_pill", category = 2, title = "Luyện đan",
            text = "Luyện ba mảnh Huyết Đan hoặc Khí Đan thành một viên ở trang Tu luyện.",
            progress = Pills, coins = 80, item = "manh_huyet_dan", itemCount = 1,
        });
        list.Add(new Quest
        {
            id = "train_gem", category = 2, title = "Khảm ngọc",
            text = "Khảm một viên ngọc vào vũ khí hoặc giáp ở lò rèn.",
            progress = HasGem, coins = 100, item = "manh_khi_dan", itemCount = 1,
        });
        list.Add(new Quest
        {
            id = "train_kills_300", category = 2, target = 300, title = "Trăm trận",
            text = "Hạ tổng cộng 300 quái trên mọi nẻo đường.",
            progress = p => p.TotalKills, coins = 400, xp = 200,
        });

        return list.ToArray();
    }

    static int MaxLevel(PlayerProfile p)
    {
        int best = 1;
        foreach (var c in p.characters) best = Math.Max(best, c.level);
        return best;
    }

    static int Pills(PlayerProfile p)
    {
        int n = 0;
        foreach (var c in p.characters) n += c.bloodPills + c.energyPills;
        return n;
    }

    static int BestForge(PlayerProfile p)
    {
        int best = 0;
        foreach (var arr in new[] { p.bag, p.equipped })
            foreach (var s in arr)
                if (!s.IsEmpty) best = Math.Max(best, s.level);
        return best;
    }

    static int HasGem(PlayerProfile p)
    {
        foreach (var arr in new[] { p.bag, p.equipped })
            foreach (var s in arr)
                if (!s.IsEmpty && s.gems.Exists(g => !string.IsNullOrEmpty(g))) return 1;
        return 0;
    }

    public static int Progress(Quest q, PlayerProfile p) => Math.Min(q.target, Math.Max(0, q.progress(p)));

    public static State StateOf(Quest q, PlayerProfile p) =>
        p.questsClaimed.Contains(q.id) ? State.Claimed : Progress(q, p) >= q.target ? State.Claimable : State.InProgress;

    /// <summary>Nhiệm vụ của một nhóm, xếp: chờ nhận thưởng → đang làm → đã xong (giữ thứ tự gốc trong mỗi loại).</summary>
    public static List<Quest> InCategory(int category, PlayerProfile p)
    {
        var list = new List<Quest>();
        foreach (var s in new[] { State.Claimable, State.InProgress, State.Claimed })
            foreach (var q in All)
                if (q.category == category && StateOf(q, p) == s) list.Add(q);
        return list;
    }

    /// <summary>Số nhiệm vụ đã xong mà chưa nhận thưởng (category &lt; 0 = mọi nhóm).</summary>
    public static int ClaimableCount(PlayerProfile p, int category = -1)
    {
        int n = 0;
        foreach (var q in All)
            if ((category < 0 || q.category == category) && StateOf(q, p) == State.Claimable) n++;
        return n;
    }

    /// <summary>Dòng phần thưởng: "120 đồng · 40 XP · 2× Mảnh Huyết Đan".</summary>
    public static string RewardText(Quest q)
    {
        var parts = new List<string>();
        if (q.coins > 0) parts.Add($"{q.coins} đồng");
        if (q.xp > 0) parts.Add($"{q.xp} XP");
        var def = ItemDatabase.Get(q.item);
        if (def != null && q.itemCount > 0) parts.Add(q.itemCount > 1 ? $"{q.itemCount}× {def.displayName}" : def.displayName);
        return parts.Count > 0 ? string.Join(" · ", parts) : "—";
    }

    /// <summary>
    /// Nhận thưởng (đồng, XP cho con giáp đang chơi, đồ vào túi). Túi đầy thì không nhận — trả về false kèm lý do.
    /// </summary>
    public static bool Claim(Quest q, PlayerProfile p, string character, out string message)
    {
        message = "";
        if (StateOf(q, p) != State.Claimable) return false;
        var def = ItemDatabase.Get(q.item);
        if (def != null && q.itemCount > 0)
        {
            int left = p.Add(def, q.itemCount);
            if (left > 0)
            {
                if (left < q.itemCount) p.Consume(def.id, q.itemCount - left);   // trả lại phần đã lỡ cho vào
                message = "Túi đầy — dọn bớt rồi nhận thưởng";
                return false;
            }
        }
        p.questsClaimed.Add(q.id);
        if (q.xp > 0 && !string.IsNullOrEmpty(character)) p.AddXp(character, q.xp);
        p.AddCoins(q.coins);
        p.Commit();
        message = $"Hoàn thành: {q.title} — nhận {RewardText(q)}";
        return true;
    }
}
