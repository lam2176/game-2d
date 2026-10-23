using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Menu tạm dừng (Esc, nút Start tay cầm): Tiếp tục · Chơi lại màn · Về làng (ở làng: Về màn hình chính) · Cài đặt
/// (âm thanh, hình ảnh, đổi phím — <see cref="SettingsWindow"/>). Dựng giao diện bằng code khi vào màn; mở thì chọn sẵn
/// nút đầu để đi bằng tay cầm. Không mở khi đang mở hành trang hoặc màn đã kết thúc.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    static readonly Color Gold = new Color(0.95f, 0.78f, 0.35f);

    public static bool IsOpen { get; private set; }

    GameObject window;
    Font font;
    Text footer;
    GameObject firstButton, settingsButton;
    bool disabledPlayer;

    void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Build();
        window.SetActive(false);
    }

    void OnDisable()
    {
        if (IsOpen) Close();
    }

    void Update()
    {
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        bool pressed = (kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame) ||
                       (IsOpen && pad != null && pad.buttonEast.wasPressedThisFrame);   // B tay cầm: tiếp tục
        if (!pressed || StoryUi.IsOpen || InventoryWindow.IsOpen || InventoryWindow.ClosedFrame == Time.frameCount ||
            VillageWindow.IsOpen || VillageWindow.ClosedFrame == Time.frameCount ||
            SettingsWindow.IsOpen || SettingsWindow.ClosedFrame == Time.frameCount) return;
        if (IsOpen) Close();
        else Open();
    }

    void Open()
    {
        var gm = ZodiacGameManager.Instance;
        if (gm != null && gm.Current != ZodiacGameManager.State.Playing) return;
        IsOpen = true;
        window.SetActive(true);
        Time.timeScale = 0f;
        var player = ZodiacPlayerController.Current;
        disabledPlayer = player != null && player.ControlEnabled;
        if (disabledPlayer) player.SetControlEnabled(false);
        footer.text = ZodiacInput.UsingGamepad
            ? $"{ZodiacInput.PadButton("start")}: tiếp tục · {ZodiacInput.PadButton("select")}: hành trang"
            : "Esc: tiếp tục · I / Tab: hành trang";
        Select(firstButton);
    }

    static void Select(GameObject go)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
    }

    public void Close()
    {
        IsOpen = false;
        if (SettingsWindow.IsOpen) FindAnyObjectByType<SettingsWindow>()?.Close();
        window.SetActive(false);
        Time.timeScale = 1f;
        var player = ZodiacPlayerController.Current;
        if (disabledPlayer && player != null) player.SetControlEnabled(true);
        disabledPlayer = false;
    }

    void Restart()
    {
        Close();
        ZodiacGameManager.Instance?.Restart();
    }

    void BackToMenu()
    {
        Close();
        ZodiacGameManager.Instance?.BackToMenu();
    }

    void BackToVillage()
    {
        Close();
        ZodiacGameManager.Instance?.GoToVillage();
    }

    void OpenSettings()
    {
        window.SetActive(false);
        SettingsWindow.Open(() =>
        {
            if (!IsOpen) return;
            window.SetActive(true);
            Select(settingsButton);
        });
    }

    // ───────── Giao diện ─────────

    void Build()
    {
        var canvasGo = new GameObject("PauseCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 550;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        window = new GameObject("Window", typeof(RectTransform));
        window.transform.SetParent(canvasGo.transform, false);
        Stretch((RectTransform)window.transform);
        window.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        var panel = Img(window.transform, "Panel", new Color(0.11f, 0.08f, 0.06f, 0.96f));
        Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 560f));
        panel.gameObject.AddComponent<Outline>().effectColor = Gold;
        var p = panel.transform;
        Label(p, "TẠM DỪNG", 52, Gold, new Vector2(0f, 210f), new Vector2(560f, 70f)).fontStyle = FontStyle.Bold;
        firstButton = MakeButton(p, "Tiếp tục", new Vector2(0f, 110f), Close).gameObject;
        var gm = FindAnyObjectByType<ZodiacGameManager>();
        float y = 30f;
        if (gm != null && gm.isHub) MakeButton(p, "Về màn hình chính", new Vector2(0f, y), BackToMenu);
        else
        {
            MakeButton(p, "Chơi lại màn", new Vector2(0f, y), Restart);
            y -= 80f;
            MakeButton(p, "Về làng", new Vector2(0f, y), BackToVillage);
        }
        y -= 80f;
        settingsButton = MakeButton(p, "Cài đặt · Phím", new Vector2(0f, y), OpenSettings).gameObject;
        footer = Label(p, "", 20, new Color(0.8f, 0.72f, 0.6f), new Vector2(0f, -235f), new Vector2(560f, 30f));
    }

    Image Img(Transform parent, string name, Color color)
    {
        var img = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = color;
        return img;
    }

    Text Label(Transform parent, string content, int size, Color color, Vector2 pos, Vector2 box)
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
        Place(t.rectTransform, new Vector2(0.5f, 0.5f), pos, box);
        return t;
    }

    Button MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var img = Img(parent, "Button", new Color(0.7f, 0.15f, 0.12f));
        Place(img.rectTransform, new Vector2(0.5f, 0.5f), pos, new Vector2(420f, 62f));
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var colors = b.colors;
        colors.highlightedColor = colors.selectedColor = new Color(1f, 0.85f, 0.6f);
        b.colors = colors;
        b.onClick.AddListener(ZodiacUi.Click);
        b.onClick.AddListener(onClick);
        var t = Label(img.transform, label, 28, Color.white, Vector2.zero, new Vector2(420f, 62f));
        Stretch(t.rectTransform);
        return b;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }
}
