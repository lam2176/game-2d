using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quy đổi tu luyện của một con giáp ra chỉ số: mỗi điểm Sinh lực / Nội khí / Sức mạnh / Thân pháp và mỗi viên Đan
/// cộng bao nhiêu. Dùng chung cho <see cref="PlayerEquipment"/> (cộng vào CharacterStats) và giao diện tu luyện.
/// </summary>
public static class Progression
{
    public static readonly string[] AttributeNames = { "Sinh lực", "Nội khí", "Sức mạnh", "Thân pháp" };
    public static readonly string[] AttributeHelp =
    {
        "+12 Máu tối đa",
        "+6 Khí tối đa, +0.15 hồi Khí/giây",
        "+2 Công",
        "+1% Chí mạng, +3 Giáp",
    };

    /// <summary>XP một con quái cho khi bị hạ: theo máu tối đa (quái 3 tim ≈ 15 XP), boss nhiều hơn hẳn.</summary>
    public static int XpFor(int maxHealth, bool boss) => Mathf.Max(5, maxHealth / ZodiacBalance.Unit * 5) * (boss ? 4 : 1);

    public static IEnumerable<StatModifier> Modifiers(CharacterProgress p)
    {
        if (p.vitality > 0) yield return new StatModifier(StatType.MaxHealth, ModifierKind.Flat, 12f * p.vitality);
        if (p.spirit > 0)
        {
            yield return new StatModifier(StatType.MaxEnergy, ModifierKind.Flat, 6f * p.spirit);
            yield return new StatModifier(StatType.EnergyRegen, ModifierKind.Flat, 0.15f * p.spirit);
        }
        if (p.strength > 0) yield return new StatModifier(StatType.Attack, ModifierKind.Flat, 2f * p.strength);
        if (p.agility > 0)
        {
            yield return new StatModifier(StatType.CritChance, ModifierKind.Flat, 0.01f * p.agility);
            yield return new StatModifier(StatType.Defense, ModifierKind.Flat, 3f * p.agility);
        }
        if (p.bloodPills > 0) yield return new StatModifier(StatType.MaxHealth, ModifierKind.Flat, CharacterProgress.PillBonus * p.bloodPills);
        if (p.energyPills > 0) yield return new StatModifier(StatType.MaxEnergy, ModifierKind.Flat, CharacterProgress.PillBonus * p.energyPills);
    }
}
