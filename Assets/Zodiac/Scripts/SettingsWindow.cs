using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Cửa sổ Cài đặt (mở từ menu tạm dừng hoặc màn hình chính): bên trái âm lượng nhạc / hiệu ứng, toàn màn hình,
/// độ phân giải, đồ hoạ, giới hạn khung hình, hiện FPS; bên phải bảng phím — mỗi hành động một ô bàn phím và một ô tay cầm,
/// bấm ô rồi bấm phím mới để đổi (xem <see cref="ZodiacInput"/>). Esc / B tay cầm: đóng. Dựng bằng code mỗi lần mở.
/// </summary>
public class SettingsWindow : MonoBehaviour
{
    public static bool IsOpen => instance != null;
    /// <summary>Khung hình vừa đóng — để phím Esc vừa bấm không mở / đóng thêm menu khác.</summary>
    public static int ClosedFrame { get; private set; } = -10;

    static SettingsWindow instance;

    Action onClosed;
    int openedFrame;
    Text fullscreenText, resolutionText, qualityText, frameText, fpsText, statusText, moveText;
    readonly Text[] keyboardTexts = new Text[ZodiacInput.Rebindable.Length];
    readonly Text[] padTexts = new Text[ZodiacInput.Rebindable.Length];
    GameObject firstSelected;

    /// <summary>Mở cửa sổ; onClosed gọi khi đóng (để nơi mở chọn lại nút của mình cho tay cầm).</summary>
    public static void Open(Action onClosed = null)
    {
        if (instance != null) return;
        var go = new GameObject("SettingsWindow");
        instance = go.AddComponent<SettingsWindow>();
        instance.onClosed = onClosed;
        instance.openedFrame = Time.frameCount;
        instance.Build();
        instance.Refresh();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(instance.firstSelected);
    }

    void OnEnable() => ZodiacInput.Changed += Refresh;

    void OnDisable()
    {
        ZodiacInput.Changed -= Refresh;
        if (ZodiacInput.Rebinding) ZodiacInput.CancelRebind();
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        if (Time.frameCount == openedFrame) return;
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        if (ZodiacInput.Rebinding)
        {
            if (pad != null && pad.startButton.wasPressedThisFrame) ZodiacInput.CancelRebind();
            return;
        }
        if (Time.frameCount <= ZodiacInput.RebindEndFrame + 1) return;   // phím vừa gán / vừa huỷ không tính là đóng
        if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame)) Close();
    }

    public void Close()
    {
        ClosedFrame = Time.frameCount;
        instance = null;
        Destroy(gameObject);
        onClosed?.Invoke();
    }

    void Refresh()
    {
        if (fullscreenText == null) return;
        fullscreenText.text = $"Toàn màn hình: {(Screen.fullScreen ? "Bật" : "Tắt")}";
        resolutionText.text = $"Độ phân giải: {ZodiacSettings.ResolutionLabel}";
        qualityText.text = $"Đồ hoạ: {ZodiacSettings.QualityLabel}";
        frameText.text = $"Khung hình: {ZodiacSettings.FrameCapLabel}";
        fpsText.text = $"Hiện FPS: {(ZodiacSettings.ShowFps ? "Bật" : "Tắt")}";
        for (int i = 0; i < ZodiacInput.Rebindable.Length; i++)
        {
            string action = ZodiacInput.Rebindable[i].action;
            keyboardTexts[i].text = ZodiacInput.Key(action, false);
            padTexts[i].text = ZodiacInput.Key(action, true);
        }
        moveText.text = ZodiacInput.UsingGamepad ? "Di chuyển: cần trái (lên / xuống = đổi làn)"
                                                 : "Di chuyển: ← → (A D) · ↑ ↓ đổi làn";
        if (!ZodiacInput.Rebinding)
            statusText.text = ZodiacInput.UsingGamepad ? $"Chọn ô rồi bấm {ZodiacInput.PadButton("buttonSouth")} để đổi · {ZodiacInput.PadButton("buttonEast")}: đóng"
                                                       : "Bấm vào ô để đổi phím · Esc: đóng";
    }

    // Toàn màn hình / độ phân giải đổi ở cuối khung hình — đợi một chút rồi mới cập nhật chữ.
    void RefreshLater() => StartCoroutine(RefreshAfter());

    System.Collections.IEnumerator RefreshAfter()
    {
        yield return new WaitForSecondsRealtime(0.15f);   // menu tạm dừng để timeScale = 0
        Refresh();
    }

    void StartRebind(int row, bool gamepad)
    {
        if (ZodiacInput.Rebinding || Time.frameCount <= ZodiacInput.RebindEndFrame + 1) return;
        var entry = ZodiacInput.Rebindable[row];
        (gamepad ? padTexts : keyboardTexts)[row].text = "…";
        statusText.text = gamepad ? $"Bấm nút tay cầm mới cho «{entry.label}» · {ZodiacInput.PadButton("start")}: huỷ"
                                  : $"Bấm phím mới cho «{entry.label}» · Esc: huỷ";
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = false;   // không để phím vừa bấm di chuyển ô chọn
        ZodiacInput.Rebind(entry.action, gamepad, () =>
        {
            if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true;
            Refresh();
        });
    }

    // ───────── Giao diện ─────────

    void Build()
    {
        var canvas = ZodiacUi.Canvas(transform, "SettingsCanvas", 600);
        var (_, panel) = ZodiacUi.Window(canvas, "CÀI ĐẶT", new Vector2(1560f, 900f));
        var mid = new Vector2(0.5f, 0.5f);

        // Cột trái: âm thanh & hình ảnh
        const float left = -430f;
        ZodiacUi.MakeText(panel, "ÂM THANH & HÌNH ẢNH", 30, ZodiacUi.Gold, mid, new Vector2(left, 320f), new Vector2(560f, 44f));
        ZodiacUi.MakeText(panel, "Nhạc", 26, ZodiacUi.Ink, mid, new Vector2(left - 190f, 250f), new Vector2(180f, 40f), TextAnchor.MiddleLeft);
        var music = ZodiacUi.MakeSlider(panel, mid, new Vector2(left + 80f, 250f), new Vector2(360f, 28f), ZodiacAudio.MusicVolume, ZodiacAudio.SetMusicVolume);
        ZodiacUi.MakeText(panel, "Hiệu ứng", 26, ZodiacUi.Ink, mid, new Vector2(left - 190f, 180f), new Vector2(180f, 40f), TextAnchor.MiddleLeft);
        ZodiacUi.MakeSlider(panel, mid, new Vector2(left + 80f, 180f), new Vector2(360f, 28f), ZodiacAudio.SfxVolume, v => ZodiacAudio.SetSfxVolume(v));
        firstSelected = music.gameObject;

        var size = new Vector2(540f, 58f);
        fullscreenText = Option(panel, new Vector2(left, 100f), size, () =>
        {
            Screen.fullScreen = !Screen.fullScreen;
            fullscreenText.text = $"Toàn màn hình: {(!Screen.fullScreen ? "Bật" : "Tắt")}";
            RefreshLater();
        });
        resolutionText = Option(panel, new Vector2(left, 25f), size, () =>
        {
            ZodiacSettings.CycleResolution();
            RefreshLater();
        });
        qualityText = Option(panel, new Vector2(left, -50f), size, () => { ZodiacSettings.CycleQuality(); Refresh(); });
        frameText = Option(panel, new Vector2(left, -125f), size, () => { ZodiacSettings.CycleFrameCap(); Refresh(); });
        fpsText = Option(panel, new Vector2(left, -200f), size, () => { ZodiacSettings.ToggleFps(); Refresh(); });
        ZodiacUi.MakeText(panel, "Đồ hoạ Thấp: tắt bớt bóng đổ và hiệu ứng — dùng khi máy giật.\nF3: bật / tắt FPS nhanh trong lúc chơi.",
                          20, ZodiacUi.Muted, mid, new Vector2(left, -280f), new Vector2(560f, 60f));

        // Cột phải: phím điều khiển
        const float right = 330f, keyX = right + 40f, padX = right + 280f;
        ZodiacUi.MakeText(panel, "PHÍM ĐIỀU KHIỂN", 30, ZodiacUi.Gold, mid, new Vector2(right, 320f), new Vector2(760f, 44f));
        ZodiacUi.MakeText(panel, "Hành động", 22, ZodiacUi.Muted, mid, new Vector2(right - 250f, 272f), new Vector2(260f, 32f), TextAnchor.MiddleLeft);
        ZodiacUi.MakeText(panel, "Bàn phím", 22, ZodiacUi.Muted, mid, new Vector2(keyX, 272f), new Vector2(220f, 32f));
        ZodiacUi.MakeText(panel, "Tay cầm", 22, ZodiacUi.Muted, mid, new Vector2(padX, 272f), new Vector2(220f, 32f));
        var keySize = new Vector2(210f, 46f);
        for (int i = 0; i < ZodiacInput.Rebindable.Length; i++)
        {
            int row = i;
            float y = 222f - i * 56f;
            ZodiacUi.MakeText(panel, ZodiacInput.Rebindable[i].label, 24, ZodiacUi.Ink, mid, new Vector2(right - 250f, y), new Vector2(260f, 40f), TextAnchor.MiddleLeft);
            keyboardTexts[i] = Option(panel, new Vector2(keyX, y), keySize, () => StartRebind(row, false));
            padTexts[i] = Option(panel, new Vector2(padX, y), keySize, () => StartRebind(row, true));
        }
        moveText = ZodiacUi.MakeText(panel, "", 22, ZodiacUi.Muted, mid, new Vector2(right, -290f), new Vector2(760f, 32f));
        statusText = ZodiacUi.MakeText(panel, "", 24, ZodiacUi.Gold, mid, new Vector2(right, -335f), new Vector2(760f, 36f));

        ZodiacUi.MakeButton(panel, "Đóng", mid, new Vector2(left, -390f), new Vector2(300f, 62f), Close);
        ZodiacUi.MakeButton(panel, "Đặt lại phím mặc định", mid, new Vector2(right, -390f), new Vector2(400f, 62f), () =>
        {
            if (!ZodiacInput.Rebinding) ZodiacInput.ResetAll();
        });
    }

    /// <summary>Nút có chữ thay đổi được (trả về chữ để cập nhật).</summary>
    static Text Option(Transform parent, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var b = ZodiacUi.MakeButton(parent, "", new Vector2(0.5f, 0.5f), pos, size, onClick);
        var t = b.GetComponentInChildren<Text>();
        t.fontSize = Mathf.RoundToInt(Mathf.Min(26f, size.y * 0.5f));
        return t;
    }
}
