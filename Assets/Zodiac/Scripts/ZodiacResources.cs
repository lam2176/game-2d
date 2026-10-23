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
    [Tooltip("Đòn đánh vào khiên.")]
    public AudioClip[] block;
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

    [Header("Âm thanh — làng, giao diện")]
    [Tooltip("Búa gõ đe (lò rèn).")]
    public AudioClip[] anvil;
    public AudioClip[] forgeSuccess;
    [Tooltip("Đập hỏng tụt cấp / phân giải.")]
    public AudioClip[] forgeBreak;
    [Tooltip("Tiếng đếm tiền (mua bán ở tiệm).")]
    public AudioClip[] coinsHandle;
    [Tooltip("Lật trang (sổ ghi chép, hướng dẫn).")]
    public AudioClip[] page;
    public AudioClip[] uiClick;
    [Tooltip("Lửa lò rèn — lặp liên tục, phát 3D quanh lò rèn trong làng.")]
    public AudioClip fireLoop;

    [Header("Sổ ghi chép")]
    [Tooltip("Ảnh quái (Art/Enemies) — sổ ghi chép tìm theo tên file.")]
    public Sprite[] enemyArt;

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
