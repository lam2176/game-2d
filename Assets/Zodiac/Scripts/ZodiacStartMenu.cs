using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Màn hình chính: lưới 12 con giáp (chuột hoặc phím mũi tên / tay cầm để chọn), bảng thông tin nhân vật
/// (chỉ số, cấp của con đó, nội tại, chiêu thức), nút Bắt đầu (mở bảng chọn màn — màn chưa mở bị khoá),
/// nút Chơi mới (xoá hồ sơ, có hỏi lại) và nút Thoát. Bảng chọn màn / hỏi lại dựng bằng code lúc chạy.
/// Ô đang được chọn (EventSystem) chính là nhân vật sẽ chơi — lưu vào <see cref="ZodiacSelection"/>.
/// </summary>
public class ZodiacStartMenu : MonoBehaviour
{
    public string levelSceneName = "Zodiac_Level01";
    [Tooltip("Scene và tên các màn theo thứ tự (builder điền từ ZodiacLevelData).")]
    public string[] levelScenes = System.Array.Empty<string>();
    public string[] levelTitles = System.Array.Empty<string>();
    [Tooltip("Làng Trung Tâm — Bắt đầu vào thẳng làng (chọn màn ở Cổng làng).")]
    public string villageScene = "";
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
    GameObject levelPanel, confirmPanel;
    Font font;

    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildOverlay();
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
        if (SettingsWindow.IsOpen) return;
        if ((levelPanel != null && levelPanel.activeSelf) || (confirmPanel != null && confirmPanel.activeSelf))
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) CloseOverlays();
            return;
        }
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
        var progress = PlayerProfile.Current.Progress(c.name);
        if (instrumentText != null) instrumentText.text += $"   ·   <b>Cấp {progress.level}</b>";
        if (statsText != null)
            statsText.text = $"Máu {Bar(c.maxHealth / ZodiacBalance.Unit, 8)} {c.maxHealth}   Tốc độ {Bar(Mathf.RoundToInt(c.runSpeed - 4f), 8)}\n" +
                             $"Nhảy {Bar(Mathf.RoundToInt((c.jumpHeight - 2f) * 2f), 8)}   Sức đánh {Bar(c.attackDamage * 2 / ZodiacBalance.Unit, 8)} {c.attackDamage}";
        if (passiveText != null) passiveText.text = $"<b>Nội tại — {c.passiveName}</b>\n{c.passiveDescription}";
        if (skillText != null)
            skillText.text = $"<b>Chiêu [E] — {c.skill.name}</b>  <i>({c.skill.energyCost} Khí, hồi {c.skill.cooldown:0.#}s)</i>\n{c.skill.description}\n" +
                             $"<b>Tuyệt kỹ [R] — {c.ultimateName}</b>  <i>(đầy {c.maxEnergy} Khí)</i>\n{c.ultimateDescription}";
    }

    static string Bar(int value, int max)
    {
        value = Mathf.Clamp(value, 0, max);
        return new string('■', value) + new string('□', max - value);
    }

    /// <summary>Nút Bắt đầu: mở bảng chọn màn (chưa có danh sách màn thì vào thẳng màn 1).</summary>
    public void Play()
    {
        if (!string.IsNullOrEmpty(villageScene))
        {
            ScreenFader.LoadScene(villageScene);
            return;
        }
        if (levelScenes == null || levelScenes.Length == 0 || levelPanel == null)
        {
            ScreenFader.LoadScene(levelSceneName);
            return;
        }
        RebuildLevelButtons();
        levelPanel.SetActive(true);
    }

    void CloseOverlays()
    {
        if (levelPanel != null) levelPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (EventSystem.current != null && shown >= 0 && shown < slots.Length) EventSystem.current.SetSelectedGameObject(slots[shown].gameObject);
    }

    // ───────── Bảng chọn màn, chơi mới (dựng bằng code) ─────────

    Transform levelList;

    void BuildOverlay()
    {
        var canvasGo = new GameObject("MenuOverlay", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Nút Chơi mới (góc phải trên)
        var newGame = MakeButton(canvasGo.transform, "Chơi mới", new Vector2(1f, 1f), new Vector2(-150f, -60f), new Vector2(240f, 64f), AskNewGame);
        newGame.name = "NewGameButton";
        MakeButton(canvasGo.transform, "Cài đặt", new Vector2(1f, 1f), new Vector2(-410f, -60f), new Vector2(240f, 64f), OpenSettings).name = "SettingsButton";

        levelPanel = Overlay(canvasGo.transform, "LevelPanel", "CHỌN MÀN", 760f, 640f);
        var list = new GameObject("List", typeof(RectTransform));
        list.transform.SetParent(levelPanel.transform.Find("Box"), false);
        Place((RectTransform)list.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(680f, 420f));
        levelList = list.transform;
        MakeButton(levelPanel.transform.Find("Box"), "Quay lại", new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(260f, 64f), CloseOverlays);
        levelPanel.SetActive(false);

        confirmPanel = Overlay(canvasGo.transform, "ConfirmPanel", "CHƠI MỚI?", 760f, 420f);
        var box = confirmPanel.transform.Find("Box");
        var msg = MakeLabel(box, "Xoá toàn bộ đồ, tiền, cấp và màn đã mở của mọi con giáp?", 28, new Color(0.25f, 0.15f, 0.1f),
                            new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(680f, 100f));
        msg.horizontalOverflow = HorizontalWrapMode.Wrap;
        MakeButton(box, "Xoá và chơi mới", new Vector2(0.5f, 0f), new Vector2(-170f, 70f), new Vector2(300f, 64f), ConfirmNewGame);
        MakeButton(box, "Thôi", new Vector2(0.5f, 0f), new Vector2(170f, 70f), new Vector2(300f, 64f), CloseOverlays);
        confirmPanel.SetActive(false);
    }

    void RebuildLevelButtons()
    {
        foreach (Transform child in levelList) Destroy(child.gameObject);
        int unlocked = PlayerProfile.Current.unlockedLevels;
        GameObject first = null;
        for (int i = 0; i < levelScenes.Length; i++)
        {
            int index = i;
            bool open = i < unlocked;
            string title = i < levelTitles.Length ? levelTitles[i] : levelScenes[i];
            var b = MakeButton(levelList, open ? title : $"{title}  (chưa mở)", new Vector2(0.5f, 1f), new Vector2(0f, -50f - i * 100f),
                               new Vector2(640f, 80f), () => ScreenFader.LoadScene(levelScenes[index]));
            b.interactable = open;
            if (open) first = b.gameObject;   // chọn sẵn màn mới nhất đã mở
        }
        if (EventSystem.current != null && first != null) EventSystem.current.SetSelectedGameObject(first);
    }

    void OpenSettings()
    {
        CloseOverlays();
        SettingsWindow.Open(() =>
        {
            if (EventSystem.current != null && slots != null && shown >= 0 && shown < slots.Length)
                EventSystem.current.SetSelectedGameObject(slots[shown].gameObject);
        });
    }

    void AskNewGame()
    {
        if (levelPanel != null) levelPanel.SetActive(false);
        confirmPanel.SetActive(true);
    }

    void ConfirmNewGame()
    {
        PlayerProfile.ResetProfile();
        CloseOverlays();
        if (shown >= 0) Show(shown);
    }

    GameObject Overlay(Transform parent, string name, string title, float width, float height)
    {
        var dim = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        dim.transform.SetParent(parent, false);
        dim.color = new Color(0f, 0f, 0f, 0.6f);
        var rt = dim.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        var box = new GameObject("Box", typeof(RectTransform)).AddComponent<Image>();
        box.transform.SetParent(dim.transform, false);
        box.color = new Color(1f, 0.96f, 0.86f);
        Place(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
        MakeLabel(box.transform, title, 56, new Color(0.7f, 0.08f, 0.08f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(width - 40f, 80f));
        return dim.gameObject;
    }

    Button MakeButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var img = new GameObject("Button " + label, typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = new Color(0.8f, 0.15f, 0.12f);
        Place(img.rectTransform, anchor, pos, size);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var colors = b.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
        colors.selectedColor = new Color(1f, 0.85f, 0.6f);
        colors.disabledColor = new Color(0.5f, 0.45f, 0.42f, 0.8f);
        b.colors = colors;
        b.onClick.AddListener(onClick);
        var t = MakeLabel(img.transform, label, 32, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        var trt = t.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;
        return b;
    }

    Text MakeLabel(Transform parent, string content, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 box)
    {
        var t = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        Place(t.rectTransform, anchor, pos, box);
        return t;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
