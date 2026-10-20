using System;
using UnityEngine;

/// <summary>
/// Thanh "Khí" (tham khảo Soul của Hollow Knight): đánh trúng quái thì nạp, tung chiêu thì tốn.
/// Khuyến khích đánh áp sát thay vì đứng xa chờ hồi chiêu. Tự hồi rất chậm để không bao giờ kẹt hẳn.
/// Vào màn với thanh đầy.
/// </summary>
public class PlayerEnergy : MonoBehaviour
{
    [Min(1)] public int max = 6;
    [Tooltip("Khí nạp mỗi đòn đánh thường trúng (đòn kết combo, đạp đầu nạp thêm 1).")]
    [Min(0)] public int gainPerHit = 1;
    [Tooltip("Tự hồi 1 Khí sau mỗi khoảng này (giây). 0 = tắt.")]
    [Min(0f)] public float regenInterval = 6f;

    public int Current { get; private set; }
    public float Normalized => (float)Current / max;
    /// <summary>Khí thay đổi.</summary>
    public event Action Changed;

    float nextRegen;

    void Awake()
    {
        Current = max;
        nextRegen = Time.time + regenInterval;
    }

    void Update()
    {
        if (regenInterval <= 0f || Time.time < nextRegen) return;
        nextRegen = Time.time + regenInterval;
        if (Current < max) Gain(1);
    }

    public void Gain(int amount)
    {
        if (amount <= 0 || Current >= max) return;
        Current = Mathf.Min(max, Current + amount);
        Changed?.Invoke();
    }

    public bool CanSpend(int amount) => Current >= amount;

    /// <summary>Trừ Khí nếu đủ. Trả về false (không trừ) khi thiếu.</summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (Current < amount) return false;
        Current -= amount;
        nextRegen = Time.time + regenInterval;
        Changed?.Invoke();
        return true;
    }
}
