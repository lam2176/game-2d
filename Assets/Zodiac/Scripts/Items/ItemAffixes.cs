using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dòng chỉ số phụ tung ngẫu nhiên khi đồ trang bị rơi ra: số dòng theo độ hiếm (Thường 0 · Tốt 1 · Hiếm 2 · Sử thi / Huyền thoại 3),
/// mỗi dòng một chỉ số khác nhau lấy từ bảng hợp với ô đồ (vũ khí thiên công/chí mạng, giáp thiên giáp/máu, bùa – ngọc đủ loại).
/// </summary>
public static class ItemAffixes
{
    struct Range
    {
        public StatType stat;
        public float min, max;
        public bool integer;

        public Range(StatType stat, float min, float max, bool integer = true)
        {
            this.stat = stat;
            this.min = min;
            this.max = max;
            this.integer = integer;
        }
    }

    static readonly Range[] Weapon =
    {
        new Range(StatType.Attack, 2, 5),
        new Range(StatType.CritChance, 0.02f, 0.05f, false),
        new Range(StatType.CritDamage, 0.1f, 0.25f, false),
        new Range(StatType.MaxEnergy, 5, 15),
    };

    static readonly Range[] Armor =
    {
        new Range(StatType.Defense, 4, 10),
        new Range(StatType.MaxHealth, 8, 20),
        new Range(StatType.EnergyRegen, 0.2f, 0.5f, false),
    };

    static readonly Range[] Trinket =
    {
        new Range(StatType.MaxHealth, 6, 15),
        new Range(StatType.MaxEnergy, 5, 12),
        new Range(StatType.EnergyRegen, 0.2f, 0.4f, false),
        new Range(StatType.CritChance, 0.01f, 0.03f, false),
        new Range(StatType.Attack, 1, 3),
    };

    public static int CountFor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Good => 1,
        ItemRarity.Rare => 2,
        ItemRarity.Epic => 3,
        ItemRarity.Legendary => 3,
        _ => 0,
    };

    /// <summary>Một món đồ mới với dòng phụ đã tung (đồ không phải trang bị thì trả về chồng thường).</summary>
    public static ItemStack Roll(ItemDefinition def, int count = 1)
    {
        var stack = new ItemStack { id = def.id, count = count };
        if (def.type != ItemType.Equipment) return stack;
        var pool = new List<Range>(def.slot switch
        {
            EquipSlot.Weapon => Weapon,
            EquipSlot.Armor => Armor,
            _ => Trinket,
        });
        int n = Mathf.Min(CountFor(def.rarity), pool.Count);
        for (int i = 0; i < n; i++)
        {
            int k = Random.Range(0, pool.Count);
            var r = pool[k];
            pool.RemoveAt(k);   // mỗi chỉ số tối đa một dòng
            float v = Random.Range(r.min, r.max);
            if (r.integer) v = Mathf.Round(v);
            stack.affixes.Add(new StatModifier(r.stat, ModifierKind.Flat, v));
        }
        return stack;
    }
}
