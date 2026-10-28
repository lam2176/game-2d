using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

/// <summary>
/// Phím điều khiển dùng chung: nạp / lưu phím đã đổi (PlayerPrefs), đổi phím tương tác cho các hành động trong map Player,
/// nhớ người chơi đang dùng bàn phím hay tay cầm để mọi chữ gợi ý (HUD, biển, trạm làng, thoại) hiện đúng nút.
/// Mỗi hành động có một phím "chính" cho bàn phím (QWER…) và một nút chính cho tay cầm — đó là phím được hiện và được đổi.
/// </summary>
public static class ZodiacInput
{
    public struct Entry
    {
        public string action;
        public string label;
        public string keyboardPath;   // phím chính mặc định trên bàn phím (đường dẫn gốc trong asset)
        public Entry(string action, string label, string keyboardPath)
        {
            this.action = action;
            this.label = label;
            this.keyboardPath = keyboardPath;
        }
    }

    /// <summary>Các hành động đổi được phím, theo thứ tự hiện trong cửa sổ cài đặt.</summary>
    public static readonly Entry[] Rebindable =
    {
        new Entry("Attack", "Đánh", "<Keyboard>/q"),
        new Entry("Dodge", "Lăn né", "<Keyboard>/w"),
        new Entry("Skill", "Chiêu", "<Keyboard>/e"),
        new Entry("Ultimate", "Tuyệt kỹ", "<Keyboard>/r"),
        new Entry("Jump", "Nhảy", "<Keyboard>/space"),
        new Entry("Sprint", "Chạy nhanh", "<Keyboard>/leftShift"),
        new Entry("Interact", "Túm · Tương tác", "<Keyboard>/f"),
        new Entry("Previous", "Dùng nhanh 1", "<Keyboard>/1"),
        new Entry("Next", "Dùng nhanh 2", "<Keyboard>/2"),
    };

    // Phím giữ cho việc khác (menu, hành trang, nhiệm vụ, chơi lại, di chuyển lên/xuống) — không cho gán.
    static readonly string[] ReservedKeys =
    {
        "<Keyboard>/escape", "<Keyboard>/tab", "<Keyboard>/i", "<Keyboard>/j", "<Keyboard>/f5", "<Keyboard>/anyKey",
        "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow",
    };
    static readonly string[] ReservedPad =
    {
        "<Gamepad>/start", "<Gamepad>/select", "<Gamepad>/leftStick", "<Gamepad>/rightStick",
    };

    const string PrefsKey = "zodiac.bindings";

    /// <summary>Đổi phím xong / đặt lại / người chơi chuyển giữa bàn phím và tay cầm — làm mới chữ gợi ý.</summary>
    public static event Action Changed;

    public static bool UsingGamepad { get; private set; }
    /// <summary>Đang chạy trên điện thoại — có điều khiển cảm ứng (<see cref="TouchControls"/>).</summary>
    public static bool Touch { get; private set; }
    /// <summary>Lần bấm gần nhất đến từ nút cảm ứng trên màn hình (tay cầm ảo) — gợi ý hiện tên nút cảm ứng.</summary>
    public static bool UsingTouch { get; private set; }
    public static bool Rebinding => operation != null;
    /// <summary>Khung hình vừa xong đổi phím — để phím Esc / B vừa bấm không đóng thêm cửa sổ.</summary>
    public static int RebindEndFrame { get; private set; } = -10;

    static bool loaded;
    static InputActionRebindingExtensions.RebindingOperation operation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        loaded = false;
        operation = null;
        Touch = Application.isMobilePlatform;
        UsingGamepad = UsingTouch = Touch;
        InputSystem.onActionChange -= OnActionChange;
        InputSystem.onActionChange += OnActionChange;
        EnsureLoaded();
    }

    static void EnsureLoaded()
    {
        if (loaded || InputSystem.actions == null) return;
        loaded = true;
        string json = PlayerPrefs.GetString(PrefsKey, "");
        if (string.IsNullOrEmpty(json)) return;
        try { InputSystem.actions.LoadBindingOverridesFromJson(json); }
        catch (Exception e) { Debug.LogWarning($"Không nạp được phím đã lưu: {e.Message}"); }
    }

    static void OnActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed) return;
        var device = (obj as InputAction)?.activeControl?.device;
        bool pad = device is Gamepad;
        if (!pad && !(device is Keyboard) && !(device is Mouse)) return;
        bool touch = pad && !device.native;   // tay cầm ảo do OnScreenControl tạo
        if (pad == UsingGamepad && touch == UsingTouch) return;
        UsingGamepad = pad;
        UsingTouch = touch;
        Changed?.Invoke();
    }

    public static InputAction Find(string action)
    {
        EnsureLoaded();
        return InputSystem.actions != null ? InputSystem.actions.FindAction("Player/" + action) : null;
    }

    /// <summary>Vị trí phím chính của hành động (bàn phím: theo đường dẫn mặc định; tay cầm: nút Gamepad đầu tiên). -1 nếu không có.</summary>
    static int PrimaryIndex(InputAction action, bool gamepad)
    {
        if (action == null) return -1;
        string keyboardPath = null;
        foreach (var e in Rebindable)
            if (e.action == action.name) keyboardPath = e.keyboardPath;
        var bindings = action.bindings;
        for (int i = 0; i < bindings.Count; i++)
        {
            var b = bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;
            if (gamepad ? b.path.StartsWith("<Gamepad>/", StringComparison.Ordinal)
                        : string.Equals(b.path, keyboardPath, StringComparison.OrdinalIgnoreCase)) return i;
        }
        return -1;
    }

    /// <summary>Tên phím chính của hành động theo thiết bị đang dùng (vd. "Q", "Space", "X"; cảm ứng: tên nút trên màn hình).</summary>
    public static string Key(string action)
    {
        if (UsingTouch && TouchNames.TryGetValue(action, out var n)) return n;
        return Key(action, UsingGamepad);
    }

    /// <summary>Đường dẫn nút tay cầm đang gán cho hành động (đã tính phím đổi), vd. "&lt;Gamepad&gt;/buttonWest". Null nếu không có.</summary>
    public static string PadPath(string action)
    {
        var a = Find(action);
        int i = PrimaryIndex(a, true);
        return i < 0 ? null : a.bindings[i].effectivePath;
    }

    public static string Key(string action, bool gamepad)
    {
        var a = Find(action);
        int i = PrimaryIndex(a, gamepad);
        return i < 0 ? "—" : PathName(a.bindings[i].effectivePath);
    }

    /// <summary>Tên dễ đọc của một nút tay cầm theo loại tay cầm đang cắm (Xbox / PlayStation), vd. "start" → "Start" / "Options".</summary>
    public static string PadButton(string control) =>
        UsingTouch && TouchNames.TryGetValue(control, out var n) ? n : PathName("<Gamepad>/" + control);

    // Tên nút trên màn hình cảm ứng: theo hành động (Key) và theo nút tay cầm dùng cho menu (PadButton).
    static readonly Dictionary<string, string> TouchNames = new Dictionary<string, string>
    {
        { "Attack", "Đánh" }, { "Dodge", "Né" }, { "Skill", "Chiêu" }, { "Ultimate", "Tuyệt kỹ" }, { "Jump", "Nhảy" },
        { "Sprint", "Chạy" }, { "Interact", "Túm" }, { "Previous", "1" }, { "Next", "2" },
        { "start", "II" }, { "select", "Túi" }, { "buttonSouth", "Chạm" }, { "buttonEast", "Đóng" },
    };

    static readonly Dictionary<string, string> XboxNames = new Dictionary<string, string>
    {
        { "buttonSouth", "A" }, { "buttonEast", "B" }, { "buttonWest", "X" }, { "buttonNorth", "Y" },
        { "leftShoulder", "LB" }, { "rightShoulder", "RB" }, { "leftTrigger", "LT" }, { "rightTrigger", "RT" },
        { "leftStickPress", "L3" }, { "rightStickPress", "R3" }, { "start", "Start" }, { "select", "Back" },
        { "dpad/up", "▲" }, { "dpad/down", "▼" }, { "dpad/left", "◄" }, { "dpad/right", "►" },
    };
    static readonly Dictionary<string, string> PlayStationNames = new Dictionary<string, string>
    {
        { "buttonSouth", "×" }, { "buttonEast", "○" }, { "buttonWest", "□" }, { "buttonNorth", "△" },
        { "leftShoulder", "L1" }, { "rightShoulder", "R1" }, { "leftTrigger", "L2" }, { "rightTrigger", "R2" },
        { "leftStickPress", "L3" }, { "rightStickPress", "R3" }, { "start", "Options" }, { "select", "Share" },
    };
    static readonly Dictionary<string, string> KeyNames = new Dictionary<string, string>
    {
        { "Left Shift", "Shift" }, { "Right Shift", "Shift phải" }, { "Left Control", "Ctrl" }, { "Right Control", "Ctrl phải" },
        { "Left Alt", "Alt" }, { "Right Alt", "Alt phải" }, { "Left Button", "Chuột trái" }, { "Right Button", "Chuột phải" },
        { "Middle Button", "Chuột giữa" },
    };

    static string PathName(string path)
    {
        if (string.IsNullOrEmpty(path)) return "—";
        if (path.StartsWith("<Gamepad>/", StringComparison.Ordinal))
        {
            string control = path.Substring("<Gamepad>/".Length);
            var names = Gamepad.current is DualShockGamepad ? PlayStationNames : XboxNames;
            if (names.TryGetValue(control, out var n) || XboxNames.TryGetValue(control, out n)) return n;
        }
        string s = InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        return KeyNames.TryGetValue(s, out var vn) ? vn : s;
    }

    /// <summary>Thay {Attack}, {Dodge}… trong chữ bằng tên phím đang dùng; {Move} / {Lane} = phím đi ngang / đổi làn.</summary>
    public static string Format(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
        foreach (var e in Rebindable) text = text.Replace("{" + e.action + "}", Key(e.action));
        text = text.Replace("{Move}", UsingTouch ? "Cần ← →" : UsingGamepad ? "Cần trái ← →" : "← →");
        text = text.Replace("{Lane}", UsingTouch ? "Cần ↑ ↓" : UsingGamepad ? "Cần trái ↑ ↓" : "↑ ↓");
        return text;
    }

    // ───────── Đổi phím ─────────

    /// <summary>Chờ người chơi bấm phím mới cho hành động (Esc / Start tay cầm: huỷ). Trùng phím với hành động khác thì đổi chỗ.</summary>
    public static void Rebind(string actionName, bool gamepad, Action done)
    {
        var action = Find(actionName);
        int index = PrimaryIndex(action, gamepad);
        if (index < 0 || Rebinding)
        {
            done?.Invoke();
            return;
        }
        string before = action.bindings[index].effectivePath;
        bool wasEnabled = action.enabled;
        action.Disable();
        operation = action.PerformInteractiveRebinding(index)
            .WithControlsHavingToMatchPath(gamepad ? "<Gamepad>" : "<Keyboard>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f);
        foreach (var path in gamepad ? ReservedPad : ReservedKeys) operation.WithControlsExcluding(path);
        operation
            .OnComplete(_ =>
            {
                EndRebind(action, wasEnabled);
                ResolveConflicts(action, index, before);
                Save();
                done?.Invoke();
            })
            .OnCancel(_ =>
            {
                EndRebind(action, wasEnabled);
                done?.Invoke();
            })
            .Start();
    }

    public static void CancelRebind() => operation?.Cancel();

    static void EndRebind(InputAction action, bool wasEnabled)
    {
        operation?.Dispose();
        operation = null;
        RebindEndFrame = Time.frameCount;
        if (wasEnabled) action.Enable();
    }

    /// <summary>
    /// Phím mới đang thuộc chỗ khác trong map Player: phím chính của hành động khác thì nhận phím cũ (đổi chỗ),
    /// phím phụ (J, Enter, A/D…) thì tắt đi để một phím không kích hai việc.
    /// </summary>
    static void ResolveConflicts(InputAction changed, int changedIndex, string oldPath)
    {
        string newPath = changed.bindings[changedIndex].effectivePath;
        if (string.Equals(newPath, oldPath, StringComparison.OrdinalIgnoreCase)) return;
        foreach (var other in changed.actionMap.actions)
        {
            var bindings = other.bindings;
            for (int j = 0; j < bindings.Count; j++)
            {
                if (other == changed && j == changedIndex) continue;
                var b = bindings[j];
                if (b.isComposite || !string.Equals(b.effectivePath, newPath, StringComparison.OrdinalIgnoreCase)) continue;
                bool primary = j == PrimaryIndex(other, false) || j == PrimaryIndex(other, true);
                if (!primary) other.ApplyBindingOverride(j, "");
                else if (string.Equals(b.path, oldPath, StringComparison.OrdinalIgnoreCase)) other.RemoveBindingOverride(j);
                else other.ApplyBindingOverride(j, oldPath);
            }
        }
    }

    static void Save()
    {
        PlayerPrefs.SetString(PrefsKey, InputSystem.actions.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    /// <summary>Trả mọi phím về mặc định.</summary>
    public static void ResetAll()
    {
        EnsureLoaded();
        InputSystem.actions?.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
