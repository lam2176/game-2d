using UnityEngine;

/// <summary>
/// Tài nguyên dùng lúc chạy mà script tạo object động cần tới: âm thanh và vật liệu sprite.
/// Asset nằm ở Assets/Zodiac/Resources/ZodiacResources.asset (builder tự tạo và gán file).
/// Âm thanh: Kenney.nl (CC0) + tổng hợp bằng code (Assets/Zodiac/Audio/Synth, Music) — xem docs/THIET_KE_GAME.md.
/// </summary>
[CreateAssetMenu(menuName = "Zodiac/Resources", fileName = "ZodiacResources")]
public class ZodiacResources : ScriptableObject
{
    [Header("Vật liệu")]
    [Tooltip("Sprite chịu ánh sáng 2D (nhân vật, địa hình).")]
    public Material spriteLit;
    [Tooltip("Sprite tự phát sáng, không bị bóng tối che (lửa, ma trơi, hiệu ứng).")]
    public Material spriteUnlit;

    [Header("Âm thanh — di chuyển")]
    public AudioClip[] footstepGrass;
    public AudioClip[] footstepStone;
    public AudioClip[] footstepWood;
    public AudioClip[] jump;
    public AudioClip[] land;

    [Header("Âm thanh — chiến đấu")]
    public AudioClip[] swing;
    public AudioClip[] swingHeavy;
    public AudioClip[] hit;
    public AudioClip[] hitHeavy;
    public AudioClip[] hurt;
    public AudioClip[] stomp;
    public AudioClip[] enemyDeath;
    public AudioClip[] spike;
    public AudioClip[] fireball;
    public AudioClip[] bossRoar;
    public AudioClip[] bossSlam;

    [Header("Âm thanh — khác")]
    public AudioClip[] coin;
    public AudioClip[] lantern;
    public AudioClip[] win;
    public AudioClip[] lose;
    public AudioClip musicMenu;

    static ZodiacResources instance;
    static bool searched;

    /// <summary>Asset trong Resources (null nếu chưa dựng — mọi chỗ gọi phải chịu được null).</summary>
    public static ZodiacResources Get
    {
        get
        {
            if (instance == null && !searched)
            {
                searched = true;
                instance = Resources.Load<ZodiacResources>("ZodiacResources");
            }
            return instance;
        }
    }
}
