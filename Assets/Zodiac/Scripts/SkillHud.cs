using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD chiêu thức kiểu game hành động online (Ngọc Rồng…): thanh 4 ô tròn giữa đáy màn hình —
/// Q Đánh · W Lăn · E Chiêu · R Tuyệt kỹ (chữ phím theo phím đã đổi / tay cầm). Mỗi ô có biểu tượng, phím, số Khí cần,
/// lớp tối quét tròn theo thời gian hồi chiêu, nảy lên khi bấm; ô Tuyệt kỹ viền vàng, sáng nhấp nháy khi đầy Khí.
/// Góc trái trên: chân dung tròn, tên, nội tại, thanh máu (HealthBar riêng) và các ô Khí.
/// </summary>
public class SkillHud : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public RectTransform root;
        public Image icon;
        [Tooltip("Image kiểu Filled / Radial 360 phủ lên biểu tượng.")]
        public Image cooldown;
        public Image ring;
        public Text key;
        public Text label;
        public Text cost;
        [NonSerialized] public float punch;
    }

    public PlayerSkill skill;
    public PlayerMeleeAttack attack;
    public PlayerDodge dodge;
    public PlayerEnergy energy;

    [Header("Thanh chiêu")]
    public Slot attackSlot;
    public Slot dodgeSlot;
    public Slot skillSlot;
    public Slot ultimateSlot;

    [Header("Thông tin nhân vật (góc trái trên)")]
    public Image portrait;
    public Image portraitRing;
    public Text characterText;
    public Text passiveText;
    [Tooltip("Các ô chia đều thanh Khí (ô đang nạp dở sáng mờ).")]
    public Image[] energyPips;
    public Color energyFull = new Color(0.45f, 0.85f, 1f);
    public Color energyEmpty = new Color(0.15f, 0.2f, 0.3f, 0.8f);

    static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
    static readonly Color RingIdle = new Color(0.85f, 0.85f, 0.9f, 0.9f);
    ZodiacCharacter character;

    void Start()
    {
        var player = skill != null ? skill.GetComponent<ZodiacPlayerController>() : null;
        character = player != null ? player.Character : null;
        if (character == null) return;

        if (portrait != null) { portrait.sprite = character.sprite; portrait.preserveAspect = true; }
        if (portraitRing != null) portraitRing.color = character.themeColor;
        if (characterText != null) characterText.text = $"{character.displayName} · Tuổi {character.zodiacName}";
        if (passiveText != null) passiveText.text = $"Nội tại — {character.passiveName}";

        Setup(attackSlot, ZodiacFx.Crescent, ZodiacInput.Key("Attack"), "Đánh", "");
        Setup(dodgeSlot, ZodiacFx.Chevrons, ZodiacInput.Key("Dodge"), "Lăn", "");
        Setup(skillSlot, character.sprite, ZodiacInput.Key("Skill"), character.skill.name, $"{character.skill.energyCost}");
        Setup(ultimateSlot, character.sprite, ZodiacInput.Key("Ultimate"), character.ultimateName, energy != null ? $"{energy.max}" : "");
        if (ultimateSlot.icon != null) ultimateSlot.icon.color = new Color(1f, 0.9f, 0.6f);

        if (attack != null) attack.Swung += (_, __) => attackSlot.punch = 1f;
        if (dodge != null) dodge.Dodged += _ => dodgeSlot.punch = 1f;
        if (skill != null)
        {
            skill.Used += _ => skillSlot.punch = 1f;
            skill.UltimateUsed += () => ultimateSlot.punch = 1.5f;
        }
    }

    void OnEnable() => ZodiacInput.Changed += RefreshKeys;
    void OnDisable() => ZodiacInput.Changed -= RefreshKeys;

    /// <summary>Chữ phím trên 4 ô theo phím đã đổi / thiết bị đang dùng.</summary>
    void RefreshKeys()
    {
        SetKey(attackSlot, "Attack");
        SetKey(dodgeSlot, "Dodge");
        SetKey(skillSlot, "Skill");
        SetKey(ultimateSlot, "Ultimate");
    }

    static void SetKey(Slot s, string action)
    {
        if (s != null && s.key != null) s.key.text = ZodiacInput.Key(action);
    }

    static void Setup(Slot s, Sprite icon, string key, string label, string cost)
    {
        if (s == null || s.root == null) return;
        if (s.icon != null) { s.icon.sprite = icon; s.icon.preserveAspect = true; }
        if (s.key != null) { s.key.text = key; s.key.horizontalOverflow = HorizontalWrapMode.Overflow; }   // "Space", "Shift"… dài hơn ô
        if (s.label != null) s.label.text = label;
        if (s.cost != null)
        {
            s.cost.text = cost;
            s.cost.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(cost));
        }
    }

    void Update()
    {
        if (skill == null) return;
        bool enough = skill.HasEnergy;

        UpdateSlot(attackSlot, attack != null ? attack.CooldownNormalized : 0f, true, RingIdle);
        UpdateSlot(dodgeSlot, dodge != null ? dodge.CooldownNormalized : 0f, dodge == null || dodge.IsReady, RingIdle);
        float skillCd = skill.IsBusy ? 1f : skill.CooldownNormalized;
        UpdateSlot(skillSlot, skillCd, skill.IsReady && enough, character != null ? character.themeColor : RingIdle);
        if (skillSlot.icon != null) skillSlot.icon.color = enough ? Color.white : new Color(0.45f, 0.45f, 0.5f);

        // Tuyệt kỹ: lớp tối = phần Khí còn thiếu; đầy Khí thì viền vàng sáng nhấp nháy
        float missing = energy != null ? 1f - energy.Normalized : 0f;
        bool ultReady = skill.UltimateReady;
        Color gold = ultReady ? Color.Lerp(Gold, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)) : Gold * 0.6f;
        UpdateSlot(ultimateSlot, missing, ultReady, gold);
        if (ultReady && ultimateSlot.root != null)
            ultimateSlot.root.localScale *= 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 6f);

        if (energy == null || energyPips == null) return;
        int cost = skill.Config != null ? skill.Config.energyCost : 0;
        float segment = (float)energy.max / energyPips.Length;   // mỗi ô = một đoạn bằng nhau của thanh Khí
        for (int i = 0; i < energyPips.Length; i++)
        {
            if (energyPips[i] == null) continue;
            float fill = Mathf.Clamp01((energy.Current - i * segment) / segment);
            bool willSpend = fill > 0f && enough && (i + 1) * segment > energy.Current - cost;   // các ô sẽ bị tốn khi tung chiêu: nhấp nháy
            float pulse = willSpend ? 0.75f + 0.25f * Mathf.Sin(Time.time * 6f) : 1f;
            var full = new Color(energyFull.r * pulse, energyFull.g * pulse, energyFull.b * pulse, 1f);
            energyPips[i].color = fill >= 1f ? full : Color.Lerp(energyEmpty, full, fill * 0.6f);
        }
    }

    static void UpdateSlot(Slot s, float cooldown, bool ready, Color ringColor)
    {
        if (s == null || s.root == null) return;
        if (s.cooldown != null) s.cooldown.fillAmount = cooldown;
        if (s.ring != null) s.ring.color = ready ? ringColor : new Color(0.35f, 0.35f, 0.4f, 0.9f);
        s.punch = Mathf.MoveTowards(s.punch, 0f, Time.unscaledDeltaTime * 5f);
        float p = Mathf.Sin(Mathf.Min(1f, s.punch) * Mathf.PI) * 0.18f;
        s.root.localScale = Vector3.one * (1f + p);
    }
}
