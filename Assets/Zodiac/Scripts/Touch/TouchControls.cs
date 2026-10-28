using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Điều khiển cảm ứng trên điện thoại: cần ảo bên trái (đi ngang / đổi làn), cụm nút hành động bên phải, nút tạm dừng
/// và hành trang ở góc trên. Mỗi nút giả lập một nút tay cầm (OnScreenControl) — lấy đúng nút tay cầm đang gán cho
/// hành động trong <see cref="ZodiacInput"/>, nên nhân vật, menu, đổi phím dùng chung đường input có sẵn.
/// Chỉ dựng khi chạy trên điện thoại; ẩn khi đang mở menu / hành trang / làng / cài đặt, khi thoại chỉ giữ nút tạm dừng.
/// </summary>
public class TouchControls : MonoBehaviour
{
    static readonly Color ButtonBack = new Color(0.11f, 0.08f, 0.06f, 0.45f);
    static readonly Color AttackBack = new Color(0.7f, 0.15f, 0.12f, 0.6f);

    static TouchControls instance;
    static Sprite circle;

    RectTransform safeArea;
    GameObject gameplay, menu;
    Rect appliedSafeArea;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (!ZodiacInput.Touch || instance != null) return;
        Application.targetFrameRate = 60;   // điện thoại mặc định chỉ 30 khung hình
        var go = new GameObject("TouchControls");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<TouchControls>();
    }

    void Awake()
    {
        Build();
        Refresh();
    }

    void Update()
    {
        if (appliedSafeArea != Screen.safeArea) FitSafeArea();
        Refresh();
    }

    void Refresh()
    {
        var player = ZodiacPlayerController.Current;
        var gm = ZodiacGameManager.Instance;
        bool inLevel = player != null && SceneManager.GetActiveScene().name != "Zodiac_Start";
        bool playing = gm == null || gm.Current == ZodiacGameManager.State.Playing;
        bool windowOpen = PauseMenu.IsOpen || InventoryWindow.IsOpen || VillageWindow.IsOpen || SettingsWindow.IsOpen;
        bool show = inLevel && playing && !windowOpen;
        // Đang thoại: chạm đâu cũng sang câu, chỉ giữ nút tạm dừng (= bỏ qua).
        SetActive(gameplay, show && !StoryUi.IsOpen);
        SetActive(menu, show);
    }

    static void SetActive(GameObject go, bool value)
    {
        if (go.activeSelf != value) go.SetActive(value);   // tắt OnScreenControl thì nút / cần tự nhả
    }

    // ───────── Dựng giao diện ─────────

    void Build()
    {
        var canvas = ZodiacUi.Canvas(transform, "TouchCanvas", 85);
        safeArea = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
        safeArea.SetParent(canvas, false);
        FitSafeArea();

        gameplay = Group("Gameplay");
        menu = Group("Menu");

        // Cần ảo — góc dưới trái.
        var stickBase = Circle(gameplay.transform, "StickBase", new Vector2(0f, 0f), new Vector2(260f, 250f), 300f, ButtonBack);
        var knob = Circle(stickBase.transform, "Knob", new Vector2(0.5f, 0.5f), Vector2.zero, 130f, new Color(0.95f, 0.78f, 0.35f, 0.7f));
        knob.gameObject.SetActive(false);
        var stick = knob.gameObject.AddComponent<OnScreenStick>();
        stick.movementRange = 110f;
        stick.controlPath = "<Gamepad>/leftStick";
        knob.gameObject.SetActive(true);

        // Cụm nút hành động — góc dưới phải, nút Đánh to ở giữa.
        var right = new Vector2(1f, 0f);
        Button(gameplay.transform, "Attack", "Đánh", right, new Vector2(-250f, 230f), 210f, AttackBack, 40);
        Button(gameplay.transform, "Jump", "Nhảy", right, new Vector2(-110f, 110f), 150f, ButtonBack, 30);
        Button(gameplay.transform, "Dodge", "Né", right, new Vector2(-420f, 120f), 150f, ButtonBack, 30);
        Button(gameplay.transform, "Skill", "Chiêu", right, new Vector2(-430f, 330f), 150f, ButtonBack, 30);
        Button(gameplay.transform, "Ultimate", "Tuyệt kỹ", right, new Vector2(-260f, 440f), 150f, ButtonBack, 26);
        Button(gameplay.transform, "Interact", "Túm", right, new Vector2(-100f, 330f), 140f, ButtonBack, 30);
        Button(gameplay.transform, "Sprint", "Chạy", right, new Vector2(-100f, 500f), 120f, ButtonBack, 26);
        // Dùng nhanh 1 / 2 — trên cần ảo.
        Button(gameplay.transform, "Previous", "1", new Vector2(0f, 0f), new Vector2(120f, 520f), 110f, ButtonBack, 34);
        Button(gameplay.transform, "Next", "2", new Vector2(0f, 0f), new Vector2(250f, 520f), 110f, ButtonBack, 34);

        // Tạm dừng / hành trang — góc trên phải.
        PadButton(menu.transform, "<Gamepad>/start", "II", new Vector2(1f, 1f), new Vector2(-80f, -80f), 110f, 40);
        PadButton(gameplay.transform, "<Gamepad>/select", "Túi", new Vector2(1f, 1f), new Vector2(-210f, -80f), 110f, 28);
    }

    GameObject Group(string name)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(safeArea, false);
        ZodiacUi.Stretch(rt);
        return rt.gameObject;
    }

    void FitSafeArea()
    {
        appliedSafeArea = Screen.safeArea;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        safeArea.anchorMin = new Vector2(appliedSafeArea.xMin / Screen.width, appliedSafeArea.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(appliedSafeArea.xMax / Screen.width, appliedSafeArea.yMax / Screen.height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
    }

    /// <summary>Nút cho một hành động trong map Player — giả lập nút tay cầm đang gán cho hành động đó.</summary>
    static void Button(Transform parent, string action, string label, Vector2 anchor, Vector2 pos, float size, Color back, int fontSize)
    {
        string path = ZodiacInput.PadPath(action);
        if (path != null) PadButton(parent, path, label, anchor, pos, size, fontSize, back);
    }

    static void PadButton(Transform parent, string controlPath, string label, Vector2 anchor, Vector2 pos, float size, int fontSize,
                          Color? back = null)
    {
        var img = Circle(parent, label, anchor, pos, size, back ?? ButtonBack);
        img.gameObject.AddComponent<Outline>().effectColor = new Color(0.95f, 0.78f, 0.35f, 0.6f);
        ZodiacUi.MakeText(img.transform, label, fontSize, ZodiacUi.Ink, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size))
            .raycastTarget = false;
        img.gameObject.SetActive(false);   // gán đường dẫn trước khi bật, để không tạo nhầm thiết bị
        img.gameObject.AddComponent<OnScreenButton>().controlPath = controlPath;
        img.gameObject.SetActive(true);
    }

    static Image Circle(Transform parent, string name, Vector2 anchor, Vector2 pos, float size, Color color)
    {
        var img = ZodiacUi.MakeImage(parent, name, color);
        img.sprite = CircleSprite();
        ZodiacUi.Place(img.rectTransform, anchor, pos, new Vector2(size, size));
        return img;
    }

    /// <summary>Hình tròn mịn dựng lúc chạy (khỏi cần ảnh).</summary>
    static Sprite CircleSprite()
    {
        if (circle != null) return circle;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[n * n];
        float r = n * 0.5f;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
            byte a = (byte)(Mathf.Clamp01(r - d) * 255f);
            pixels[y * n + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }
}
