using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thông báo nhỏ góc phải màn hình khi nhặt đồ ("+1 Bình máu nhỏ", màu theo độ hiếm) hoặc có cảnh báo ("Túi đầy!").
/// Tự tạo khi gọi lần đầu, giữ qua các scene; mỗi dòng nổi lên rồi mờ dần.
/// </summary>
public class PickupToast : MonoBehaviour
{
    const int MaxLines = 5;
    const float Lifetime = 2.2f;

    static PickupToast host;
    readonly List<(Text text, float born)> lines = new List<(Text, float)>();
    Font font;

    static PickupToast Host
    {
        get
        {
            if (host != null) return host;
            var go = new GameObject("PickupToast");
            DontDestroyOnLoad(go);
            host = go.AddComponent<PickupToast>();
            return host;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook() => PlayerProfile.Picked += (def, count) =>
        Show($"+{count} {def.displayName}", ItemDefinition.RarityColor(def.rarity));

    void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
    }

    public static void Show(string message, Color color)
    {
        if (!Application.isPlaying) return;
        Host.Add(message, color);
    }

    void Add(string message, Color color)
    {
        var t = new GameObject("Line", typeof(RectTransform)).AddComponent<Text>();
        t.transform.SetParent(transform, false);
        t.font = font;
        t.fontSize = 30;
        t.text = message;
        t.color = color;
        t.alignment = TextAnchor.MiddleRight;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        t.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(700f, 40f);
        lines.Add((t, Time.unscaledTime));
        while (lines.Count > MaxLines)
        {
            Destroy(lines[0].text.gameObject);
            lines.RemoveAt(0);
        }
    }

    void Update()
    {
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            var (text, born) = lines[i];
            float age = Time.unscaledTime - born;
            if (age >= Lifetime || text == null)
            {
                if (text != null) Destroy(text.gameObject);
                lines.RemoveAt(i);
                continue;
            }
            int fromBottom = lines.Count - 1 - i;
            text.rectTransform.anchoredPosition = new Vector2(-40f, 260f + fromBottom * 44f + Mathf.Min(age, 0.2f) * 50f);
            var c = text.color;
            c.a = 1f - Mathf.Clamp01((age - Lifetime * 0.6f) / (Lifetime * 0.4f));
            text.color = c;
        }
    }
}
