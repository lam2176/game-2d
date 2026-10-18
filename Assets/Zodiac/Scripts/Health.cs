using System;
using UnityEngine;

/// <summary>
/// Máu dùng chung cho người chơi và quái. Sau mỗi lần trúng đòn có thể bất tử ngắn (invulnerableTime)
/// để không bị trừ máu liên tục mỗi frame khi còn chạm nguồn sát thương.
/// </summary>
public class Health : MonoBehaviour
{
    [Min(1)] public int maxHealth = 3;
    [Tooltip("Không nhận thêm sát thương trong khoảng này sau mỗi lần trúng đòn (giây). 0 = tắt.")]
    [Min(0f)] public float invulnerableTime = 0f;

    public int Current { get; private set; }
    public bool IsDead => Current <= 0;
    public bool IsInvulnerable => Time.time < invulnerableUntil;
    public float Normalized => (float)Current / maxHealth;

    /// <summary>Máu thay đổi (trúng đòn hoặc khởi tạo).</summary>
    public event Action Changed;
    /// <summary>(sát thương, vị trí nguồn gây sát thương) — dùng để đẩy lùi ra xa nguồn.</summary>
    public event Action<int, Vector2> Damaged;
    public event Action Died;

    float invulnerableUntil = float.NegativeInfinity;

    void Awake() => Current = maxHealth;

    /// <summary>Trừ máu. Trả về false nếu đã chết, đang bất tử hoặc amount ≤ 0.</summary>
    public bool TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (IsDead || IsInvulnerable || amount <= 0) return false;
        Current = Mathf.Max(0, Current - amount);
        invulnerableUntil = Time.time + invulnerableTime;
        Changed?.Invoke();
        Damaged?.Invoke(amount, sourcePosition);
        if (IsDead) Died?.Invoke();
        return true;
    }
}
