using UnityEngine;

/// <summary>
/// Dữ liệu một con giáp: ảnh và collider ôm phần thân.
/// Mỗi con giáp một asset — prototype hiện chỉ có Thân (Khỉ).
/// </summary>
[CreateAssetMenu(menuName = "Zodiac/Character", fileName = "ZodiacCharacter")]
public class ZodiacCharacter : ScriptableObject
{
    public string displayName = "Khỉ";
    public string zodiacName = "Thân";
    public Sprite sprite;

    [Header("Collider thân (gốc toạ độ ở bàn chân = pivot sprite)")]
    public Vector2 colliderSize = new Vector2(0.8f, 1.9f);
    public Vector2 colliderOffset = new Vector2(0.15f, 0.95f);

    // Tốc độ, lực nhảy, trọng lực chỉnh ở ZodiacPlayerController trên Player (chỉnh được cả khi đang Play).
}
