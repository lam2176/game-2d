using UnityEngine;

/// <summary>
/// Thang số cân bằng chung. Bản cũ dùng số rất nhỏ (máu 4–7, đòn 1–2, Khí 6) nên không còn chỗ cho trang bị,
/// lên cấp, chí mạng... Nay mọi máu / sát thương nhân <see cref="Unit"/> (1 "tim" cũ = 20 máu) — cảm giác
/// đánh giữ nguyên (quái 3 tim vẫn chết sau 3 đòn) nhưng cộng chỉ số lẻ (+5 máu, +3 công) có ý nghĩa.
/// Thanh Khí đổi từ 6 ô sang 100 điểm.
/// </summary>
public static class ZodiacBalance
{
    /// <summary>1 đơn vị máu / sát thương của bảng số liệu cũ.</summary>
    public const int Unit = 20;
    /// <summary>Khí tối đa mặc định.</summary>
    public const int BaseEnergy = 100;
    /// <summary>Số ô Khí của thang cũ (để quy đổi giá chiêu).</summary>
    public const int LegacyEnergy = 6;

    public const float BaseCritChance = 0.05f;
    public const float BaseCritDamage = 1.5f;
    /// <summary>Sát thương dao động ±8% để số bay lên không lặp lại.</summary>
    public const float DamageSpread = 0.08f;

    /// <summary>Quy đổi giá chiêu thang cũ (ô Khí / 6) sang thang 100, làm tròn tới bội số 5.</summary>
    public static int EnergyCost(int legacyCost) =>
        Mathf.RoundToInt(legacyCost * (float)BaseEnergy / LegacyEnergy / 5f) * 5;

    /// <summary>Giáp giảm sát thương theo tỉ lệ 100 / (100 + giáp): 100 giáp = giảm một nửa. Trúng đòn luôn mất ít nhất 1.</summary>
    public static int Mitigate(int amount, int defense) =>
        defense <= 0 ? amount : Mathf.Max(1, Mathf.RoundToInt(amount * 100f / (100f + defense)));
}
