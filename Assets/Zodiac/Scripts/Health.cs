using System;
using UnityEngine;

/// <summary>
/// Một lần trúng đòn: sát thương, vị trí nguồn (để đẩy lùi ra xa), hệ số đẩy lùi, thời gian choáng
/// và có quật ngã không (đòn kết combo, đá bay — bên bị đánh văng ra nằm đất rồi mới dậy).
/// </summary>
public struct Hit
{
    public int amount;
    public Vector3 source;
    [Tooltip("Hệ số nhân lực đẩy lùi của bên bị đánh. 0 = không đẩy (độc, gai...).")]
    public float knockback;
    [Tooltip("Thời gian choáng thêm (giây) — quái đứng im, không tấn công.")]
    public float stun;
    [Tooltip("Đòn chí mạng (số bay lên to hơn, rung mạnh hơn).")]
    public bool crit;
    [Tooltip("Quật ngã: văng lên, nằm đất một lúc (bất tử khi nằm) rồi đứng dậy.")]
    public bool knockdown;

    public Hit(int amount, Vector3 source, float knockback = 1f, float stun = 0f)
    {
        this.amount = amount;
        this.source = source;
        this.knockback = knockback;
        this.stun = stun;
        crit = false;
        knockdown = false;
    }
}

/// <summary>
/// Máu dùng chung cho người chơi và quái. Sau mỗi lần trúng đòn có thể bất tử ngắn (invulnerableTime)
/// để không bị trừ máu liên tục mỗi frame khi còn chạm nguồn sát thương.
/// Chiêu thức có thể bật bất tử tạm (lướt, tàng hình) bằng <see cref="SetInvulnerable"/>.
/// Giáp (defense) giảm sát thương theo <see cref="ZodiacBalance.Mitigate"/>; người chơi lấy giáp từ <see cref="CharacterStats"/>.
/// </summary>
public class Health : MonoBehaviour
{
    [Min(1)] public int maxHealth = 3;
    [Tooltip("Không nhận thêm sát thương trong khoảng này sau mỗi lần trúng đòn (giây). 0 = tắt.")]
    [Min(0f)] public float invulnerableTime = 0f;
    [Tooltip("Số lần được giữ lại 1 máu thay vì chết (nội tại \"Chín mạng\" của Mèo).")]
    [Min(0)] public int lastStandCharges = 0;
    [Tooltip("Giáp: 100 giáp = giảm một nửa sát thương.")]
    [Min(0)] public int defense = 0;

    public int Current { get; private set; }
    public bool IsDead => Current <= 0;
    public bool IsInvulnerable => Time.time < invulnerableUntil;
    public float Normalized => (float)Current / maxHealth;

    /// <summary>Máu thay đổi (trúng đòn, hồi máu hoặc khởi tạo).</summary>
    public event Action Changed;
    /// <summary>Trúng đòn (đã trừ máu; hit.amount là lượng máu thực mất sau giáp) — dùng để đẩy lùi, choáng, nháy màu.</summary>
    public event Action<Hit> Damaged;
    public event Action Died;
    /// <summary>Nội tại giữ mạng vừa kích hoạt.</summary>
    public event Action LastStand;
    /// <summary>Đòn bị chặn (khiên) — không mất máu.</summary>
    public event Action<Hit> Blocked;

    /// <summary>Trả về true nếu đòn này bị chặn (quái cầm khiên đỡ đòn phía trước).</summary>
    public Func<Hit, bool> Blocks;

    float invulnerableUntil = float.NegativeInfinity;
    float vulnerableUntil = float.NegativeInfinity, vulnerableScale = 1f;

    /// <summary>Nhận thêm sát thương (×scale) trong seconds giây (tuyệt kỹ của Gà).</summary>
    public void MakeVulnerable(float seconds, float scale)
    {
        vulnerableUntil = Mathf.Max(vulnerableUntil, Time.time + seconds);
        vulnerableScale = Mathf.Max(1f, scale);
    }

    void Awake() => Current = maxHealth;

    /// <summary>Đặt lại máu tối đa (khi đổi nhân vật) và hồi đầy.</summary>
    public void Configure(int max)
    {
        maxHealth = Mathf.Max(1, max);
        Current = maxHealth;
        Changed?.Invoke();
    }

    /// <summary>
    /// Đổi máu tối đa giữa màn (trang bị, nâng cấp): tăng thì máu hiện tại tăng theo phần chênh lệch,
    /// giảm thì chỉ cắt phần vượt quá tối đa mới.
    /// </summary>
    public void SetMax(int max)
    {
        max = Mathf.Max(1, max);
        if (max == maxHealth) return;
        int gained = Mathf.Max(0, max - maxHealth);
        maxHealth = max;
        if (!IsDead) Current = Mathf.Clamp(Current + gained, 1, maxHealth);
        Changed?.Invoke();
    }

    public bool TakeDamage(int amount, Vector3 sourcePosition) => TakeDamage(new Hit(amount, sourcePosition));

    /// <summary>Trừ máu. Trả về false nếu đã chết, đang bất tử hoặc amount ≤ 0.</summary>
    public bool TakeDamage(Hit hit)
    {
        if (IsDead || IsInvulnerable || hit.amount <= 0) return false;
        if (Blocks != null && Blocks(hit))
        {
            Blocked?.Invoke(hit);
            return false;
        }
        if (Time.time < vulnerableUntil) hit.amount = Mathf.RoundToInt(hit.amount * vulnerableScale);
        hit.amount = ZodiacBalance.Mitigate(hit.amount, defense);
        Current = Mathf.Max(0, Current - hit.amount);
        bool lastStand = Current == 0 && lastStandCharges > 0;
        if (lastStand)
        {
            lastStandCharges--;
            Current = 1;
        }
        invulnerableUntil = Time.time + (lastStand ? Mathf.Max(invulnerableTime, 1.5f) : invulnerableTime);
        Changed?.Invoke();
        Damaged?.Invoke(hit);
        if (lastStand) LastStand?.Invoke();
        if (IsDead) Died?.Invoke();
        return true;
    }

    /// <summary>Hồi máu, không vượt quá tối đa. Trả về lượng máu thực sự hồi được.</summary>
    public int Heal(int amount)
    {
        if (IsDead || amount <= 0) return 0;
        int before = Current;
        Current = Mathf.Min(maxHealth, Current + amount);
        if (Current != before) Changed?.Invoke();
        return Current - before;
    }

    /// <summary>Bất tử trong ít nhất duration giây kể từ bây giờ (không rút ngắn bất tử đang có).</summary>
    public void SetInvulnerable(float duration) =>
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + duration);
}
