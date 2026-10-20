using UnityEngine;

/// <summary>Kiểu chiêu thức (phím K). Thông số chi tiết nằm trong <see cref="SkillConfig"/>.</summary>
public enum SkillType
{
    /// <summary>Lao theo hướng nhìn (có thể chếch lên): bất tử và/hoặc gây sát thương khi lao qua quái.</summary>
    Dash,
    /// <summary>Bắn đạn: một hay nhiều viên, xỏ xuyên, bay về như boomerang, lướt sát đất.</summary>
    Projectile,
    /// <summary>Sóng âm quanh mình hoặc phía trước: sát thương, choáng, đẩy văng, xoá đạn địch.</summary>
    Burst,
    /// <summary>Đứng gồng rồi hồi máu.</summary>
    Heal,
    /// <summary>Tàng hình: bất tử, quái mất dấu; đòn đánh đầu tiên sau đó nhân sát thương.</summary>
    Stealth,
    /// <summary>Lao thẳng xuống đất, tạo sóng chấn khi chạm đất.</summary>
    GroundPound,
}

[System.Serializable]
public class SkillConfig
{
    public SkillType type;
    public string name = "Chiêu";
    [TextArea] public string description;
    [Min(0.1f)] public float cooldown = 2f;
    [Min(0)] public int damage = 1;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashTime = 0.2f;
    [Tooltip("Góc lao so với phương ngang (độ). >0 = chếch lên.")]
    public float dashAngle = 0f;
    public bool dashInvulnerable = true;
    [Tooltip("Lao trúng quái thì bật nảy lên (Hổ vồ).")]
    public bool dashBounceOnHit = false;

    [Header("Projectile")]
    [Min(1)] public int projectileCount = 1;
    [Tooltip("Tổng góc toả của chùm đạn (độ).")]
    public float spreadAngle = 0f;
    public float projectileSpeed = 14f;
    public float projectileRange = 10f;
    public float projectileSize = 0.6f;
    [Tooltip("Xuyên qua bao nhiêu mục tiêu. -1 = xuyên tất cả.")]
    public int pierce = 0;
    public bool boomerang = false;
    [Tooltip("Bắn cả hai hướng (sóng chấn chạy trên mặt đất).")]
    public bool bothDirections = false;
    [Tooltip("Đạn bay sát mặt đất, không bị tường thấp chặn ngay.")]
    public bool groundWave = false;

    [Header("Burst / GroundPound")]
    public float radius = 3f;
    [Tooltip("Tâm sóng dời ra phía trước bao xa (0 = quanh mình).")]
    public float forwardOffset = 0f;
    public float stunTime = 1f;
    public float knockback = 1f;
    public bool clearsProjectiles = false;
    public float diveSpeed = 26f;

    [Header("Heal / Stealth")]
    [Min(0)] public int healAmount = 2;
    public float channelTime = 0.6f;
    public float stealthDuration = 2.5f;
    public float backstabMultiplier = 3f;
}

/// <summary>
/// Dữ liệu một con giáp: ảnh, collider, chỉ số, nội tại và chiêu thức.
/// ZodiacPlayerController / PlayerMeleeAttack / PlayerSkill đọc asset này khi vào màn.
/// </summary>
[CreateAssetMenu(menuName = "Zodiac/Character", fileName = "ZodiacCharacter")]
public class ZodiacCharacter : ScriptableObject
{
    public string displayName = "Khỉ";
    public string zodiacName = "Thân";
    [Tooltip("Nhạc cụ nhân vật cầm — chủ đề của chiêu thức.")]
    public string instrument = "";
    public Sprite sprite;
    [Tooltip("Màu chủ đạo: hiệu ứng chiêu, khung chọn nhân vật.")]
    public Color themeColor = new Color(1f, 0.8f, 0.3f);

    [Header("Collider thân (gốc toạ độ ở bàn chân = pivot sprite)")]
    public Vector2 colliderSize = new Vector2(0.8f, 1.9f);
    public Vector2 colliderOffset = new Vector2(0.15f, 0.95f);

    [Header("Chỉ số")]
    [Min(1)] public int maxHealth = 5;
    [Min(0f)] public float walkSpeed = 5f;
    [Min(0f)] public float runSpeed = 8.5f;
    [Min(0f)] public float jumpHeight = 3.2f;
    [Min(1)] public int attackDamage = 1;

    [Header("Nội tại")]
    public string passiveName = "";
    [TextArea] public string passiveDescription = "";
    [Tooltip("Số lần nhảy thêm trên không.")]
    [Min(0)] public int airJumps = 0;
    [Tooltip("Giữ Space khi đang rơi để lượn với tốc độ rơi này. 0 = không lượn.")]
    [Min(0f)] public float glideFallSpeed = 0f;
    [Tooltip("Giảm lực đẩy lùi khi bị đánh (0..1).")]
    [Range(0f, 1f)] public float knockbackResistance = 0f;
    [Tooltip("Số lần giữ lại 1 máu thay vì chết mỗi màn.")]
    [Min(0)] public int lastStandCharges = 0;
    [Tooltip("Đánh thường gây độc: tổng sát thương độc (0 = không).")]
    [Min(0)] public int poisonDamage = 0;
    [Tooltip("Hút đồng xu trong bán kính này. 0 = tắt.")]
    [Min(0f)] public float coinMagnetRadius = 0f;
    [Tooltip("Nhặt đủ số đồng xu này thì hồi 1 máu. 0 = tắt.")]
    [Min(0)] public int coinsPerHeal = 0;
    [Tooltip("Nhân sát thương khi đạp đầu quái.")]
    [Min(1)] public int stompDamageMultiplier = 1;

    [Header("Chiêu thức (phím K)")]
    public SkillConfig skill = new SkillConfig();

    public string Title => $"{displayName} — Tuổi {zodiacName}";
}
