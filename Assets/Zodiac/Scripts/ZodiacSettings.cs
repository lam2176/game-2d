using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cài đặt hình ảnh (lưu PlayerPrefs, áp lúc mở game): chất lượng đồ hoạ (Thấp = bộ URP Mobile, Cao = bộ PC),
/// giới hạn khung hình, độ phân giải, hiện FPS (F3 bật/tắt nhanh). Toàn màn hình và độ phân giải do Unity tự nhớ.
/// </summary>
public static class ZodiacSettings
{
    const string QualityKey = "zodiac.quality";
    const string FrameKey = "zodiac.frameCap";
    const string FpsKey = "zodiac.showFps";

    /// <summary>Các mức giới hạn khung hình: 0 = theo tần số màn hình (VSync), còn lại là số khung/giây (-1 = không giới hạn).</summary>
    static readonly int[] FrameCaps = { 0, 60, 120, -1 };

    public static int FrameCapIndex { get; private set; }
    public static bool ShowFps { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        int quality = PlayerPrefs.GetInt(QualityKey, -1);
        if (quality >= 0 && quality < QualitySettings.names.Length) QualitySettings.SetQualityLevel(quality, true);
        FrameCapIndex = Mathf.Clamp(PlayerPrefs.GetInt(FrameKey, 0), 0, FrameCaps.Length - 1);
        ApplyFrameCap();
        ShowFps = PlayerPrefs.GetInt(FpsKey, 0) == 1;
        var go = new GameObject("FpsCounter");
        Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideInHierarchy;
        go.AddComponent<FpsCounter>();
    }

    // ───────── Chất lượng ─────────

    public static string QualityLabel
    {
        get
        {
            int level = QualitySettings.GetQualityLevel();
            string name = QualitySettings.names[level];
            if (name.Contains("Mobile")) return "Thấp";
            if (name.Contains("PC")) return "Cao";
            return name;
        }
    }

    public static void CycleQuality()
    {
        int level = (QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length;
        QualitySettings.SetQualityLevel(level, true);
        ApplyFrameCap();   // đổi mức chất lượng làm Unity đặt lại vSyncCount theo mức đó
        PlayerPrefs.SetInt(QualityKey, level);
        PlayerPrefs.Save();
    }

    // ───────── Khung hình ─────────

    public static string FrameCapLabel
    {
        get
        {
            int cap = FrameCaps[FrameCapIndex];
            return cap == 0 ? "Theo màn hình" : cap < 0 ? "Không giới hạn" : $"{cap} khung/giây";
        }
    }

    public static void CycleFrameCap()
    {
        FrameCapIndex = (FrameCapIndex + 1) % FrameCaps.Length;
        ApplyFrameCap();
        PlayerPrefs.SetInt(FrameKey, FrameCapIndex);
        PlayerPrefs.Save();
    }

    static void ApplyFrameCap()
    {
        int cap = FrameCaps[FrameCapIndex];
        QualitySettings.vSyncCount = cap == 0 ? 1 : 0;
        Application.targetFrameRate = cap == 0 ? -1 : cap;
    }

    // ───────── Độ phân giải ─────────

    static List<Vector2Int> Resolutions() =>
        Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)).Where(r => r.y >= 600).Distinct().OrderBy(r => r.x * r.y).ToList();

    public static string ResolutionLabel => $"{Screen.width} × {Screen.height}";

    public static void CycleResolution()
    {
        var list = Resolutions();
        if (list.Count == 0) return;
        int current = list.FindIndex(r => r.x == Screen.width && r.y == Screen.height);
        var next = list[(current + 1) % list.Count];
        Screen.SetResolution(next.x, next.y, Screen.fullScreenMode);
    }

    // ───────── FPS ─────────

    public static void ToggleFps()
    {
        ShowFps = !ShowFps;
        PlayerPrefs.SetInt(FpsKey, ShowFps ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>Số khung/giây ở góc trái trên (trung bình trượt), F3 bật/tắt.</summary>
    class FpsCounter : MonoBehaviour
    {
        float smoothed;
        GUIStyle style;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f3Key.wasPressedThisFrame) ToggleFps();
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) smoothed = smoothed <= 0f ? dt : Mathf.Lerp(smoothed, dt, 0.05f);
        }

        void OnGUI()
        {
            if (!ShowFps || smoothed <= 0f) return;
            style ??= new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 45), fontStyle = FontStyle.Bold };
            float fps = 1f / smoothed;
            style.normal.textColor = fps >= 55f ? new Color(0.6f, 1f, 0.6f) : fps >= 30f ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.45f, 0.4f);
            GUI.Label(new Rect(10f, 6f, 300f, 40f), $"{fps:0} FPS · {smoothed * 1000f:0.0} ms", style);
        }
    }
}
