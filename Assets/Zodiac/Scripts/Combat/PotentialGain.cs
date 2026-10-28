using UnityEngine;

/// <summary>
/// Tiềm năng kiểu Ngọc Rồng Online: đánh trúng quái là nhận tiềm năng (XP) theo sát thương gây ra, không đợi hạ quái.
/// Gom lại rồi cộng vào hồ sơ mỗi <see cref="FlushInterval"/> giây (mỗi lần cộng là một lần lưu file) và hiện "+N TN"
/// màu xanh trên đầu nhân vật. Quy đổi khớp với cách tính cũ: đánh hết máu một con quái = đúng số XP khi hạ nó.
/// </summary>
public class PotentialGain : MonoBehaviour
{
    const float FlushInterval = 1.2f;

    static PotentialGain instance;
    static float pending;   // tiềm năng chưa cộng (lẻ)

    float nextFlush;

    /// <summary>Quái mất amount máu (đã nhân độ khó: rewardScale nhân thưởng).</summary>
    public static void FromDamage(int amount, float rewardScale)
    {
        if (amount <= 0) return;
        pending += amount * 5f / ZodiacBalance.Unit * rewardScale;   // khớp Progression.XpFor: 5 XP mỗi Unit máu
        Ensure();
    }

    /// <summary>Thưởng thêm khi hạ quái (boss, quái có XP đặt riêng).</summary>
    public static void Bonus(int amount)
    {
        if (amount <= 0) return;
        pending += amount;
        Ensure();
    }

    /// <summary>Cộng ngay phần đã gom vào con giáp đang chơi.</summary>
    public static void Flush()
    {
        int whole = Mathf.FloorToInt(pending);
        var player = ZodiacPlayerController.Current;
        if (whole <= 0 || player == null || player.Character == null) return;
        pending -= whole;
        PlayerProfile.Current.AddXp(player.Character.name, whole);
        DamageNumbers.Show(player.transform.position + Vector3.up * 2.4f, $"+{DamageNumbers.Format(whole)} TN", DamageNumbers.Potential, 0.8f);
    }

    static void Ensure()
    {
        if (instance != null) return;
        var go = new GameObject("PotentialGain");
        instance = go.AddComponent<PotentialGain>();   // nằm trong scene hiện tại — đổi màn thì OnDestroy cộng nốt
    }

    void Update()
    {
        if (Time.unscaledTime < nextFlush) return;
        nextFlush = Time.unscaledTime + FlushInterval;
        Flush();
    }

    void OnDestroy()
    {
        Flush();
        if (instance == this) instance = null;
    }
}
