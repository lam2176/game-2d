using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ô chiêu thức trên HUD: tên chiêu + phím, lớp phủ tối co dần theo thời gian hồi chiêu, viền sáng khi sẵn sàng.
/// Kèm dòng nội tại của nhân vật.
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
        if (nameText != null) nameText.text = $"[K] {c.skill.name}";
        if (passiveText != null) passiveText.text = $"Nội tại — {c.passiveName}";
        if (characterText != null) characterText.text = $"{c.displayName} · Tuổi {c.zodiacName}";
        if (readyFrame != null) readyFrame.color = c.themeColor;
    }

    void Update()
    {
        if (skill == null) return;
        float n = skill.CooldownNormalized;
        if (cooldownFill != null) cooldownFill.localScale = new Vector3(1f, skill.IsBusy ? 1f : n, 1f);
        if (cooldownText != null)
            cooldownText.text = skill.IsReady ? "" : skill.IsBusy ? "…" : skill.CooldownRemaining.ToString("0.0");
        if (readyFrame != null) readyFrame.enabled = skill.IsReady;
    }
}
