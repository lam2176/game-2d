using System;
using UnityEngine;

/// <summary>
/// Chỉ số cơ bản: HP, KI, sức mạnh, tiềm năng. Dùng chung cho người chơi và quái.
/// </summary>
public class CharacterStats : MonoBehaviour
{
    [Header("Chỉ số gốc")]
    public int maxHp = 100;
    public int maxKi = 100;
    public int damage = 10;

    [Header("Hồi phục mỗi giây")]
    public float kiRegenPerSecond = 5f;

    [Header("Sức mạnh / tiềm năng")]
    public long power = 1000;
    public long potential = 0;
    [Tooltip("Tiềm năng nhận được khi giết đối tượng này")]
    public long potentialReward = 50;

    public int Hp { get; private set; }
    public float Ki { get; private set; }
    public bool IsDead => Hp <= 0;

    public event Action<CharacterStats> OnChanged;
    public event Action OnDamaged;
    public event Action<CharacterStats, CharacterStats> OnDied; // (nạn nhân, kẻ giết)

    void Awake()
    {
        Hp = maxHp;
        Ki = maxKi;
    }

    void Update()
    {
        if (IsDead || Ki >= maxKi) return;
        Ki = Mathf.Min(maxKi, Ki + kiRegenPerSecond * Time.deltaTime);
        OnChanged?.Invoke(this);
    }

    public bool TrySpendKi(float amount)
    {
        if (Ki < amount) return false;
        Ki -= amount;
        OnChanged?.Invoke(this);
        return true;
    }

    public void TakeDamage(int amount, CharacterStats attacker)
    {
        if (IsDead) return;
        Hp = Mathf.Max(0, Hp - amount);
        OnDamaged?.Invoke();
        OnChanged?.Invoke(this);

        if (IsDead)
        {
            if (attacker != null) attacker.GainPotential(potentialReward);
            OnDied?.Invoke(this, attacker);
        }
    }

    public void Heal(int hp, int ki)
    {
        if (IsDead) return;
        Hp = Mathf.Min(maxHp, Hp + hp);
        Ki = Mathf.Min(maxKi, Ki + ki);
        OnChanged?.Invoke(this);
    }

    public void Revive()
    {
        Hp = maxHp;
        Ki = maxKi;
        OnChanged?.Invoke(this);
    }

    public void GainPotential(long amount)
    {
        potential += amount;
        power += amount;
        OnChanged?.Invoke(this);
    }

    /// <summary>Dùng tiềm năng để tăng chỉ số (giống cộng điểm).</summary>
    public bool UpgradeStat(string stat, long cost)
    {
        if (potential < cost) return false;
        potential -= cost;
        switch (stat)
        {
            case "hp": maxHp += 20; Hp += 20; break;
            case "ki": maxKi += 20; break;
            case "damage": damage += 2; break;
            default: potential += cost; return false;
        }
        OnChanged?.Invoke(this);
        return true;
    }
}
