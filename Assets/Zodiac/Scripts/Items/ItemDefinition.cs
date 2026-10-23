using System.Text;
using UnityEngine;

public enum ItemType
{
    /// <summary>Dùng một lần (bình máu, bình khí).</summary>
    Consumable,
    /// <summary>Mặc vào ô trang bị, cộng chỉ số.</summary>
    Equipment,
    /// <summary>Nguyên liệu (rèn, đổi đồ — dùng ở làng trung tâm).</summary>
    Material,
}

public enum EquipSlot { None, Weapon, Armor, Charm, Gem }

public enum ItemRarity { Common, Good, Rare, Epic, Legendary }

/// <summary>
/// Một loại vật phẩm: tên, biểu tượng, độ hiếm, loại; trang bị thì có ô và các modifier cộng vào <see cref="CharacterStats"/>;
/// đồ dùng thì có lượng máu / Khí hồi. Asset do builder tạo trong Assets/Zodiac/Items (bảng ZodiacItemDefs), tra theo
/// <see cref="id"/> qua <see cref="ItemDatabase"/> (để lưu game chỉ cần ghi id).
/// </summary>
[CreateAssetMenu(menuName = "Zodiac/Item", fileName = "Item")]
public class ItemDefinition : ScriptableObject
{
    [Tooltip("Mã cố định (lưu game dùng mã này) — không đổi sau khi đã phát hành.")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemType type;
    public ItemRarity rarity;
    [Min(1)] public int maxStack = 1;

    [Header("Trang bị")]
    public EquipSlot slot;
    public StatModifier[] modifiers = System.Array.Empty<StatModifier>();

    [Header("Đồ dùng")]
    [Min(0)] public int heal;
    [Min(0)] public int energy;

    [Header("Giá (đồng)")]
    [Min(0)] public int price = 10;

    public bool Stackable => maxStack > 1;

    public static Color RarityColor(ItemRarity r) => r switch
    {
        ItemRarity.Good => new Color(0.45f, 0.9f, 0.4f),
        ItemRarity.Rare => new Color(0.35f, 0.65f, 1f),
        ItemRarity.Epic => new Color(0.75f, 0.45f, 1f),
        ItemRarity.Legendary => new Color(1f, 0.6f, 0.15f),
        _ => new Color(0.92f, 0.92f, 0.92f),
    };

    public static string RarityName(ItemRarity r) => r switch
    {
        ItemRarity.Good => "Tốt",
        ItemRarity.Rare => "Hiếm",
        ItemRarity.Epic => "Sử thi",
        ItemRarity.Legendary => "Huyền thoại",
        _ => "Thường",
    };

    public static string SlotName(EquipSlot s) => s switch
    {
        EquipSlot.Weapon => "Vũ khí",
        EquipSlot.Armor => "Giáp",
        EquipSlot.Charm => "Bùa",
        EquipSlot.Gem => "Ngọc",
        _ => "",
    };

    public static string StatName(StatType s) => s switch
    {
        StatType.MaxHealth => "Máu tối đa",
        StatType.MaxEnergy => "Khí tối đa",
        StatType.EnergyRegen => "Hồi Khí/giây",
        StatType.Attack => "Công",
        StatType.Defense => "Giáp",
        StatType.CritChance => "Chí mạng",
        StatType.CritDamage => "Sát thương chí mạng",
        _ => s.ToString(),
    };

    /// <summary>Một dòng chỉ số: "+6 Công", "+5% Chí mạng".</summary>
    public static string ModifierText(StatModifier m)
    {
        bool percent = m.kind == ModifierKind.Percent || m.stat == StatType.CritChance || m.stat == StatType.CritDamage;
        string value = percent ? $"{m.value * 100f:+0;-0}%" : $"{m.value:+0.#;-0.#}";
        return $"{value} {StatName(m.stat)}";
    }

    /// <summary>Mô tả loại đồ (chưa có cấp đập / dòng phụ).</summary>
    public string Tooltip() => Tooltip(null);

    /// <summary>Mô tả đầy đủ cho khung xem đồ: tên (+cấp), loại, độ hiếm, chỉ số đã tính cấp đập, dòng phụ, hiệu quả, mô tả.</summary>
    public string Tooltip(ItemStack stack)
    {
        var sb = new StringBuilder();
        string color = ColorUtility.ToHtmlStringRGB(RarityColor(rarity));
        string title = stack != null && !stack.IsEmpty ? stack.DisplayName : displayName;
        sb.Append($"<b><color=#{color}>{title}</color></b>\n");
        string kind = type switch
        {
            ItemType.Equipment => SlotName(slot),
            ItemType.Consumable => "Đồ dùng",
            _ => "Nguyên liệu (không mặc được)",
        };
        sb.Append($"<size=22>{kind} · {RarityName(rarity)}</size>\n");
        float scale = 1f + (stack != null ? ItemStack.LevelBonus * stack.level : 0f);
        foreach (var m in modifiers) sb.Append($"<color=#9fe39a>{ModifierText(new StatModifier(m.stat, m.kind, m.value * scale))}</color>\n");
        if (stack != null && stack.affixes != null)
            foreach (var a in stack.affixes) sb.Append($"<color=#8fc8ff>{ModifierText(a)}  (phụ)</color>\n");
        if (heal > 0) sb.Append($"<color=#ff9a9a>Hồi {heal} máu</color>\n");
        if (energy > 0) sb.Append($"<color=#8fd3ff>Hồi {energy} Khí</color>\n");
        if (!string.IsNullOrEmpty(description)) sb.Append($"<i>{description}</i>\n");
        sb.Append($"<size=20>Giá: {price} đồng</size>");
        return sb.ToString();
    }
}
