using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Bộ đồ: mặc cùng lúc 2 / 3 món của một bộ thì được thêm chỉ số. Mỗi món thuộc nhiều nhất một bộ.
/// <see cref="PlayerEquipment"/> cộng phần thưởng bộ vào chỉ số; khung xem đồ hiện tiến độ bộ (<see cref="Describe"/>).
/// </summary>
public static class ItemSets
{
    public class Set
    {
        public string name;
        public string[] items;
        public StatModifier[] two, three;
    }

    static StatModifier Flat(StatType s, float v) => new StatModifier(s, ModifierKind.Flat, v);
    static StatModifier Pct(StatType s, float v) => new StatModifier(s, ModifierKind.Percent, v);

    public static readonly Set[] Sets =
    {
        new Set
        {
            name = "Bộ Thôn Quê", items = new[] { "kiem_tre", "ao_giap_tre", "vong_bac" },
            two = new[] { Flat(StatType.MaxHealth, 20) },
            three = new[] { Flat(StatType.EnergyRegen, 1f), Flat(StatType.Defense, 8) },
        },
        new Set
        {
            name = "Bộ Đất Nung", items = new[] { "giao_dong", "khien_dong", "ngoc_luc" },
            two = new[] { Flat(StatType.Defense, 15) },
            three = new[] { Flat(StatType.Attack, 6), Flat(StatType.MaxHealth, 25) },
        },
        new Set
        {
            name = "Bộ Lạc Long", items = new[] { "dao_rong", "bua_khi", "ngoc_bich" },
            two = new[] { Pct(StatType.Attack, 0.08f) },
            three = new[] { Flat(StatType.CritDamage, 0.3f), Flat(StatType.MaxEnergy, 20) },
        },
        new Set
        {
            name = "Bộ Yêu Tinh", items = new[] { "chuy_chan_tinh", "giap_vay_thuong_luong", "bua_mat_ba_bi" },
            two = new[] { Flat(StatType.Attack, 6), Flat(StatType.Defense, 10) },
            three = new[] { Flat(StatType.CritChance, 0.08f), Flat(StatType.MaxHealth, 40) },
        },
    };

    public static Set SetOf(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var s in Sets)
            if (System.Array.IndexOf(s.items, id) >= 0) return s;
        return null;
    }

    /// <summary>Số món khác nhau của bộ đang mặc.</summary>
    public static int Worn(Set set, ItemStack[] equipped)
    {
        int n = 0;
        foreach (var id in set.items)
            foreach (var e in equipped)
                if (e != null && !e.IsEmpty && e.id == id) { n++; break; }
        return n;
    }

    /// <summary>Chỉ số thưởng của mọi bộ đang mặc đủ 2 / 3 món.</summary>
    public static IEnumerable<StatModifier> ActiveBonuses(ItemStack[] equipped)
    {
        foreach (var set in Sets)
        {
            int n = Worn(set, equipped);
            if (n >= 2) foreach (var m in set.two) yield return m;
            if (n >= 3) foreach (var m in set.three) yield return m;
        }
    }

    /// <summary>Dòng mô tả bộ cho khung xem đồ: tên, số món đang mặc, thưởng 2 / 3 món (sáng khi đã có).</summary>
    public static string Describe(string id, ItemStack[] equipped)
    {
        var set = SetOf(id);
        if (set == null) return "";
        int n = equipped != null ? Worn(set, equipped) : 0;
        var sb = new StringBuilder();
        sb.Append($"<color=#ffd27a><b>{set.name}</b> ({n}/3)</color>\n");
        sb.Append(Line("2 món", set.two, n >= 2));
        sb.Append(Line("3 món", set.three, n >= 3));
        var names = new List<string>();
        foreach (var item in set.items)
        {
            var def = ItemDatabase.Get(item);
            names.Add(def != null ? def.displayName : item);
        }
        sb.Append($"<size=18><color=#a89880>{string.Join(" · ", names)}</color></size>\n");
        return sb.ToString();
    }

    static string Line(string label, StatModifier[] mods, bool active)
    {
        var parts = new List<string>();
        foreach (var m in mods) parts.Add(ItemDefinition.ModifierText(m));
        string color = active ? "#9fe39a" : "#7f7466";
        return $"<size=20><color={color}>{label}: {string.Join(", ", parts)}</color></size>\n";
    }
}
