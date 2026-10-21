using UnityEngine;

/// <summary>
/// Độ khó chọn ở Cổng làng: Thường / Khó / Ác Mộng (mở sau khi phá đảo màn cuối).
/// Nhân máu và sát thương quái, tỉ lệ ra quái tinh anh, đồ rơi và XP. Lưu trong <see cref="PlayerProfile.difficulty"/>.
/// </summary>
public static class ZodiacDifficulty
{
    public const int Normal = 0, Hard = 1, Nightmare = 2;

    public static readonly string[] Names = { "Thường", "Khó", "Ác Mộng" };
    public static readonly string[] Help =
    {
        "Quái như bình thường.",
        "Quái trâu hơn ×1.5, đánh đau hơn ×1.3, nhiều tinh anh hơn · đồ rơi ×1.25, XP ×1.3",
        "Quái trâu ×2.2, đánh đau ×1.7, tinh anh khắp nơi · đồ rơi ×1.6, XP ×1.7",
    };
    public static readonly Color[] Colors =
    {
        new Color(0.62f, 0.9f, 0.6f), new Color(1f, 0.7f, 0.3f), new Color(0.9f, 0.35f, 0.9f),
    };

    static readonly float[] health = { 1f, 1.5f, 2.2f };
    static readonly float[] damage = { 1f, 1.3f, 1.7f };
    static readonly float[] elite = { 0.08f, 0.2f, 0.35f };
    static readonly float[] loot = { 1f, 1.25f, 1.6f };
    static readonly float[] xp = { 1f, 1.3f, 1.7f };

    public static int Current => Mathf.Clamp(PlayerProfile.Current.difficulty, Normal, Nightmare);
    public static float HealthScale => health[Current];
    public static float DamageScale => damage[Current];
    public static float EliteChance => elite[Current];
    public static float LootScale => loot[Current];
    public static float XpScale => xp[Current];

    public static bool IsUnlocked(int level, PlayerProfile p) => level != Nightmare || p.nightmareUnlocked;
}
