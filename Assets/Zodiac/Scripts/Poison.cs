using UnityEngine;

/// <summary>
/// Trạng thái nhiễm độc (nội tại của Rắn): mỗi tickInterval trừ tickDamage máu, không đẩy lùi, cho tới khi hết số lần.
/// Trúng độc lần nữa thì làm mới số lần trừ máu. Thân quái ánh xanh lục trong lúc nhiễm độc.
/// </summary>
[RequireComponent(typeof(Health))]
public class Poison : MonoBehaviour
{
    static readonly Color PoisonTint = new Color(0.6f, 1f, 0.5f);

    public float tickInterval = 0.8f;
    public int tickDamage = ZodiacBalance.Unit;
    int ticksLeft;
    float nextTick;
    Health health;
    HitFlash flash;

    /// <param name="tickDamage">Sát thương mỗi lần (0 = giữ giá trị của component).</param>
    public static void Apply(Health target, int ticks, int tickDamage = 0)
    {
        if (target == null || target.IsDead || ticks <= 0) return;
        if (!target.TryGetComponent(out Poison poison)) poison = target.gameObject.AddComponent<Poison>();
        if (tickDamage > 0) poison.tickDamage = tickDamage;
        poison.ticksLeft = Mathf.Max(poison.ticksLeft, ticks);
        poison.nextTick = Time.time + poison.tickInterval;
        poison.enabled = true;
    }

    void Awake()
    {
        health = GetComponent<Health>();
        flash = GetComponent<HitFlash>();
    }

    void Update()
    {
        if (health.IsDead || ticksLeft <= 0)
        {
            if (flash != null) flash.tint = Color.white;
            enabled = false;
            return;
        }
        if (flash != null) flash.tint = PoisonTint;
        if (Time.time < nextTick) return;
        nextTick = Time.time + tickInterval;
        ticksLeft--;
        health.TakeDamage(new Hit(tickDamage, transform.position, 0f));
    }
}
