using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Màn hình chính: lưới 12 con giáp (chuột hoặc phím mũi tên / tay cầm để chọn), bảng thông tin nhân vật
/// (chỉ số, cấp của con đó, nội tại, chiêu thức), nút Bắt đầu (mở bảng chọn màn — màn chưa mở bị khoá),
/// nút Chơi mới (xoá hồ sơ, có hỏi lại) và nút Thoát. Bảng chọn màn / hỏi lại dựng bằng code lúc chạy.
/// Ô đang được chọn (EventSystem) chính là nhân vật sẽ chơi — lưu vào <see cref="ZodiacSelection"/>.
/// Có máy chủ (<see cref="ZodiacCloud.Configured"/>) thì phải đăng nhập trước ở scene <see cref="LoginScreen"/>; tài khoản mới chọn một con giáp
/// (một lần, không đổi được), sau đó lưới chỉ còn con giáp của tài khoản. Nút Ô lưu thành nút Đăng xuất.
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
    GameObject levelPanel, confirmPanel, slotPanel;
    Text confirmText, slotButtonText, accountText;
    Transform slotList;
    System.Action pendingConfirm;
    Font font;

    void Start()
    {
        if (ZodiacCloud.Configured && !ZodiacCloud.LoggedIn)
        {
            SceneManager.LoadScene(LoginScreen.SceneName);   // chưa đăng nhập (mở thẳng scene này / vừa đăng xuất)
            return;
        }
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildOverlay();
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.PlayMusic(res.musicMenu);
        if (roster == null || roster.Count == 0) return;
        int index = Mathf.Clamp(ZodiacSelection.Index, 0, roster.Count - 1);
        if (slots != null && index < slots.Length && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(slots[index].gameObject);
        Show(index);
        if (ZodiacCloud.Configured)
        {
            ZodiacCloud.SessionLost -= OnSessionLost;
            ZodiacCloud.SessionLost += OnSessionLost;
            OnLoggedIn();
        }
    }

    void OnDestroy() => ZodiacCloud.SessionLost -= OnSessionLost;

    void Update()
    {
        if (SettingsWindow.IsOpen) return;
        if ((levelPanel != null && levelPanel.activeSelf) || (confirmPanel != null && confirmPanel.activeSelf) ||
            (slotPanel != null && slotPanel.activeSelf))
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
        if (ZodiacCloud.Configured && !ZodiacCloud.LoggedIn)
        {
            GoLogin("");
            return;
        }
        if (ZodiacCloud.Configured && !ZodiacCloud.HasCharacter)
        {
            AskChooseCharacter();
            return;
        }
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
        if (slotPanel != null) slotPanel.SetActive(false);
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
        var slotButton = MakeButton(canvasGo.transform, "", new Vector2(1f, 1f), new Vector2(-150f, -60f), new Vector2(240f, 64f), OpenSlots);
        slotButton.name = "SaveSlotButton";
        slotButtonText = slotButton.GetComponentInChildren<Text>();
        RefreshSlotButton();
        // Góc trái trên (tiêu đề nằm giữa)
        accountText = MakeLabel(canvasGo.transform, "", 26, new Color(0.5f, 0.12f, 0.08f), new Vector2(0f, 1f), new Vector2(370f, -65f), new Vector2(680f, 90f));
        accountText.alignment = TextAnchor.MiddleLeft;
        accountText.horizontalOverflow = HorizontalWrapMode.Wrap;
        MakeButton(canvasGo.transform, "Cài đặt", new Vector2(1f, 1f), new Vector2(-410f, -60f), new Vector2(240f, 64f), OpenSettings).name = "SettingsButton";

        levelPanel = Overlay(canvasGo.transform, "LevelPanel", "CHỌN MÀN", 1320f, 760f);
        var list = new GameObject("List", typeof(RectTransform));
        list.transform.SetParent(levelPanel.transform.Find("Box"), false);
        Place((RectTransform)list.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1240f, 520f));
        levelList = list.transform;
        MakeButton(levelPanel.transform.Find("Box"), "Quay lại", new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(260f, 64f), CloseOverlays);
        levelPanel.SetActive(false);

        slotPanel = Overlay(canvasGo.transform, "SlotPanel", "Ô LƯU", 1400f, 720f);
        var slotBox = slotPanel.transform.Find("Box");
        var sl = new GameObject("Slots", typeof(RectTransform));
        sl.transform.SetParent(slotBox, false);
        Place((RectTransform)sl.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1320f, 480f));
        slotList = sl.transform;
        MakeButton(slotBox, "Quay lại", new Vector2(0.5f, 0f), new Vector2(0f, 55f), new Vector2(260f, 64f), CloseOverlays);
        slotPanel.SetActive(false);

        confirmPanel = Overlay(canvasGo.transform, "ConfirmPanel", "CHẮC CHƯA?", 760f, 420f);
        var box = confirmPanel.transform.Find("Box");
        confirmText = MakeLabel(box, "", 28, new Color(0.25f, 0.15f, 0.1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(680f, 100f));
        confirmText.horizontalOverflow = HorizontalWrapMode.Wrap;
        MakeButton(box, "Đồng ý", new Vector2(0.5f, 0f), new Vector2(-170f, 70f), new Vector2(300f, 64f), () =>
        {
            var act = pendingConfirm;
            pendingConfirm = null;
            confirmPanel.SetActive(false);
            act?.Invoke();
        });
        MakeButton(box, "Thôi", new Vector2(0.5f, 0f), new Vector2(170f, 70f), new Vector2(300f, 64f), () =>
        {
            pendingConfirm = null;
            confirmPanel.SetActive(false);
            if (ZodiacCloud.Configured) CloseOverlays();
            else OpenSlots();
        });
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
            // Hai cột: màn lẻ bên trái, màn chẵn bên phải
            var b = MakeButton(levelList, open ? title : $"{title}  (chưa mở)", new Vector2(0.5f, 1f),
                               new Vector2(i % 2 == 0 ? -310f : 310f, -45f - (i / 2) * 96f),
                               new Vector2(590f, 80f), () => ScreenFader.LoadScene(levelScenes[index]));
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

    // ───────── Ô lưu ─────────

    void RefreshSlotButton()
    {
        if (slotButtonText == null) return;
        if (!ZodiacCloud.Configured) slotButtonText.text = $"Ô lưu {PlayerProfile.Slot}";
        else slotButtonText.text = ZodiacCloud.LoggedIn ? "Đăng xuất" : "Đăng nhập";
    }

    void OpenSlots()
    {
        if (ZodiacCloud.Configured)
        {
            if (ZodiacCloud.LoggedIn) AskLogout();
            else GoLogin("");
            return;
        }
        if (levelPanel != null) levelPanel.SetActive(false);
        confirmPanel.SetActive(false);
        slotPanel.SetActive(true);
        RebuildSlots();
    }

    /// <summary>Ba thẻ ô lưu: tóm tắt tiến trình, nút Dùng / Bắt đầu mới và Xoá.</summary>
    void RebuildSlots()
    {
        foreach (Transform child in slotList)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        GameObject first = null;
        for (int i = 1; i <= PlayerProfile.SlotCount; i++)
        {
            int slot = i;
            bool active = slot == PlayerProfile.Slot;
            var p = PlayerProfile.Peek(slot);
            float x = (i - 2) * 440f;
            var card = new GameObject("Slot " + slot, typeof(RectTransform)).AddComponent<Image>();
            card.transform.SetParent(slotList, false);
            card.color = active ? new Color(1f, 0.88f, 0.6f) : new Color(0.93f, 0.86f, 0.72f);
            card.raycastTarget = false;
            Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(410f, 470f));
            var title = MakeLabel(card.transform, $"Ô {slot}" + (active ? "  (đang dùng)" : ""), 34, new Color(0.6f, 0.1f, 0.08f),
                                  new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(380f, 50f));
            title.fontStyle = FontStyle.Bold;
            var info = MakeLabel(card.transform, p != null ? Summary(p) : "Trống\n\nBắt đầu một hành trình mới", 22, new Color(0.25f, 0.15f, 0.1f),
                                 new Vector2(0.5f, 1f), new Vector2(0f, -175f), new Vector2(370f, 220f));
            info.horizontalOverflow = HorizontalWrapMode.Wrap;
            var use = MakeButton(card.transform, p != null ? (active ? "Tiếp tục" : "Dùng ô này") : "Bắt đầu mới", new Vector2(0.5f, 0f),
                                 new Vector2(0f, 120f), new Vector2(320f, 60f), () => UseSlot(slot));
            if (first == null || active) first = use.gameObject;
            if (p != null)
                MakeButton(card.transform, "Xoá", new Vector2(0.5f, 0f), new Vector2(0f, 45f), new Vector2(200f, 50f), () => AskDelete(slot));
        }
        if (EventSystem.current != null && first != null) EventSystem.current.SetSelectedGameObject(first);
    }

    string Summary(PlayerProfile p)
    {
        int best = 0;
        string bestName = "";
        if (p.characters != null)
            foreach (var c in p.characters)
                if (c.level > best)
                {
                    best = c.level;
                    var def = roster != null && roster.characters != null ? System.Array.Find(roster.characters, r => r != null && r.name == c.character) : null;
                    bestName = def != null ? def.displayName : c.character;
                }
        int levels = levelScenes != null && levelScenes.Length > 0 ? levelScenes.Length : 8;
        int endings = p.endingsSeen != null ? p.endingsSeen.Count : 0;
        return $"Màn đã mở: {Mathf.Min(p.unlockedLevels, levels)}/{levels}\n" +
               $"Đồng: {p.coins}\n" +
               (best > 0 ? $"Cấp cao nhất: {best} ({bestName})\n" : "") +
               $"Phá đảo: {endings}/12" + (p.towerBest > 0 ? $" · Tháp: {p.towerBest} tầng" : "") + "\n" +
               $"Thời gian chơi: {p.PlayTimeText}\n" +
               (string.IsNullOrEmpty(p.lastSaved) ? "" : $"<size=18>Lưu lúc {p.lastSaved}</size>");
    }

    void UseSlot(int slot)
    {
        bool fresh = !PlayerProfile.SlotExists(slot);
        PlayerProfile.SelectSlot(slot);
        if (fresh) PlayerProfile.ResetProfile();   // ô trống: tạo hồ sơ mới ngay (quà khởi đầu) và lưu
        RefreshSlotButton();
        CloseOverlays();
        if (shown >= 0) Show(shown);
    }

    void AskDelete(int slot)
    {
        slotPanel.SetActive(false);
        confirmText.text = $"Xoá hẳn ô lưu {slot}? Toàn bộ đồ, tiền, cấp và màn đã mở trong ô này sẽ mất.";
        pendingConfirm = () =>
        {
            PlayerProfile.DeleteSlot(slot);
            OpenSlots();
        };
        confirmPanel.SetActive(true);
    }

    // ───────── Tài khoản online ─────────

    int AccountIndex => roster != null && ZodiacCloud.HasCharacter
        ? System.Array.FindIndex(roster.characters, c => c != null && c.name == ZodiacCloud.Current.character)
        : -1;

    /// <summary>Đăng nhập xong (hoặc quay về menu khi đang đăng nhập): khoá lưới theo con giáp của tài khoản.</summary>
    void OnLoggedIn()
    {
        RefreshSlotButton();
        int mine = AccountIndex;
        for (int i = 0; slots != null && i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            bool open = mine < 0 || i == mine;
            slots[i].interactable = open;
            var group = slots[i].GetComponent<CanvasGroup>();
            if (group == null) group = slots[i].gameObject.AddComponent<CanvasGroup>();
            group.alpha = open ? 1f : 0.35f;
        }
        int index = mine >= 0 ? mine : Mathf.Clamp(shown, 0, roster.Count - 1);
        if (EventSystem.current != null && slots != null && index < slots.Length) EventSystem.current.SetSelectedGameObject(slots[index].gameObject);
        Show(index);
        RefreshAccountText();
    }

    void RefreshAccountText()
    {
        if (accountText == null) return;
        if (!ZodiacCloud.LoggedIn)
        {
            accountText.text = "";
            return;
        }
        var acc = ZodiacCloud.Current;
        accountText.text = ZodiacCloud.HasCharacter
            ? $"Tài khoản <b>{acc.username}</b>"
            : $"Tài khoản <b>{acc.username}</b> · chọn một con giáp rồi bấm Bắt đầu (chỉ chọn một lần)";
    }

    void AskChooseCharacter()
    {
        var c = roster.Get(shown);
        if (c == null) return;
        confirmText.text = $"Chọn {c.displayName} làm nhân vật của tài khoản? Chơi từ đầu và không đổi được nữa.";
        pendingConfirm = () =>
        {
            accountText.text = "Đang lưu lựa chọn…";
            ZodiacCloud.ChooseCharacter(c.name, error =>
            {
                if (error != null)
                {
                    accountText.text = error;
                    return;
                }
                ZodiacSelection.Index = shown;
                PlayerProfile.Current.Progress(c.name);
                PlayerProfile.Current.Save();   // hồ sơ đầu tiên lên máy chủ
                OnLoggedIn();
                Play();
            });
        };
        confirmPanel.SetActive(true);
    }

    void AskLogout()
    {
        confirmText.text = $"Đăng xuất tài khoản {ZodiacCloud.Current.username}? Tiến trình đã được lưu trên máy chủ.";
        pendingConfirm = () =>
        {
            ZodiacCloud.Logout();
            GoLogin("");
        };
        confirmPanel.SetActive(true);
    }

    void OnSessionLost(string message) => GoLogin(message);

    static void GoLogin(string message)
    {
        LoginScreen.PendingMessage = message;
        ScreenFader.LoadScene(LoginScreen.SceneName);
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
        b.onClick.AddListener(ZodiacUi.Click);
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
