using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Màn hình chính: lưới 12 con giáp (chuột hoặc phím mũi tên / tay cầm để chọn), bảng thông tin nhân vật
/// (chỉ số, nội tại, chiêu thức), nút Bắt đầu vào màn 1 và nút Thoát.
/// Ô đang được chọn (EventSystem) chính là nhân vật sẽ chơi — lưu vào <see cref="ZodiacSelection"/>.
/// </summary>
public class ZodiacStartMenu : MonoBehaviour
{
    public string levelSceneName = "Zodiac_Level01";
    public ZodiacRoster roster;

    [Header("Lưới chọn")]
    [Tooltip("Mỗi ô ứng với roster.characters[i].")]
    public Button[] slots;
    [Tooltip("Khung viền của từng ô — sáng màu chủ đạo khi được chọn.")]
    public Image[] slotFrames;

    [Header("Bảng thông tin")]
    public Image portrait;
    public Text nameText;
    public Text instrumentText;
    public Text statsText;
    public Text passiveText;
    public Text skillText;

    int shown = -1;

    void Start()
    {
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.PlayMusic(res.musicMenu);
        if (roster == null || roster.Count == 0) return;
        int index = Mathf.Clamp(ZodiacSelection.Index, 0, roster.Count - 1);
        if (slots != null && index < slots.Length && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(slots[index].gameObject);
        Show(index);
    }

    void Update()
    {
        if (slots == null || EventSystem.current == null) return;
        var selected = EventSystem.current.currentSelectedGameObject;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null && slots[i].gameObject == selected && i != shown) Show(i);
    }

    /// <summary>Gọi từ nút ô nhân vật (click).</summary>
    public void Choose(int index)
    {
        if (EventSystem.current != null && index < slots.Length) EventSystem.current.SetSelectedGameObject(slots[index].gameObject);
        Show(index);
    }

    void Show(int index)
    {
        var c = roster.Get(index);
        if (c == null) return;
        shown = index;
        ZodiacSelection.Index = index;

        if (slotFrames != null)
            for (int i = 0; i < slotFrames.Length; i++)
                if (slotFrames[i] != null)
                    slotFrames[i].color = i == index ? c.themeColor : new Color(0.35f, 0.2f, 0.12f, 0.6f);

        if (portrait != null)
        {
            portrait.sprite = c.sprite;
            portrait.preserveAspect = true;
        }
        if (nameText != null)
        {
            nameText.text = c.Title;
            nameText.color = Color.Lerp(c.themeColor, Color.black, 0.35f);
        }
        if (instrumentText != null) instrumentText.text = string.IsNullOrEmpty(c.instrument) ? "" : $"Nhạc cụ: {c.instrument}";
        if (statsText != null)
            statsText.text = $"Máu {Bar(c.maxHealth, 8)}   Tốc độ {Bar(Mathf.RoundToInt(c.runSpeed - 4f), 8)}\n" +
                             $"Nhảy {Bar(Mathf.RoundToInt((c.jumpHeight - 2f) * 2f), 8)}   Sức đánh {Bar(c.attackDamage * 2, 8)}";
        if (passiveText != null) passiveText.text = $"<b>Nội tại — {c.passiveName}</b>\n{c.passiveDescription}";
        if (skillText != null)
            skillText.text = $"<b>Chiêu [E] — {c.skill.name}</b>  <i>({c.skill.energyCost} Khí, hồi {c.skill.cooldown:0.#}s)</i>\n{c.skill.description}\n" +
                             $"<b>Tuyệt kỹ [R] — {c.ultimateName}</b>  <i>(đầy 6 Khí)</i>\n{c.ultimateDescription}";
    }

    static string Bar(int value, int max)
    {
        value = Mathf.Clamp(value, 0, max);
        return new string('■', value) + new string('□', max - value);
    }

    public void Play() => ScreenFader.LoadScene(levelSceneName);

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
