using UnityEngine;

/// <summary>
/// Dữ liệu một con giáp: ảnh, collider ôm phần thân và thông số di chuyển.
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

    [Header("Di chuyển")]
    public float walkSpeed = 5f;
    public float runSpeed = 8.5f;
    public float jumpHeight = 3.2f;
}
