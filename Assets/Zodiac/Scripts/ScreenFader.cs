using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Lớp phủ toàn màn hình: mờ đen khi chuyển scene / hồi sinh, loé đỏ viền khi người chơi trúng đòn.
/// Tự tạo khi gọi lần đầu và giữ qua các scene; scene mới tải xong thì tự sáng dần lên.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    static ScreenFader host;
    Image black, flash;
    Text message;
    Coroutine fading, flashing;

    static ScreenFader Host
    {
        get
        {
            if (host != null) return host;
            var go = new GameObject("ScreenFader");
            DontDestroyOnLoad(go);
            host = go.AddComponent<ScreenFader>();
            return host;
        }
    }

    void Awake()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        flash = MakeImage("Flash");
        black = MakeImage("Black");
        // Chữ giữa màn hình lúc tối hẳn (chuyển khu: "Đang tới khu tiếp theo…")
        message = new GameObject("Message", typeof(RectTransform)).AddComponent<Text>();
        message.transform.SetParent(transform, false);
        message.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        message.fontSize = 46;
        message.alignment = TextAnchor.MiddleCenter;
        message.horizontalOverflow = HorizontalWrapMode.Overflow;
        message.color = new Color(1f, 0.92f, 0.75f, 0f);
        message.raycastTarget = false;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        var mrt = message.rectTransform;
        mrt.anchorMin = mrt.anchorMax = new Vector2(0.5f, 0.5f);
        mrt.sizeDelta = new Vector2(1600f, 200f);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    Image MakeImage(string name)
    {
        var img = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(transform, false);
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;
        return img;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (black.color.a > 0f) Fade(0f, 0.45f);
    }

    /// <summary>Mờ đen rồi tải scene (scene mới tự sáng lên).</summary>
    public static void LoadScene(string sceneName, float fadeTime = 0.35f)
    {
        if (!Application.isPlaying) { SceneManager.LoadScene(sceneName); return; }
        Host.Fade(1f, fadeTime, () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        });
    }

    /// <summary>Mờ đen, chạy action (dịch chuyển người chơi…), rồi sáng lại.</summary>
    public static void Blink(Action whileDark, float fadeTime = 0.25f) => Blink(whileDark, fadeTime, null, 0f);

    /// <summary>
    /// Như <see cref="Blink(Action, float)"/> nhưng hiện chữ giữa màn hình đen và giữ tối thêm hold giây
    /// (màn chuyển khu kiểu "đang tải").
    /// </summary>
    public static void Blink(Action whileDark, float fadeTime, string text, float hold)
    {
        var h = Host;
        h.message.text = text ?? "";
        h.Fade(1f, fadeTime, () =>
        {
            whileDark?.Invoke();
            h.StartCoroutine(h.HoldThenFade(fadeTime, hold));
        });
    }

    IEnumerator HoldThenFade(float fadeTime, float hold)
    {
        if (hold > 0f) yield return new WaitForSecondsRealtime(hold);
        Fade(0f, fadeTime);
    }

    /// <summary>Loé màu (đỏ khi bị thương) rồi tắt dần.</summary>
    public static void Flash(Color color, float duration = 0.3f)
    {
        if (!Application.isPlaying) return;
        var h = Host;
        if (h.flashing != null) h.StopCoroutine(h.flashing);
        h.flashing = h.StartCoroutine(h.FlashRoutine(color, duration));
    }

    void Fade(float target, float time, Action done = null)
    {
        if (fading != null) StopCoroutine(fading);
        fading = StartCoroutine(FadeRoutine(target, time, done));
    }

    IEnumerator FadeRoutine(float target, float time, Action done)
    {
        black.raycastTarget = target > 0f;
        float start = black.color.a;
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            black.color = new Color(0f, 0f, 0f, Mathf.Lerp(start, target, t / time));
            SyncMessage();
            yield return null;
        }
        black.color = new Color(0f, 0f, 0f, target);
        SyncMessage();
        fading = null;
        done?.Invoke();
    }

    void SyncMessage()
    {
        var c = message.color;
        c.a = black.color.a;
        message.color = c;
        if (black.color.a <= 0f) message.text = "";
    }

        IEnumerator FlashRoutine(Color color, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            flash.color = new Color(color.r, color.g, color.b, color.a * (1f - t / duration));
            yield return null;
        }
        flash.color = Color.clear;
    }
}
