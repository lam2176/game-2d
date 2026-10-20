using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cảnh cắt khi tung Tuyệt kỹ (kiểu anime / Ngọc Rồng): dải đen chéo ngang màn hình, chân dung nhân vật trượt vào,
/// tên tuyệt kỹ chữ lớn, thời gian chậm lại trong chốc lát rồi trở về bình thường.
/// </summary>
public class UltimateCutIn : MonoBehaviour
{
    const float Duration = 0.75f;
    const float SlowScale = 0.12f;

    static UltimateCutIn host;
    RectTransform band, portraitRt;
    Image bandImage, portrait, flash;
    Text title, subtitle;
    Coroutine running;

    public static void Show(ZodiacCharacter character, Color theme)
    {
        if (!Application.isPlaying || character == null) return;
        if (host == null)
        {
            var go = new GameObject("UltimateCutIn");
            DontDestroyOnLoad(go);
            host = go.AddComponent<UltimateCutIn>();
        }
        if (host.running != null) host.StopCoroutine(host.running);
        host.running = host.StartCoroutine(host.Play(character, theme));
    }

    void Awake()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        flash = Make<Image>("Flash", transform);
        Stretch(flash.rectTransform);

        bandImage = Make<Image>("Band", transform);
        band = bandImage.rectTransform;
        band.anchorMin = new Vector2(-0.2f, 0.5f);
        band.anchorMax = new Vector2(1.2f, 0.5f);
        band.sizeDelta = new Vector2(0f, 300f);
        band.localRotation = Quaternion.Euler(0f, 0f, 8f);

        var mask = band.gameObject.AddComponent<RectMask2D>();
        mask.padding = Vector4.zero;
        portrait = Make<Image>("Portrait", band);
        portrait.preserveAspect = true;
        portraitRt = portrait.rectTransform;
        portraitRt.anchorMin = portraitRt.anchorMax = new Vector2(0.3f, 0.5f);
        portraitRt.sizeDelta = new Vector2(520f, 520f);
        portraitRt.localRotation = Quaternion.Euler(0f, 0f, -8f);

        title = MakeText("Title", band, 92, new Vector2(0.62f, 0.5f), new Vector2(0f, 30f));
        subtitle = MakeText("Subtitle", band, 34, new Vector2(0.62f, 0.5f), new Vector2(0f, -55f));
        SetVisible(false);
    }

    IEnumerator Play(ZodiacCharacter c, Color theme)
    {
        SetVisible(true);
        portrait.sprite = c.sprite;
        title.text = c.ultimateName;
        subtitle.text = $"Tuyệt kỹ của {c.displayName} — Tuổi {c.zodiacName}";
        bandImage.color = new Color(0.05f, 0.03f, 0.08f, 0.92f);
        title.color = Color.Lerp(theme, Color.white, 0.35f);

        Time.timeScale = SlowScale;
        for (float t = 0f; t < Duration; t += Time.unscaledDeltaTime)
        {
            float k = t / Duration;
            float inK = Mathf.Clamp01(k / 0.25f), outK = Mathf.Clamp01((k - 0.8f) / 0.2f);
            float ease = 1f - Mathf.Pow(1f - inK, 3f);
            band.localScale = new Vector3(1f, Mathf.Lerp(0f, 1f, ease) * (1f - outK), 1f);
            portraitRt.anchoredPosition = new Vector2(Mathf.Lerp(-700f, 0f, ease) + k * 60f, 0f);
            title.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(600f, 0f, ease) - k * 40f, 30f);
            flash.color = new Color(theme.r, theme.g, theme.b, 0.45f * (1f - Mathf.Clamp01(k / 0.15f)));
            yield return null;
        }
        Time.timeScale = 1f;
        SetVisible(false);
        running = null;
    }

    void SetVisible(bool v)
    {
        band.gameObject.SetActive(v);
        flash.gameObject.SetActive(v);
    }

    void OnDisable()
    {
        if (running != null) Time.timeScale = 1f;
    }

    static T Make<T>(string name, Transform parent) where T : Graphic
    {
        var g = new GameObject(name, typeof(RectTransform)).AddComponent<T>();
        g.transform.SetParent(parent, false);
        g.raycastTarget = false;
        return g;
    }

    static Text MakeText(string name, Transform parent, int size, Vector2 anchor, Vector2 position)
    {
        var t = Make<Text>(name, parent);
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.color = Color.white;
        t.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(1100f, 120f);
        rt.localRotation = Quaternion.Euler(0f, 0f, -8f);
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }
}
