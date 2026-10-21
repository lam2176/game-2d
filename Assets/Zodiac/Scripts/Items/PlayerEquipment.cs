using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gắn trên người chơi: áp chỉ số của đồ đang mặc (cấp đập + dòng phụ) và tu luyện của con giáp đang chơi (điểm cộng, Đan)
/// vào <see cref="CharacterStats"/> mỗi khi hồ sơ thay đổi; dùng nhanh đồ bằng phím 1 / 2 (action Player/Previous, Player/Next);
/// lên cấp thì hồi đầy máu, Khí và báo lên màn hình.
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

    /// <summary>Mã con giáp đang chơi (tên asset nhân vật) — khoá tiến trình trong hồ sơ.</summary>
    public string CharacterKey => controller != null && controller.Character != null ? controller.Character.name : "";

    void OnEnable()
    {
        PlayerProfile.Changed += Apply;
        PlayerProfile.LeveledUp += OnLeveledUp;
        quick1?.Enable();
        quick2?.Enable();
    }

    void OnDisable()
    {
        PlayerProfile.Changed -= Apply;
        PlayerProfile.LeveledUp -= OnLeveledUp;
    }

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
        var profile = PlayerProfile.Current;
        foreach (var e in profile.equipped)
            foreach (var m in e.EffectiveModifiers()) stats.AddModifier(m, this);
        if (!string.IsNullOrEmpty(CharacterKey))
            foreach (var m in Progression.Modifiers(profile.Progress(CharacterKey))) stats.AddModifier(m, this);
    }

    void OnLeveledUp(CharacterProgress p)
    {
        if (p.character != CharacterKey) return;
        if (health != null) health.Heal(health.maxHealth);
        if (energy != null) energy.Gain(energy.max);
        Vector3 at = controller != null ? controller.BodyCenter : transform.position + Vector3.up;
        ZodiacFx.Ring(transform.position, 2.5f, new Color(1f, 0.85f, 0.3f, 0.9f), 0.6f, flat: true);
        ZodiacParticles.Sparkle(at, new Color(1f, 0.9f, 0.4f), 30, 0.9f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.win, 0.7f, 0f);
        PickupToast.Show($"LÊN CẤP {p.level}!  +{CharacterProgress.PointsPerLevel} điểm tu luyện (I → Tu luyện)", new Color(1f, 0.85f, 0.3f));
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
