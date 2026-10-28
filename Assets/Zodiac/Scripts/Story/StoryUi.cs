using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Hiển thị cốt truyện (<see cref="ZodiacStory"/>): khung thoại ở đáy màn hình (chân dung, tên, chữ chạy) và tranh kể chuyện
/// toàn màn hình kiểu giấy dó (cảnh vẽ bằng code <see cref="StoryArt"/> + hình con giáp). Trong lúc hiện: dừng game,
/// khoá điều khiển. Space / Enter / Q / J / F / chuột trái / nút A: hiện hết chữ → sang câu kế; Esc: bỏ qua cả đoạn.
/// Tự tạo khi cần.
/// </summary>
public class StoryUi : MonoBehaviour
{
    const float CharsPerSecond = 48f;

    static StoryUi instance;

    /// <summary>Đang hiện thoại / tranh.</summary>
    /// <summary>Đang hiện thoại / tranh (tính cả khung hình vừa đóng — để phím Esc vừa bấm không mở thêm menu tạm dừng).</summary>
    public static bool IsOpen => instance != null && (instance.open || instance.closedFrame == Time.frameCount);
    /// <summary>Số câu / khung đã hiện kể từ khi chạy (để kiểm tra).</summary>
    public static int ShownCount { get; private set; }

    Transform canvas;
    // Khung thoại
    GameObject dialogue;
    Image portrait;
    Text speakerText, lineText, dialogueHint;
    // Tranh
    GameObject panels;
    Image picture, icon;
    Text panelTitle, panelText, panelPage;
    readonly List<Image> figures = new List<Image>();

    bool open, showingPanels, playerWasEnabled;
    float previousTimeScale = 1f;
    int index, openedFrame, closedFrame = -1;
    float typed;
    string fullText = "";
    ZodiacStory.Line[] lines;
    ZodiacStory.Panel[] pages;
    Action done;

    static StoryUi Get
    {
        get
        {
            if (instance != null) return instance;
            instance = new GameObject("StoryUi").AddComponent<StoryUi>();
            return instance;
        }
    }

    /// <summary>Hiện một đoạn thoại; xong (hoặc bỏ qua) thì gọi done. Không có câu nào thì gọi done ngay.</summary>
    public static void ShowDialogue(ZodiacStory.Line[] lines, Action done = null)
    {
        if (lines == null || lines.Length == 0) { done?.Invoke(); return; }
        Get.Begin(lines, null, done);
    }

    /// <summary>Hiện chuỗi tranh kể chuyện; xong (hoặc bỏ qua) thì gọi done.</summary>
    public static void ShowPanels(ZodiacStory.Panel[] panels, Action done = null)
    {
        if (panels == null || panels.Length == 0) { done?.Invoke(); return; }
        Get.Begin(null, panels, done);
    }

    void Awake()
    {
        canvas = ZodiacUi.Canvas(transform, "StoryCanvas", 80);
        BuildDialogue();
        BuildPanels();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (open) Time.timeScale = previousTimeScale;
    }

    // ───────── Dựng giao diện ─────────

    void BuildDialogue()
    {
        var box = ZodiacUi.MakeImage(canvas, "Dialogue", ZodiacUi.Panel);
        ZodiacUi.Place(box.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 175f), new Vector2(1560f, 270f));
        box.gameObject.AddComponent<Outline>().effectColor = ZodiacUi.Gold;
        dialogue = box.gameObject;

        var frame = ZodiacUi.MakeImage(box.transform, "PortraitFrame", ZodiacUi.SlotBack);
        ZodiacUi.Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(135f, 0f), new Vector2(220f, 230f));
        frame.gameObject.AddComponent<Outline>().effectColor = ZodiacUi.Gold;
        frame.gameObject.AddComponent<RectMask2D>();
        portrait = ZodiacUi.MakeImage(frame.transform, "Portrait", Color.white);
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;

        speakerText = ZodiacUi.MakeText(box.transform, "", 36, ZodiacUi.Gold, new Vector2(0f, 1f), new Vector2(700f, -40f), new Vector2(1000f, 50f), TextAnchor.MiddleLeft);
        speakerText.fontStyle = FontStyle.Bold;
        lineText = ZodiacUi.MakeText(box.transform, "", 32, ZodiacUi.Ink, new Vector2(0f, 1f), new Vector2(790f, -140f), new Vector2(1180f, 150f), TextAnchor.UpperLeft);
        dialogueHint = ZodiacUi.MakeText(box.transform, "Space ▶   ·   Esc: bỏ qua", 20, ZodiacUi.Muted, new Vector2(1f, 0f), new Vector2(-170f, 22f), new Vector2(320f, 28f), TextAnchor.MiddleRight);
        dialogue.SetActive(false);
    }

    void BuildPanels()
    {
        var dim = ZodiacUi.MakeImage(canvas, "Panels", new Color(0.05f, 0.03f, 0.02f, 0.96f));
        ZodiacUi.Stretch(dim.rectTransform);
        panels = dim.gameObject;

        var paper = ZodiacUi.MakeImage(dim.transform, "Paper", StoryArt.Paper);
        ZodiacUi.Place(paper.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 960f));
        var border = paper.gameObject.AddComponent<Outline>();
        border.effectColor = StoryArt.Cinnabar;
        border.effectDistance = new Vector2(8f, -8f);

        panelTitle = PaperText(paper.transform, 50, StoryArt.Cinnabar, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1400f, 70f), TextAnchor.MiddleCenter);
        panelTitle.fontStyle = FontStyle.Bold;

        var pictureFrame = ZodiacUi.MakeImage(paper.transform, "PictureFrame", StoryArt.Ink);
        ZodiacUi.Place(pictureFrame.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(1420f, 560f));
        pictureFrame.gameObject.AddComponent<RectMask2D>();
        picture = ZodiacUi.MakeImage(pictureFrame.transform, "Picture", Color.white);
        ZodiacUi.Stretch(picture.rectTransform);
        picture.rectTransform.offsetMin = new Vector2(6f, 6f);
        picture.rectTransform.offsetMax = new Vector2(-6f, -6f);
        icon = ZodiacUi.MakeImage(pictureFrame.transform, "Icon", Color.white);
        icon.preserveAspect = true;
        ZodiacUi.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 420f));
        for (int i = 0; i < 12; i++)
        {
            var f = ZodiacUi.MakeImage(pictureFrame.transform, "Figure", Color.white);
            f.preserveAspect = true;
            f.raycastTarget = false;
            f.rectTransform.pivot = new Vector2(0.5f, 0f);
            figures.Add(f);
        }

        panelText = PaperText(paper.transform, 32, StoryArt.Ink, new Vector2(0.5f, 0f), new Vector2(0f, 135f), new Vector2(1400f, 150f), TextAnchor.UpperCenter);
        panelPage = PaperText(paper.transform, 22, StoryArt.Ink * 0.8f, new Vector2(1f, 0f), new Vector2(-190f, 32f), new Vector2(340f, 30f), TextAnchor.MiddleRight);
        panels.SetActive(false);
    }

    /// <summary>Chữ mực trên giấy: không viền đen.</summary>
    static Text PaperText(Transform parent, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 box, TextAnchor align)
    {
        var t = ZodiacUi.MakeText(parent, "", size, color, anchor, pos, box, align);
        if (t.TryGetComponent(out Outline o)) o.enabled = false;
        return t;
    }

    // ───────── Chạy ─────────

    void Begin(ZodiacStory.Line[] newLines, ZodiacStory.Panel[] newPages, Action onDone)
    {
        if (open) Finish();   // đoạn cũ chưa xong thì kết thúc luôn
        lines = newLines;
        pages = newPages;
        done = onDone;
        showingPanels = pages != null;
        index = 0;
        open = true;
        openedFrame = Time.frameCount;
        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        var player = ZodiacPlayerController.Current;
        playerWasEnabled = player != null && player.ControlEnabled;
        if (playerWasEnabled) player.SetControlEnabled(false);
        dialogue.SetActive(!showingPanels);
        panels.SetActive(showingPanels);
        ShowCurrent();
    }

    void Update()
    {
        if (!open || Time.frameCount == openedFrame) return;
        if (typed < fullText.Length)
        {
            typed = Mathf.Min(fullText.Length, typed + CharsPerSecond * Time.unscaledDeltaTime);
            CurrentText.text = fullText.Substring(0, Mathf.FloorToInt(typed));
        }
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        bool skip = (kb != null && kb.escapeKey.wasPressedThisFrame) ||
                    (pad != null && pad.startButton.wasPressedThisFrame);
        if (skip)
        {
            Finish();
            return;
        }
        var attack = ZodiacInput.Find("Attack");
        var interact = ZodiacInput.Find("Interact");
        bool next = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) ||
                    (attack != null && attack.WasPressedThisFrame()) || (interact != null && interact.WasPressedThisFrame()) ||
                    (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                    (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) ||
                    (pad != null && pad.buttonSouth.wasPressedThisFrame);
        if (next) Advance();
    }

    Text CurrentText => showingPanels ? panelText : lineText;

    static string NextHint => ZodiacInput.UsingGamepad
        ? $"{ZodiacInput.PadButton("buttonSouth")} ▶   ·   {ZodiacInput.PadButton("start")}: bỏ qua"
        : "Space ▶   ·   Esc: bỏ qua";

    /// <summary>Hiện hết chữ, hoặc sang câu / khung kế.</summary>
    public static void Next()
    {
        if (instance != null && instance.open) instance.Advance();
    }

    void Advance()
    {
        if (typed < fullText.Length)
        {
            typed = fullText.Length;
            CurrentText.text = fullText;
            return;
        }
        index++;
        if (index >= (showingPanels ? pages.Length : lines.Length)) Finish();
        else ShowCurrent();
    }

    void ShowCurrent()
    {
        ShownCount++;
        typed = 0f;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.swing, 0.25f, 0.1f);
        if (showingPanels) ShowPanel(pages[index]);
        else ShowLine(lines[index]);
        CurrentText.text = "";
    }

    void ShowLine(ZodiacStory.Line line)
    {
        var who = Speaker(line.speaker, out Sprite face);
        dialogueHint.text = NextHint;
        speakerText.text = who;
        portrait.transform.parent.gameObject.SetActive(face != null);
        if (face != null)
        {
            portrait.sprite = face;
            // Chân dung: phóng to phần trên của ảnh nhân vật (mặt), ảnh biểu tượng thì vừa khung
            bool character = line.speaker != ZodiacStory.Fox;
            ZodiacUi.Place(portrait.rectTransform, new Vector2(0.5f, character ? 1f : 0.5f), Vector2.zero,
                           character ? new Vector2(360f, 520f) : new Vector2(200f, 200f));
            portrait.rectTransform.pivot = new Vector2(0.5f, character ? 0.82f : 0.5f);
        }
        bool narrator = line.speaker == ZodiacStory.Narrator;
        float left = face != null ? 270f : 50f;
        speakerText.rectTransform.anchoredPosition = new Vector2(left + 500f, -40f);
        lineText.rectTransform.anchoredPosition = new Vector2(left + 590f, narrator ? -110f : -140f);
        lineText.fontStyle = narrator ? FontStyle.Italic : FontStyle.Normal;
        fullText = line.text;
    }

    void ShowPanel(ZodiacStory.Panel p)
    {
        panelTitle.text = p.title;
        panelPage.text = $"{index + 1} / {pages.Length}   ·   {NextHint}";
        picture.sprite = StoryArt.Background(p.art);
        var ic = StoryArt.Icon(p.art);
        icon.gameObject.SetActive(ic != null);
        if (ic != null) icon.sprite = ic;

        var who = new List<Sprite>();
        if (p.figure == "@all")
        {
            var roster = Roster;
            if (roster != null) for (int i = 0; i < roster.Count; i++) if (roster.Get(i)?.sprite != null) who.Add(roster.Get(i).sprite);
        }
        else if (!string.IsNullOrEmpty(p.figure))
        {
            var c = p.figure == ZodiacStory.Me ? ZodiacPlayerController.Current?.Character : Find(p.figure);
            if (c != null && c.sprite != null) who.Add(c.sprite);
        }
        for (int i = 0; i < figures.Count; i++)
        {
            bool on = i < who.Count;
            figures[i].gameObject.SetActive(on);
            if (!on) continue;
            figures[i].sprite = who[i];
            if (who.Count == 1)
                ZodiacUi.Place(figures[i].rectTransform, new Vector2(0.5f, 0f), new Vector2(ic != null ? -420f : 0f, 10f), new Vector2(420f, 520f));
            else
            {
                // Cả 12 con: hai hàng so le
                int row = i % 2, col = i / 2;
                float x = -540f + col * 196f + row * 98f;
                ZodiacUi.Place(figures[i].rectTransform, new Vector2(0.5f, 0f), new Vector2(x, row == 0 ? 150f : 10f), new Vector2(200f, 280f));
            }
            figures[i].rectTransform.pivot = new Vector2(0.5f, 0f);
            figures[i].transform.SetAsLastSibling();
        }
        fullText = p.text;
    }

    void Finish()
    {
        if (!open) return;
        open = false;
        closedFrame = Time.frameCount;
        dialogue.SetActive(false);
        panels.SetActive(false);
        Time.timeScale = previousTimeScale;
        var player = ZodiacPlayerController.Current;
        if (playerWasEnabled && player != null) player.SetControlEnabled(true);
        var callback = done;
        done = null;
        callback?.Invoke();
    }

    // ───────── Người nói ─────────

    static ZodiacRoster Roster => ZodiacPlayerController.Current != null ? ZodiacPlayerController.Current.roster : null;

    static ZodiacCharacter Find(string key)
    {
        var roster = Roster;
        if (roster == null) return null;
        for (int i = 0; i < roster.Count; i++)
        {
            var c = roster.Get(i);
            if (c != null && c.name == key) return c;
        }
        return null;
    }

    static string Speaker(string code, out Sprite face)
    {
        face = null;
        switch (code)
        {
            case ZodiacStory.Narrator: return "";
            case ZodiacStory.Elder: return "Già làng";
            case ZodiacStory.Fox:
                face = StoryArt.Icon(StoryArt.Kind.Fox);
                return "Hồ Tinh";
        }
        var c = code == ZodiacStory.Me ? ZodiacPlayerController.Current?.Character : Find(code);
        if (c == null) return code;
        face = c.sprite;
        return c.displayName;
    }
}
