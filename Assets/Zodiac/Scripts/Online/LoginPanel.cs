using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Màn đăng nhập / đăng ký phủ lên màn hình chính (<see cref="ZodiacStartMenu"/>): ô tên đăng nhập, ô mật khẩu,
/// nút Đăng nhập / Đăng ký và dòng báo lỗi. Có phiên lưu trên máy thì tự vào lại, chỉ hiện "Đang kết nối…".
/// Tab chuyển ô, Enter đăng nhập. Xong thì gọi onDone và tự ẩn.
/// </summary>
public class LoginPanel : MonoBehaviour
{
    static readonly Color Paper = new Color(1f, 0.96f, 0.86f);
    static readonly Color InkBrown = new Color(0.25f, 0.15f, 0.1f);
    static readonly Color Red = new Color(0.8f, 0.15f, 0.12f);

    public static bool IsOpen { get; private set; }

    InputField username, password, server;
    Text status;
    Button loginButton, registerButton;
    Action onDone;
    bool busy;

    public static LoginPanel Build(Transform canvas, Action onDone)
    {
        var dim = new GameObject("LoginPanel", typeof(RectTransform)).AddComponent<Image>();
        dim.transform.SetParent(canvas, false);
        dim.color = new Color(0.06f, 0.03f, 0.02f, 0.94f);
        ZodiacUi.Stretch(dim.rectTransform);
        var panel = dim.gameObject.AddComponent<LoginPanel>();
        panel.onDone = onDone;
        panel.Layout(dim.transform);
        return panel;
    }

    void Layout(Transform root)
    {
        var box = new GameObject("Box", typeof(RectTransform)).AddComponent<Image>();
        box.transform.SetParent(root, false);
        box.color = Paper;
        ZodiacUi.Place(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 760f));
        var title = ZodiacUi.MakeText(box.transform, "12 CON GIÁP", 64, new Color(0.7f, 0.08f, 0.08f), new Vector2(0.5f, 1f),
                                      new Vector2(0f, -70f), new Vector2(760f, 90f));
        title.fontStyle = FontStyle.Bold;
        ZodiacUi.MakeText(box.transform, "Đăng nhập để lưu tiến trình — mỗi tài khoản một con giáp", 24, InkBrown,
                          new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(760f, 40f));

        username = Field(box.transform, "Tên đăng nhập", new Vector2(0f, -215f), false);
        password = Field(box.transform, "Mật khẩu", new Vector2(0f, -315f), true);

        status = ZodiacUi.MakeText(box.transform, "", 24, Red, new Vector2(0.5f, 1f), new Vector2(0f, -395f), new Vector2(760f, 60f));
        status.horizontalOverflow = HorizontalWrapMode.Wrap;

        loginButton = Button(box.transform, "Đăng nhập", new Vector2(-180f, 200f), () => Submit(false));
        registerButton = Button(box.transform, "Đăng ký", new Vector2(180f, 200f), () => Submit(true));
        ZodiacUi.MakeText(box.transform, "Tên: chữ thường không dấu, số, _ (3–20 ký tự) · Mật khẩu từ 6 ký tự", 20,
                          new Color(0.45f, 0.35f, 0.28f), new Vector2(0.5f, 0f), new Vector2(0f, 125f), new Vector2(760f, 40f));

        // Địa chỉ máy chủ (máy tính chạy Server/ZodiacServer) — đáy khung, chữ nhỏ
        ZodiacUi.MakeText(box.transform, "Máy chủ", 22, new Color(0.45f, 0.35f, 0.28f), new Vector2(0.5f, 0f), new Vector2(-310f, 55f), new Vector2(140f, 40f));
        server = Field(box.transform, "http://192.168.1.x:5080", Vector2.zero, false, 22, new Vector2(560f, 52f));
        var srt = server.GetComponent<RectTransform>();
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(70f, 55f);
        server.text = ZodiacCloud.Server;
        server.onEndEdit.AddListener(v => ZodiacCloud.Server = v);
    }

    InputField Field(Transform parent, string hint, Vector2 pos, bool secret) =>
        Field(parent, hint, pos, secret, 32, new Vector2(620f, 76f));

    /// <param name="fontSize">Nhỏ hơn 32 = ô địa chỉ máy chủ (chữ tự do, bàn phím URL).</param>
    InputField Field(Transform parent, string hint, Vector2 pos, bool secret, int fontSize, Vector2 size)
    {
        bool url = fontSize < 32;
        var back = new GameObject(hint, typeof(RectTransform)).AddComponent<Image>();
        back.transform.SetParent(parent, false);
        back.color = Color.white;
        ZodiacUi.Place(back.rectTransform, new Vector2(0.5f, 1f), pos, size);
        var outline = back.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.55f, 0.35f, 0.2f);
        outline.effectDistance = new Vector2(2f, -2f);

        var text = ZodiacUi.MakeText(back.transform, "", fontSize, InkBrown, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;
        Inset(text.rectTransform);
        var placeholder = ZodiacUi.MakeText(back.transform, hint, fontSize - 2, new Color(0.6f, 0.52f, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.fontStyle = FontStyle.Italic;
        Inset(placeholder.rectTransform);

        var field = back.gameObject.AddComponent<InputField>();
        field.targetGraphic = back;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.characterLimit = secret ? 72 : url ? 120 : 20;
        field.lineType = InputField.LineType.SingleLine;
        if (secret)
        {
            field.contentType = InputField.ContentType.Password;
            field.inputType = InputField.InputType.Password;
        }
        else if (url)
        {
            field.contentType = InputField.ContentType.Standard;
            field.keyboardType = TouchScreenKeyboardType.URL;
        }
        else
        {
            field.contentType = InputField.ContentType.Standard;   // máy chủ kiểm tra ký tự hợp lệ
            field.keyboardType = TouchScreenKeyboardType.ASCIICapable;
        }
        field.onValueChanged.AddListener(_ => status.text = "");
        return field;
    }

    static void Inset(RectTransform rt)
    {
        ZodiacUi.Stretch(rt);
        rt.offsetMin = new Vector2(22f, 6f);
        rt.offsetMax = new Vector2(-22f, -6f);
    }

    Button Button(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var b = ZodiacUi.MakeButton(parent, label, new Vector2(0.5f, 0f), pos, new Vector2(320f, 80f), onClick);
        b.GetComponent<Image>().color = Red;
        return b;
    }

    void OnEnable() => IsOpen = true;
    void OnDisable() => IsOpen = false;

    void Start()
    {
        if (ZodiacCloud.HasSavedSession)
        {
            SetBusy(true, "Đang kết nối…");
            ZodiacCloud.Resume(error =>
            {
                SetBusy(false, "");
                if (error == null) Finish();
                else ShowForm(error);
            });
        }
        else ShowForm("");
    }

    void ShowForm(string message)
    {
        status.text = message;
        if (ZodiacInput.Touch) return;   // điện thoại: chạm vào ô mới bật bàn phím ảo
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(username.gameObject);
        username.ActivateInputField();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || busy) return;
        if (kb.tabKey.wasPressedThisFrame)
        {
            var next = username.isFocused ? password : username;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(next.gameObject);
            next.ActivateInputField();
        }
        else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            if (username.isFocused && password.text.Length == 0)
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(password.gameObject);
                password.ActivateInputField();
            }
            else Submit(false);
        }
    }

    void Submit(bool register)
    {
        if (busy) return;
        ZodiacCloud.Server = server.text;
        string user = username.text.Trim().ToLowerInvariant();
        if (user.Length < 3)
        {
            status.text = "Nhập tên đăng nhập (ít nhất 3 ký tự)";
            return;
        }
        if (password.text.Length < 6)
        {
            status.text = "Mật khẩu ít nhất 6 ký tự";
            return;
        }
        SetBusy(true, register ? "Đang tạo tài khoản…" : "Đang đăng nhập…");
        Action<string> done = error =>
        {
            SetBusy(false, error ?? "");
            if (error == null) Finish();
        };
        if (register) ZodiacCloud.Register(user, password.text, done);
        else ZodiacCloud.Login(user, password.text, done);
    }

    void SetBusy(bool on, string message)
    {
        busy = on;
        status.text = message;
        status.color = on ? InkBrown : Red;
        username.interactable = password.interactable = server.interactable = loginButton.interactable = registerButton.interactable = !on;
    }

    void Finish()
    {
        password.text = "";
        gameObject.SetActive(false);
        onDone?.Invoke();
    }

    /// <summary>Hiện lại màn đăng nhập (đăng xuất, phiên hết hạn).</summary>
    public void Open(string message)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        SetBusy(false, "");
        ShowForm(message);
    }
}
