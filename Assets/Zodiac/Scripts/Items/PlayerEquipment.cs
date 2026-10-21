using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gắn trên người chơi: áp chỉ số của đồ đang mặc (<see cref="PlayerProfile.equipped"/>) vào <see cref="CharacterStats"/>
/// mỗi khi đồ thay đổi, và dùng nhanh đồ dùng bằng phím 1 / 2 (action Player/Previous, Player/Next).
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class PlayerEquipment : MonoBehaviour
{
    CharacterStats stats;
    Health health;
    PlayerEnergy energy;
    ZodiacPlayerController controller;
    InputAction quick1, quick2;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
        health = GetComponent<Health>();
        energy = GetComponent<PlayerEnergy>();
        controller = GetComponent<ZodiacPlayerController>();
        quick1 = InputSystem.actions.FindAction("Player/Previous");
        quick2 = InputSystem.actions.FindAction("Player/Next");
    }

    void OnEnable()
    {
        PlayerProfile.Changed += Apply;
        quick1?.Enable();
        quick2?.Enable();
    }

    void OnDisable() => PlayerProfile.Changed -= Apply;

    // Start: sau khi controller đã đặt chỉ số gốc theo nhân vật (Awake)
    void Start()
    {
        Apply();
        // Mặc đồ làm máu tối đa tăng → vào màn vẫn đầy máu
        if (health != null) health.Heal(health.maxHealth);
        if (energy != null) energy.Gain(energy.max);
    }

    void Apply()
    {
        if (stats == null) return;
        stats.RemoveModifiersFrom(this);
        foreach (var e in PlayerProfile.Current.equipped)
        {
            var def = e.Def;
            if (def == null) continue;
            foreach (var m in def.modifiers) stats.AddModifier(m, this);
        }
    }

    void Update()
    {
        if (controller != null && !controller.ControlEnabled) return;
        if (quick1 != null && quick1.WasPressedThisFrame()) UseQuick(0);
        if (quick2 != null && quick2.WasPressedThisFrame()) UseQuick(1);
    }

    public void UseQuick(int index)
    {
        var profile = PlayerProfile.Current;
        string id = profile.quick[index];
        if (string.IsNullOrEmpty(id)) return;
        if (profile.Use(id, health, energy))
        {
            var res = ZodiacResources.Get;
            if (res != null) ZodiacAudio.Play(res.coin, 0.6f, 0.05f);
            Vector3 at = controller != null ? controller.BodyCenter : transform.position + Vector3.up;
            var def = ItemDatabase.Get(id);
            ZodiacParticles.Sparkle(at, def != null && def.heal > 0 ? new Color(0.5f, 1f, 0.6f) : new Color(0.5f, 0.85f, 1f), 16, 0.6f);
        }
    }
}
