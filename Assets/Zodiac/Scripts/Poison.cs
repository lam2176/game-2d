using UnityEngine;

/// <summary>
/// Trạng thái nhiễm độc (nội tại của Rắn): mỗi tickInterval trừ 1 máu, không đẩy lùi, cho tới khi hết số lần.
/// Trúng độc lần nữa thì làm mới số lần trừ máu. Thân quái ánh xanh lục trong lúc nhiễm độc.
/// </summary>
[RequireComponent(typeof(Health))]
public class Poison : MonoBehaviour
{
    static readonly Color PoisonTint = new Color(0.6f, 1f, 0.5f);

    public float tickInterval = 0.8f;
    int ticksLeft;
    float nextTick;
    Health health;
    HitFlash flash;

    public static void Apply(Health target, int ticks)
    {
        if (target == null || target.IsDead || ticks <= 0) return;
        if (!target.TryGetComponent(out Poison poison)) poison = target.gameObject.AddComponent<Poison>();
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
        health.TakeDamage(new Hit(1, transform.position, 0f));
    }
}
