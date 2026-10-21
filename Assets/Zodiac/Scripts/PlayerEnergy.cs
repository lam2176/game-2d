using System;
using UnityEngine;

/// <summary>
/// Thanh "Khí" (tham khảo Soul của Hollow Knight): đánh trúng quái thì nạp, tung chiêu thì tốn.
/// Khuyến khích đánh áp sát thay vì đứng xa chờ hồi chiêu. Tự hồi chậm (ngừng một lúc sau khi tiêu)
/// để không bao giờ kẹt hẳn. Vào màn với thanh đầy.
/// Khí tối đa và tốc độ hồi do <see cref="CharacterStats"/> đặt (nâng được bằng trang bị, lên cấp, Khí Đan).
/// </summary>
public class PlayerEnergy : MonoBehaviour
{
    [Min(1)] public int max = ZodiacBalance.BaseEnergy;
    [Tooltip("Khí nạp mỗi đòn đánh thường trúng (đòn kết combo, đạp đầu nạp gấp đôi).")]
    [Min(0)] public int hitGain = 16;
    [Tooltip("Khí tự hồi mỗi giây. 0 = tắt.")]
    [Min(0f)] public float regenPerSecond = 2.5f;
    [Tooltip("Ngừng tự hồi trong khoảng này sau khi tiêu Khí (giây).")]
    [Min(0f)] public float regenDelay = 2f;

    public int Current { get; private set; }
    public float Normalized => (float)Current / max;
    /// <summary>Khí thay đổi.</summary>
    public event Action Changed;

    float regenResumeTime, regenBuffer;

    void Awake() => Current = max;

    void Update()
    {
        if (regenPerSecond <= 0f || Current >= max || Time.time < regenResumeTime)
        {
            regenBuffer = 0f;
            return;
        }
        regenBuffer += regenPerSecond * Time.deltaTime;
        int whole = Mathf.FloorToInt(regenBuffer);
        if (whole <= 0) return;
        regenBuffer -= whole;
        Gain(whole);
    }

    /// <summary>Đặt Khí tối đa (khi đổi nhân vật) và nạp đầy.</summary>
    public void Configure(int newMax)
    {
        max = Mathf.Max(1, newMax);
        Current = max;
        Changed?.Invoke();
    }

    /// <summary>Đổi Khí tối đa giữa màn: tăng thì nạp thêm phần chênh lệch, giảm thì cắt phần vượt.</summary>
    public void SetMax(int newMax)
    {
        newMax = Mathf.Max(1, newMax);
        if (newMax == max) return;
        int gained = Mathf.Max(0, newMax - max);
        max = newMax;
        Current = Mathf.Min(max, Current + gained);
        Changed?.Invoke();
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
        regenResumeTime = Time.time + regenDelay;
        Changed?.Invoke();
        return true;
    }
}
