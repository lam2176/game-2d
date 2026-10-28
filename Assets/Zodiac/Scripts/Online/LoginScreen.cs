using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Scene đăng nhập (Zodiac_Login) — mở đầu game, trước màn chọn con giáp. Nền đỏ son, tên game chữ vàng,
/// mười hai con giáp đứng hai bên nhấp nhô, giữa là thẻ có hai thẻ con Đăng nhập / Đăng ký.
/// Có phiên lưu trên máy thì tự vào lại. Xong thì sang <see cref="nextScene"/>. Địa chỉ máy chủ giấu sau nút góc phải dưới.
/// Giao diện dựng bằng code lúc chạy; scene chỉ có camera, EventSystem và component này (ZodiacLoginSceneBuilder).
/// </summary>
public class LoginScreen : MonoBehaviour
{
    public const string SceneName = "Zodiac_Login";

    public ZodiacRoster roster;
    public string nextScene = "Zodiac_Start";

    /// <summary>Lời báo khi bị đưa về màn này (phiên hết hạn, bị khoá) — hiện một lần.</summary>
    public static string PendingMessage;

    static readonly Color Gold = new Color(1f, 0.82f, 0.4f);
    static readonly Color Crimson = new Color(0.7f, 0.11f, 0.08f);
    static readonly Color Card = new Color(1f, 0.97f, 0.91f);
    static readonly Color Ink = new Color(0.27f, 0.15f, 0.09f);
    static readonly Color Muted = new Color(0.6f, 0.5f, 0.42f);
    static readonly Color FieldBack = new Color(0.95f, 0.9f, 0.82f);
    static readonly Color Error = new Color(0.8f, 0.15f, 0.1f);

    const float CardWidth = 640f, LoginHeight = 540f, RowStep = 100f;

    static Sprite rounded, gradient;

    InputField username, password, confirm, server;
    Text status, submitLabel, tabLoginText, tabRegisterText;
    Image tabLogin, tabRegister;
    Button submit;
    RectTransform card;
    CanvasGroup cardGroup;
    GameObject serverRow;
    readonly List<(RectTransform rt, Vector2 home, float phase)> figures = new List<(RectTransform, Vector2, float)>();
    bool register, busy, leaving;
    float shownAt;

    void Start()
    {
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.PlayMusic(res.musicMenu);
        if (ZodiacCloud.LoggedIn)
        {
            Leave();
            return;
        }
        Build();
        SetMode(false);
        if (!string.IsNullOrEmpty(PendingMessage))
        {
            ShowForm(PendingMessage);
            PendingMessage = null;
        }
        else if (ZodiacCloud.HasSavedSession)
        {
            SetBusy(true, "Đang vào lại…");
            ZodiacCloud.Resume(error =>
            {
                SetBusy(false, "");
                if (error == null) Leave();
                else ShowForm(error == ZodiacCloud.Offline ? error : "");
            });
        }
        else ShowForm("");
    }

    // ───────── Dựng giao diện ─────────

    void Build()
    {
        var canvas = ZodiacUi.Canvas(transform, "LoginCanvas", 10);

        var back = Img(canvas, "Background", Color.white, Gradient);
        ZodiacUi.Stretch(back.rectTransform);
        var glow = Img(canvas, "Glow", new Color(1f, 0.6f, 0.25f, 0.28f), TouchArt.Glow);
        ZodiacUi.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1500f, 1100f));

        BuildFigures(canvas);

        var title = Label(canvas, "12 CON GIÁP", 104, Gold, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1200f, 130f));
        title.fontStyle = FontStyle.Bold;
        title.gameObject.AddComponent<Shadow>().effectColor = new Color(0.25f, 0.02f, 0.02f, 0.9f);
        title.GetComponent<Shadow>().effectDistance = new Vector2(0f, -6f);
        var line = Img(canvas, "Ornament", new Color(Gold.r, Gold.g, Gold.b, 0.7f), null);
        ZodiacUi.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -188f), new Vector2(320f, 3f));
        var gem = Img(canvas, "Gem", Gold, null);
        ZodiacUi.Place(gem.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -188f), new Vector2(16f, 16f));
        gem.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        // Thẻ giữa
        var cardImage = Img(canvas, "Card", Card, Rounded);
        card = cardImage.rectTransform;
        ZodiacUi.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(CardWidth, LoginHeight));
        card.pivot = new Vector2(0.5f, 1f);
        card.anchoredPosition = new Vector2(0f, LoginHeight * 0.5f - 70f);
        cardImage.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -10f);
        cardImage.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.35f);
        cardGroup = cardImage.gameObject.AddComponent<CanvasGroup>();

        // Hai thẻ con
        var tabs = Img(card, "Tabs", FieldBack, Rounded);
        ZodiacUi.Place(tabs.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(560f, 72f));
        (tabLogin, tabLoginText) = Tab(tabs.transform, "Đăng nhập", -140f, () => SetMode(false));
        (tabRegister, tabRegisterText) = Tab(tabs.transform, "Đăng ký", 140f, () => SetMode(true));

        username = Field(card, "Tên đăng nhập", -170f, InputField.ContentType.Standard, 20);
        password = Field(card, "Mật khẩu", -170f - RowStep, InputField.ContentType.Password, 72);
        confirm = Field(card, "Nhập lại mật khẩu", -170f - RowStep * 2f, InputField.ContentType.Password, 72);

        status = Label(card, "", 24, Error, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(560f, 40f));

        var submitImage = Img(card, "Submit", Color.white, Rounded);
        ZodiacUi.Place(submitImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(560f, 84f));
        submit = submitImage.gameObject.AddComponent<Button>();
        submit.targetGraphic = submitImage;
        Tint(submit, Crimson);
        submit.onClick.AddListener(ZodiacUi.Click);
        submit.onClick.AddListener(Submit);
        submitLabel = Label(submitImage.transform, "", 32, Gold, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        submitLabel.fontStyle = FontStyle.Bold;
        ZodiacUi.Stretch(submitLabel.rectTransform);

        // Địa chỉ máy chủ: nút nhỏ góc phải dưới mở / đóng một ô dưới thẻ
        var serverButton = Img(canvas, "ServerButton", new Color(1f, 1f, 1f, 0.12f), Rounded);
        ZodiacUi.Place(serverButton.rectTransform, new Vector2(1f, 0f), new Vector2(-120f, 50f), new Vector2(180f, 52f));
        var sb = serverButton.gameObject.AddComponent<Button>();
        sb.targetGraphic = serverButton;
        sb.onClick.AddListener(ZodiacUi.Click);
        sb.onClick.AddListener(() => serverRow.SetActive(!serverRow.activeSelf));
        var sbText = Label(serverButton.transform, "Máy chủ", 22, new Color(1f, 0.9f, 0.8f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        ZodiacUi.Stretch(sbText.rectTransform);

        serverRow = new GameObject("ServerRow", typeof(RectTransform));
        serverRow.transform.SetParent(canvas, false);
        ZodiacUi.Place((RectTransform)serverRow.transform, new Vector2(1f, 0f), new Vector2(-460f, 120f), new Vector2(560f, 60f));
        server = Field(serverRow.transform, "http://192.168.1.x:5080", 0f, InputField.ContentType.Standard, 120);
        var srt = (RectTransform)server.transform;
        ZodiacUi.Stretch(srt);
        server.textComponent.fontSize = ((Text)server.placeholder).fontSize = 24;
        server.keyboardType = TouchScreenKeyboardType.URL;
        server.text = ZodiacCloud.Server;
        server.onEndEdit.AddListener(v => ZodiacCloud.Server = v);
        serverRow.SetActive(false);

        shownAt = Time.unscaledTime;
    }

    /// <summary>Mười hai con giáp: sáu con mỗi bên, xếp so le hai cột.</summary>
    void BuildFigures(Transform canvas)
    {
        if (roster == null) return;
        Vector2[] spots =
        {
            new Vector2(170f, 250f), new Vector2(400f, 170f), new Vector2(150f, -10f),
            new Vector2(390f, -90f), new Vector2(180f, -270f), new Vector2(420f, -330f),
        };
        for (int i = 0; i < roster.Count && i < 12; i++)
        {
            var c = roster.Get(i);
            if (c == null || c.sprite == null) continue;
            bool left = i < 6;
            var spot = spots[i % 6];
            var anchor = new Vector2(left ? 0f : 1f, 0.5f);
            var home = new Vector2(left ? spot.x : -spot.x, spot.y);
            var img = Img(canvas, c.name, Color.white, c.sprite);
            img.preserveAspect = true;
            ZodiacUi.Place(img.rectTransform, anchor, home, new Vector2(230f, 230f));
            if (!left) img.rectTransform.localScale = new Vector3(-1f, 1f, 1f);   // bên phải quay mặt vào giữa
            figures.Add((img.rectTransform, home, i * 0.7f));
        }
    }

    (Image, Text) Tab(Transform parent, string label, float x, UnityEngine.Events.UnityAction onClick)
    {
        var img = Img(parent, label, Crimson, Rounded);
        ZodiacUi.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(272f, 60f));
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(ZodiacUi.Click);
        b.onClick.AddListener(onClick);
        var t = Label(img.transform, label, 28, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        t.fontStyle = FontStyle.Bold;
        ZodiacUi.Stretch(t.rectTransform);
        return (img, t);
    }

    InputField Field(Transform parent, string hint, float y, InputField.ContentType type, int limit)
    {
        var back = Img(parent, hint, Color.white, Rounded);
        ZodiacUi.Place(back.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(560f, 76f));
        var text = Label(back.transform, "", 30, Ink, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;
        Inset(text.rectTransform);
        var placeholder = Label(back.transform, hint, 28, Muted, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        placeholder.alignment = TextAnchor.MiddleLeft;
        Inset(placeholder.rectTransform);

        var field = back.gameObject.AddComponent<InputField>();
        field.targetGraphic = back;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.characterLimit = limit;
        field.lineType = InputField.LineType.SingleLine;
        field.contentType = type;
        if (type == InputField.ContentType.Password) field.inputType = InputField.InputType.Password;
        else field.keyboardType = TouchScreenKeyboardType.ASCIICapable;
        var colors = field.colors;
        colors.normalColor = FieldBack;
        colors.highlightedColor = new Color(0.98f, 0.94f, 0.87f);
        colors.selectedColor = colors.pressedColor = Color.white;
        colors.disabledColor = new Color(0.9f, 0.86f, 0.8f);
        field.colors = colors;
        field.onValueChanged.AddListener(_ => { if (!busy) status.text = ""; });
        return field;
    }

    static void Inset(RectTransform rt)
    {
        ZodiacUi.Stretch(rt);
        rt.offsetMin = new Vector2(28f, 4f);
        rt.offsetMax = new Vector2(-28f, -4f);
    }

    static void Tint(Button b, Color color)
    {
        var colors = b.colors;
        colors.normalColor = color;
        colors.highlightedColor = colors.selectedColor = Color.Lerp(color, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        colors.disabledColor = Color.Lerp(color, Color.gray, 0.6f);
        b.colors = colors;
    }

    static Image Img(Transform parent, string name, Color color, Sprite sprite)
    {
        var img = ZodiacUi.MakeImage(parent, name, color);
        img.sprite = sprite;
        if (sprite == Rounded) img.type = UnityEngine.UI.Image.Type.Sliced;
        return img;
    }

    /// <summary>Chữ trơn (không viền đen như ZodiacUi.MakeText).</summary>
    static Text Label(Transform parent, string content, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 box)
    {
        var t = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = ZodiacUi.Font;
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        ZodiacUi.Place(t.rectTransform, anchor, pos, box);
        return t;
    }

    // ───────── Trạng thái ─────────

    void SetMode(bool registering)
    {
        if (busy) return;
        register = registering;
        confirm.gameObject.SetActive(registering);
        card.sizeDelta = new Vector2(CardWidth, LoginHeight + (registering ? RowStep : 0f));
        submitLabel.text = registering ? "TẠO TÀI KHOẢN" : "VÀO GAME";
        tabLogin.color = registering ? Color.clear : Crimson;
        tabRegister.color = registering ? Crimson : Color.clear;
        tabLoginText.color = registering ? Muted : Color.white;
        tabRegisterText.color = registering ? Color.white : Muted;
        status.text = "";
        confirm.text = "";
    }

    void ShowForm(string message)
    {
        status.color = Error;
        status.text = message;
        if (ZodiacInput.Touch) return;   // điện thoại: chạm vào ô mới bật bàn phím ảo
        Focus(username.text.Length == 0 ? username : password);
    }

    static void Focus(InputField field)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(field.gameObject);
        field.ActivateInputField();
    }

    void Update()
    {
        if (card == null) return;
        // Thẻ hiện dần, con giáp nhấp nhô
        float k = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.45f);
        cardGroup.alpha = k;
        card.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, 1f - (1f - k) * (1f - k));
        foreach (var (rt, home, phase) in figures)
        {
            float t = Time.unscaledTime * 1.4f + phase;
            rt.anchoredPosition = home + new Vector2(0f, Mathf.Sin(t) * 10f);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.8f) * 2.5f);
        }

        var kb = Keyboard.current;
        if (kb == null || busy) return;
        if (kb.tabKey.wasPressedThisFrame)
        {
            var order = register ? new[] { username, password, confirm } : new[] { username, password };
            int at = System.Array.FindIndex(order, f => f.isFocused);
            Focus(order[(at + 1) % order.Length]);
        }
        else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            if (server.isFocused) return;
            if (username.isFocused && password.text.Length == 0) Focus(password);
            else if (register && password.isFocused && confirm.text.Length == 0) Focus(confirm);
            else Submit();
        }
    }

    void Submit()
    {
        if (busy) return;
        ZodiacCloud.Server = server.text;
        string user = username.text.Trim().ToLowerInvariant();
        string message =
            user.Length < 3 ? "Nhập tên đăng nhập (ít nhất 3 ký tự)" :
            password.text.Length < 6 ? "Mật khẩu ít nhất 6 ký tự" :
            register && confirm.text != password.text ? "Mật khẩu nhập lại không khớp" : null;
        if (message != null)
        {
            ShowForm(message);
            return;
        }
        SetBusy(true, register ? "Đang tạo tài khoản…" : "Đang đăng nhập…");
        System.Action<string> done = error =>
        {
            SetBusy(false, "");
            if (error == null) Leave();
            else ShowForm(error);
        };
        if (register) ZodiacCloud.Register(user, password.text, done);
        else ZodiacCloud.Login(user, password.text, done);
    }

    void SetBusy(bool on, string message)
    {
        busy = on;
        status.color = on ? Muted : Error;
        status.text = message;
        username.interactable = password.interactable = confirm.interactable = server.interactable = submit.interactable = !on;
    }

    void Leave()
    {
        if (leaving) return;
        leaving = true;
        ScreenFader.LoadScene(nextScene);
    }

    // ───────── Hình vẽ ─────────

    /// <summary>Chữ nhật bo góc trắng (9 mảnh) — tô màu bằng Image.color.</summary>
    static Sprite Rounded
    {
        get
        {
            if (rounded != null) return rounded;
            const int size = 64;
            const float radius = 26f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var q = new Vector2(Mathf.Abs(x + 0.5f - size * 0.5f), Mathf.Abs(y + 0.5f - size * 0.5f)) - Vector2.one * (size * 0.5f - radius);
                float d = Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
            }
            tex.SetPixels(px);
            tex.Apply();
            rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                                    new Vector4(radius, radius, radius, radius));
            return rounded;
        }
    }

    /// <summary>Nền đỏ son: sáng ở giữa trên, sẫm dần xuống đáy và ra hai mép.</summary>
    static Sprite Gradient
    {
        get
        {
            if (gradient != null) return gradient;
            const int w = 64, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            var top = new Color(0.55f, 0.1f, 0.07f);
            var bottom = new Color(0.16f, 0.04f, 0.04f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = y / (h - 1f);
                float edge = Mathf.Abs(x / (w - 1f) - 0.5f) * 2f;
                var c = Color.Lerp(bottom, top, v * v * 0.6f + v * 0.4f);
                px[y * w + x] = Color.Lerp(c, bottom, edge * edge * 0.55f);
            }
            tex.SetPixels(px);
            tex.Apply();
            gradient = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
            return gradient;
        }
    }
}
