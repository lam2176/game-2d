using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Menu tạm dừng (Esc, nút Start tay cầm): Tiếp tục · Chơi lại màn · Về màn hình chính, cùng cài đặt âm lượng nhạc /
/// hiệu ứng và toàn màn hình (lưu PlayerPrefs qua <see cref="ZodiacAudio"/>). Dựng giao diện bằng code khi vào màn.
/// Không mở khi đang mở hành trang hoặc màn đã kết thúc.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    static readonly Color Gold = new Color(0.95f, 0.78f, 0.35f);
    static readonly Color Ink = new Color(0.95f, 0.9f, 0.8f);

    public static bool IsOpen { get; private set; }

    GameObject window;
    Font font;
    Text fullscreenLabel;
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
        bool pressed = (kb != null && kb.escapeKey.wasPressedThisFrame) ||
                       (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
        if (!pressed || StoryUi.IsOpen || InventoryWindow.IsOpen || InventoryWindow.ClosedFrame == Time.frameCount ||
            VillageWindow.IsOpen || VillageWindow.ClosedFrame == Time.frameCount) return;
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
        RefreshFullscreen();
    }

    public void Close()
    {
        IsOpen = false;
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

    void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        RefreshFullscreen(!Screen.fullScreen);
    }

    void RefreshFullscreen(bool? value = null)
    {
        if (fullscreenLabel != null) fullscreenLabel.text = (value ?? Screen.fullScreen) ? "Toàn màn hình: Bật" : "Toàn màn hình: Tắt";
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
        Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 700f));
        panel.gameObject.AddComponent<Outline>().effectColor = Gold;
        var p = panel.transform;
        Label(p, "TẠM DỪNG", 52, Gold, new Vector2(0f, 290f), new Vector2(560f, 70f)).fontStyle = FontStyle.Bold;
        MakeButton(p, "Tiếp tục", new Vector2(0f, 190f), Close);
        var gm = FindAnyObjectByType<ZodiacGameManager>();
        if (gm != null && gm.isHub) MakeButton(p, "Về màn hình chính", new Vector2(0f, 110f), BackToMenu);
        else
        {
            MakeButton(p, "Chơi lại màn", new Vector2(0f, 110f), Restart);
            MakeButton(p, "Về làng", new Vector2(0f, 30f), BackToVillage);
        }

        Label(p, "Nhạc", 26, Ink, new Vector2(-200f, -70f), new Vector2(160f, 40f));
        MakeSlider(p, new Vector2(60f, -70f), ZodiacAudio.MusicVolume, ZodiacAudio.SetMusicVolume);
        Label(p, "Hiệu ứng", 26, Ink, new Vector2(-200f, -140f), new Vector2(160f, 40f));
        MakeSlider(p, new Vector2(60f, -140f), ZodiacAudio.SfxVolume, v => ZodiacAudio.SetSfxVolume(v));
        var fs = MakeButton(p, "", new Vector2(0f, -230f), ToggleFullscreen);
        fullscreenLabel = fs.GetComponentInChildren<Text>();
        Label(p, "Esc: tiếp tục · I / Tab: hành trang", 20, new Color(0.8f, 0.72f, 0.6f), new Vector2(0f, -310f), new Vector2(560f, 30f));
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
        colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
        b.colors = colors;
        b.onClick.AddListener(onClick);
        var t = Label(img.transform, label, 28, Color.white, Vector2.zero, new Vector2(420f, 62f));
        Stretch(t.rectTransform);
        return b;
    }

    void MakeSlider(Transform parent, Vector2 pos, float value, UnityEngine.Events.UnityAction<float> onChange)
    {
        var root = new GameObject("Slider", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        Place((RectTransform)root.transform, new Vector2(0.5f, 0.5f), pos, new Vector2(320f, 28f));
        var back = Img(root.transform, "Back", new Color(0.25f, 0.2f, 0.15f));
        Stretch(back.rectTransform);
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        Stretch((RectTransform)fillArea.transform);
        var fill = Img(fillArea.transform, "Fill", Gold);
        Stretch(fill.rectTransform);
        var handleArea = new GameObject("Handle Area", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        Stretch((RectTransform)handleArea.transform);
        var handle = Img(handleArea.transform, "Handle", Color.white);
        handle.rectTransform.sizeDelta = new Vector2(24f, 0f);
        handle.rectTransform.anchorMin = new Vector2(0f, 0f);
        handle.rectTransform.anchorMax = new Vector2(0f, 1f);
        var slider = root.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;
        slider.onValueChanged.AddListener(onChange);
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
