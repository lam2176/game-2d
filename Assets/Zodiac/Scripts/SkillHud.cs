using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ô chiêu thức trên HUD: tên chiêu + phím, lớp phủ tối co dần theo thời gian hồi chiêu, viền sáng khi sẵn sàng.
/// Kèm dòng nội tại của nhân vật và các ô Khí (sáng = còn, tối = đã dùng; các ô sẽ bị tốn khi tung chiêu nhấp nháy).
/// </summary>
public class SkillHud : MonoBehaviour
{
    public PlayerSkill skill;
    public Image icon;
    [Tooltip("Lớp phủ tối, co trục Y theo phần trăm hồi chiêu (pivot dưới).")]
    public RectTransform cooldownFill;
    public Text nameText;
    public Text cooldownText;
    public Text passiveText;
    public Text characterText;
    public Image readyFrame;
    public PlayerEnergy energy;
    [Tooltip("Mỗi ô ứng với 1 Khí.")]
    public Image[] energyPips;
    public Color energyFull = new Color(0.45f, 0.85f, 1f);
    public Color energyEmpty = new Color(0.15f, 0.2f, 0.3f, 0.8f);

    void Start()
    {
        var player = skill != null ? skill.GetComponent<ZodiacPlayerController>() : null;
        var c = player != null ? player.Character : null;
        if (c == null) return;
        if (icon != null)
        {
            icon.sprite = c.sprite;
            icon.preserveAspect = true;
        }
        if (nameText != null) nameText.text = $"[K] {c.skill.name}  ·  {c.skill.energyCost} Khí";
        if (passiveText != null) passiveText.text = $"Nội tại — {c.passiveName}";
        if (characterText != null) characterText.text = $"{c.displayName} · Tuổi {c.zodiacName}";
        if (readyFrame != null) readyFrame.color = c.themeColor;
    }

    void Update()
    {
        if (skill == null) return;
        float n = skill.CooldownNormalized;
        if (cooldownFill != null) cooldownFill.localScale = new Vector3(1f, skill.IsBusy ? 1f : n, 1f);
        bool enough = skill.HasEnergy;
        if (cooldownText != null)
            cooldownText.text = skill.IsBusy ? "…" : !skill.IsReady ? skill.CooldownRemaining.ToString("0.0") : enough ? "" : "Khí";
        if (readyFrame != null) readyFrame.enabled = skill.IsReady && enough;
        if (icon != null) icon.color = enough ? Color.white : new Color(0.45f, 0.45f, 0.5f);

        if (energy == null || energyPips == null) return;
        int cost = skill.Config != null ? skill.Config.energyCost : 0;
        for (int i = 0; i < energyPips.Length; i++)
        {
            if (energyPips[i] == null) continue;
            bool filled = i < energy.Current;
            // Ô sẽ bị tốn nếu tung chiêu: nhấp nháy nhẹ
            bool willSpend = filled && enough && i >= energy.Current - cost;
            float pulse = willSpend ? 0.75f + 0.25f * Mathf.Sin(Time.time * 6f) : 1f;
            energyPips[i].color = filled ? new Color(energyFull.r * pulse, energyFull.g * pulse, energyFull.b * pulse, 1f) : energyEmpty;
        }
    }
}
