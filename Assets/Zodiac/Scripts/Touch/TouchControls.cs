using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Điều khiển cảm ứng trên điện thoại, bố cục kiểu game hành động di động:
///   • Trái: cần ảo nổi (<see cref="TouchStick"/>) — chạm đâu hiện đó, đẩy hết cỡ là chạy nhanh.
///   • Phải: nút Đánh to ở góc, quanh đó Nhảy · Lăn né · Chiêu · Tuyệt kỹ · Túm. Bốn ô chiêu của <see cref="SkillHud"/>
///     được dời vào cụm này nên giữ nguyên biểu tượng, vòng hồi chiêu, số Khí và hiệu ứng tuyệt kỹ đầy Khí.
///   • Góc trên phải: Túi và tạm dừng.
/// Mỗi nút giả lập nút tay cầm đang gán cho hành động (<see cref="ZodiacInput.PadPath"/>), nên mọi logic dùng chung đường
/// input có sẵn. Dựng lại theo từng màn có <see cref="SkillHud"/> (màn chính không có). Ẩn khi đang mở cửa sổ / thoại
/// (thời gian đứng) — lúc thoại chỉ giữ nút tạm dừng để bỏ qua. Cỡ và độ rõ của nút chỉnh trong Cài đặt.
/// </summary>
public class TouchControls : MonoBehaviour
{
    const string ScaleKey = "zodiac.touchScale";
    const string AlphaKey = "zodiac.touchAlpha";
    static readonly float[] Scales = { 0.85f, 1f, 1.15f };
    static readonly string[] ScaleNames = { "Nhỏ", "Vừa", "To" };
    static readonly float[] Alphas = { 1f, 0.75f, 0.5f };
    static readonly string[] AlphaNames = { "Rõ", "Vừa", "Mờ" };

    static readonly Color Back = new Color(0.1f, 0.07f, 0.06f, 0.78f);
    static readonly Color RimColor = new Color(1f, 0.82f, 0.4f, 0.9f);

    public static int ScaleIndex => Mathf.Clamp(PlayerPrefs.GetInt(ScaleKey, 1), 0, Scales.Length - 1);
    public static int AlphaIndex => Mathf.Clamp(PlayerPrefs.GetInt(AlphaKey, 0), 0, Alphas.Length - 1);
    public static string ScaleLabel => ScaleNames[ScaleIndex];
    public static string AlphaLabel => AlphaNames[AlphaIndex];

    static TouchControls current;

    RectTransform safeArea, right;
    CanvasGroup gameplay, menu;
    Rect appliedSafeArea;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (!ZodiacInput.Touch) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach();

    /// <summary>Màn có thanh chiêu thì dựng điều khiển (nằm trong scene, tự huỷ khi đổi màn).</summary>
    static void Attach()
    {
        if (current != null) return;
        var hud = FindAnyObjectByType<SkillHud>();
        if (hud == null) return;
        current = new GameObject("TouchControls").AddComponent<TouchControls>();
        current.Build(hud);
    }

    public static void CycleScale()
    {
        PlayerPrefs.SetInt(ScaleKey, (ScaleIndex + 1) % Scales.Length);
        PlayerPrefs.Save();
        if (current != null) current.ApplyPrefs();
    }

    public static void CycleAlpha()
    {
        PlayerPrefs.SetInt(AlphaKey, (AlphaIndex + 1) % Alphas.Length);
        PlayerPrefs.Save();
        if (current != null) current.ApplyPrefs();
    }

    void OnDestroy()
    {
        if (current == this) current = null;
    }

    void Update()
    {
        if (appliedSafeArea != Screen.safeArea) FitSafeArea();
        var gm = ZodiacGameManager.Instance;
        bool playing = gm == null || gm.Current == ZodiacGameManager.State.Playing;
        bool windowOpen = PauseMenu.IsOpen || InventoryWindow.IsOpen || VillageWindow.IsOpen || SettingsWindow.IsOpen;
        bool show = playing && !windowOpen && ZodiacPlayerController.Current != null;
        // Thời gian đứng (thoại, chọn phần thưởng tháp…): ẩn cần và nút đánh để không che nút của bảng đang mở.
        SetVisible(gameplay, show && Time.timeScale > 0f && !StoryUi.IsOpen);
        SetVisible(menu, show);
    }

    static void SetVisible(CanvasGroup group, bool value)
    {
        if (group.gameObject.activeSelf != value) group.gameObject.SetActive(value);   // tắt thì nút / cần tự nhả
    }

    void ApplyPrefs()
    {
        right.localScale = Vector3.one * Scales[ScaleIndex];
        gameplay.alpha = menu.alpha = Alphas[AlphaIndex];
    }

    // ───────── Dựng giao diện ─────────

    void Build(SkillHud hud)
    {
        // Dưới lớp hành trang (500: ô dùng nhanh, thanh bên trái) để các ô đó bấm được dù nằm trong vùng cần ảo.
        var canvas = ZodiacUi.Canvas(transform, "TouchCanvas", 490);
        safeArea = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
        safeArea.SetParent(canvas, false);
        FitSafeArea();
        gameplay = Group("Gameplay");
        menu = Group("Menu");

        BuildStick(out var sprint);

        // Cụm nút phải — co giãn theo cỡ nút trong cài đặt, gốc ở góc dưới phải.
        right = new GameObject("Right", typeof(RectTransform)).GetComponent<RectTransform>();
        right.SetParent(gameplay.transform, false);
        right.anchorMin = right.anchorMax = right.pivot = new Vector2(1f, 0f);
        right.sizeDelta = Vector2.zero;

        // Vòng cung quanh nút Đánh: Nhảy · Chiêu · Lăn · Tuyệt kỹ, vòng ngoài: Thái Dương · Tái Tạo · Túm · Đậu Thần
        Adopt(hud.attackSlot, "Attack", new Vector2(-215f, 200f), 225f);
        Adopt(hud.dodgeSlot, "Dodge", new Vector2(-175f, 445f), 128f);
        Adopt(hud.skillSlot, "Skill", new Vector2(-455f, 290f), 150f);
        Adopt(hud.ultimateSlot, "Ultimate", new Vector2(-370f, 480f), 160f);
        IconButton(right, "Jump", TouchArt.Jump, new Vector2(-440f, 95f), 130f);
        IconButton(right, "Interact", TouchArt.Hand, new Vector2(-650f, 90f), 108f);
        var extra = ExtraSkills.Ensure();
        if (extra != null)
        {
            var places = new[] { new Vector2(-590f, 460f), new Vector2(-655f, 260f), new Vector2(-840f, 110f) };
            var sizes = new[] { 120f, 112f, 104f };
            for (int i = 0; i < extra.Entries.Length && i < places.Length; i++)
                Adopt(extra.Entries[i].slot, extra.Entries[i].gamepad, places[i], sizes[i], usePath: true);
        }
        // Đổi mục tiêu (PlayerTarget nghe R3 tay cầm) — góc trên phải cạnh Túi
        IconButton(gameplay.transform, null, TouchArt.Target, new Vector2(-322f, -72f), 100f, "<Gamepad>/rightStickPress", new Vector2(1f, 1f));

        // Túi · tạm dừng — góc trên phải.
        var top = new Vector2(1f, 1f);
        IconButton(menu.transform, null, TouchArt.Bag, new Vector2(-200f, -72f), 100f, "<Gamepad>/select", top);
        IconButton(menu.transform, null, TouchArt.Pause, new Vector2(-78f, -72f), 100f, "<Gamepad>/start", top);

        // Dòng gợi ý phím bàn phím không dùng trên điện thoại.
        var hint = GameObject.Find("HintText");
        if (hint != null) hint.SetActive(false);

        ApplyPrefs();
    }

    CanvasGroup Group(string name)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(safeArea, false);
        ZodiacUi.Stretch(rt);
        return rt.gameObject.AddComponent<CanvasGroup>();
    }

    void FitSafeArea()
    {
        appliedSafeArea = Screen.safeArea;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        safeArea.anchorMin = new Vector2(appliedSafeArea.xMin / Screen.width, appliedSafeArea.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(appliedSafeArea.xMax / Screen.width, appliedSafeArea.yMax / Screen.height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Vùng cần ảo: 40% trái, 62% dưới màn hình (chừa tay nắm thanh bên ở mép trái và ô dùng nhanh phía trên);
    /// đế nghỉ ở góc dưới trái.
    /// </summary>
    void BuildStick(out TouchButton sprint)
    {
        var zone = ZodiacUi.MakeImage(gameplay.transform, "StickZone", new Color(0f, 0f, 0f, 0f));
        var zr = zone.rectTransform;
        zr.anchorMin = new Vector2(0.035f, 0f);
        zr.anchorMax = new Vector2(0.4f, 0.62f);
        zr.pivot = Vector2.zero;
        zr.offsetMin = zr.offsetMax = Vector2.zero;

        var stickBase = new GameObject("Base", typeof(RectTransform)).GetComponent<RectTransform>();
        stickBase.SetParent(zr, false);
        stickBase.anchorMin = stickBase.anchorMax = Vector2.zero;
        stickBase.anchoredPosition = new Vector2(270f, 250f);
        stickBase.sizeDelta = new Vector2(300f, 300f);
        Sprite(stickBase, "Shadow", TouchArt.Glow, new Color(0f, 0f, 0f, 0.45f), 360f);
        Sprite(stickBase, "Disc", TouchArt.Disc, new Color(0.12f, 0.09f, 0.08f, 0.55f), 300f);
        var ring = Sprite(stickBase, "Ring", TouchArt.Ring, new Color(1f, 1f, 1f, 0.55f), 300f);
        for (int i = 0; i < 4; i++)   // vạch hướng
        {
            var tick = Sprite(stickBase, "Tick", TouchArt.Disc, new Color(1f, 0.85f, 0.5f, 0.6f), 16f);
            float a = i * Mathf.PI * 0.5f;
            tick.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 122f;
        }

        var knob = new GameObject("Knob", typeof(RectTransform)).GetComponent<RectTransform>();
        knob.SetParent(stickBase, false);
        knob.sizeDelta = new Vector2(130f, 130f);
        Sprite(knob, "Glow", TouchArt.Glow, new Color(1f, 0.8f, 0.4f, 0.35f), 190f);
        Sprite(knob, "Disc", TouchArt.Disc, new Color(1f, 0.86f, 0.55f, 0.95f), 130f);
        Sprite(knob, "Ring", TouchArt.Ring, new Color(1f, 1f, 1f, 0.9f), 130f);

        // Chạy nhanh: nút ẩn do cần giữ hộ khi đẩy hết cỡ.
        sprint = null;
        string sprintPath = ZodiacInput.PadPath("Sprint");
        if (sprintPath != null)
        {
            var go = new GameObject("Sprint", typeof(RectTransform));
            go.transform.SetParent(zr, false);
            go.SetActive(false);
            sprint = go.AddComponent<TouchButton>();
            sprint.controlPath = sprintPath;
            go.SetActive(true);
        }

        zone.gameObject.SetActive(false);   // gán đế / cần trước khi bật
        var stick = zone.gameObject.AddComponent<TouchStick>();
        stick.stickBase = stickBase;
        stick.knob = knob;
        stick.ring = ring;
        stick.sprint = sprint;
        stick.controlPath = "<Gamepad>/leftStick";
        zone.gameObject.SetActive(true);
    }

    /// <summary>Dời một ô chiêu của SkillHud vào cụm nút: bọc trong khung có bóng, vành vàng và quầng bấm.</summary>
    void Adopt(SkillHud.Slot slot, string action, Vector2 pos, float size, bool usePath = false)
    {
        string path = usePath ? action : ZodiacInput.PadPath(action);
        if (slot == null || slot.root == null || path == null) return;
        var holder = Holder(right, action, pos, size, out var glow);
        float slotSize = slot.root.sizeDelta.x;
        slot.root.SetParent(holder, false);
        slot.root.anchorMin = slot.root.anchorMax = new Vector2(0.5f, 0.5f);
        slot.root.anchoredPosition = Vector2.zero;
        // SkillHud tự đặt localScale của ô (nảy khi dùng chiêu) — co bằng khung trung gian.
        var fit = new GameObject("Fit", typeof(RectTransform)).GetComponent<RectTransform>();
        fit.SetParent(holder, false);
        fit.localScale = Vector3.one * (size * 0.96f / slotSize);
        slot.root.SetParent(fit, false);
        if (slot.key != null) slot.key.transform.parent.gameObject.SetActive(false);   // nhãn phím bàn phím
        if (slot.label != null) slot.label.gameObject.SetActive(false);
        Rim(holder, size);
        Finish(holder.gameObject, path, glow);
    }

    /// <summary>Nút tròn có biểu tượng cho hành động (hoặc đường dẫn tay cầm cho trước).</summary>
    static void IconButton(Transform parent, string action, Sprite icon, Vector2 pos, float size, string path = null, Vector2? anchor = null)
    {
        path = path ?? ZodiacInput.PadPath(action);
        if (path == null) return;
        var holder = Holder(parent, action ?? icon.name, pos, size, out var glow, anchor);
        Sprite(holder, "Back", TouchArt.Disc, Back, size * 0.92f);
        Sprite(holder, "Icon", icon, new Color(1f, 0.95f, 0.85f, 1f), size * 0.5f);
        Rim(holder, size);
        Finish(holder.gameObject, path, glow);
    }

    static RectTransform Holder(Transform parent, string name, Vector2 pos, float size, out Image glow, Vector2? anchor = null)
    {
        var holder = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        holder.SetParent(parent, false);
        holder.anchorMin = holder.anchorMax = anchor ?? new Vector2(1f, 0f);
        holder.anchoredPosition = pos;
        holder.sizeDelta = new Vector2(size, size);
        var hit = holder.gameObject.AddComponent<Image>();   // vùng bấm tròn trong suốt
        hit.sprite = TouchArt.Disc;
        hit.color = new Color(0f, 0f, 0f, 0f);
        Sprite(holder, "Shadow", TouchArt.Glow, new Color(0f, 0f, 0f, 0.5f), size * 1.3f).rectTransform.anchoredPosition = new Vector2(0f, -size * 0.04f);
        glow = Sprite(holder, "Glow", TouchArt.Glow, new Color(1f, 0.85f, 0.45f, 0f), size * 1.45f);
        return holder;
    }

    /// <summary>Biến một ô HUD có sẵn (ô dùng nhanh…) thành nút cảm ứng.</summary>
    public static void Tappable(GameObject go, string path)
    {
        if (path != null) Finish(go, path, null);
    }

    static void Rim(RectTransform holder, float size) => Sprite(holder, "Rim", TouchArt.Ring, RimColor, size);

    static void Finish(GameObject go, string path, Image glow)
    {
        go.SetActive(false);   // gán đường dẫn trước khi bật, để không tạo nhầm thiết bị
        var button = go.AddComponent<TouchButton>();
        button.glow = glow;
        button.controlPath = path;
        go.SetActive(true);
    }

    static Image Sprite(Transform parent, string name, Sprite sprite, Color color, float size)
    {
        var img = ZodiacUi.MakeImage(parent, name, color);
        img.sprite = sprite;
        img.raycastTarget = false;
        ZodiacUi.Place(img.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
        return img;
    }
}
