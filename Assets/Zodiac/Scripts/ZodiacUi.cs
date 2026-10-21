using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Tiện ích dựng giao diện bằng code (canvas overlay, khung, chữ, nút, ô đồ) theo cùng một phong cách:
/// nền nâu sẫm viền vàng, nút đỏ, chữ màu kem — dùng cho các cửa sổ dựng lúc chạy (làng, lò rèn, tiệm).
/// </summary>
public static class ZodiacUi
{
    public static readonly Color Panel = new Color(0.11f, 0.08f, 0.06f, 0.96f);
    public static readonly Color SlotBack = new Color(0.22f, 0.17f, 0.13f, 0.95f);
    public static readonly Color Gold = new Color(0.95f, 0.78f, 0.35f);
    public static readonly Color Ink = new Color(0.95f, 0.9f, 0.8f);
    public static readonly Color Muted = new Color(0.8f, 0.72f, 0.6f);
    public static readonly Color ButtonRed = new Color(0.7f, 0.15f, 0.12f);

    static Font font;
    public static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    /// <summary>Canvas overlay 1920×1080 co giãn theo màn hình, có bắt chuột.</summary>
    public static Transform Canvas(Transform parent, string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go.transform;
    }

    /// <summary>Lớp tối phủ màn hình + khung giữa có tiêu đề. Trả về (lớp phủ, khung).</summary>
    public static (GameObject window, Transform panel) Window(Transform canvas, string title, Vector2 size)
    {
        var dim = MakeImage(canvas, "Window", new Color(0f, 0f, 0f, 0.55f));
        Stretch(dim.rectTransform);
        var panel = MakeImage(dim.transform, "Panel", Panel);
        Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        panel.gameObject.AddComponent<Outline>().effectColor = Gold;
        MakeText(panel.transform, title, 50, Gold, new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(size.x - 40f, 70f)).fontStyle = FontStyle.Bold;
        return (dim.gameObject, panel.transform);
    }

    public static Image MakeImage(Transform parent, string name, Color color)
    {
        var img = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = color;
        return img;
    }

    public static Text MakeText(Transform parent, string content, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 box,
                            TextAnchor align = TextAnchor.MiddleCenter)
    {
        var t = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = Font;
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.supportRichText = true;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
        Place(t.rectTransform, anchor, pos, box);
        return t;
    }

    public static Button MakeButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, UnityAction onClick)
    {
        var img = MakeImage(parent, "Button " + label, ButtonRed);
        Place(img.rectTransform, anchor, pos, size);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var colors = b.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
        colors.disabledColor = new Color(0.45f, 0.4f, 0.38f, 0.8f);
        b.colors = colors;
        b.onClick.AddListener(onClick);
        var t = MakeText(img.transform, label, Mathf.RoundToInt(Mathf.Min(28f, size.y * 0.45f)), Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Stretch(t.rectTransform);
        return b;
    }

    /// <summary>Ô đồ: viền (màu độ hiếm), nền, biểu tượng, số lượng / cấp ở góc. Bấm gọi onClick.</summary>
    public static (Image frame, Image icon, Text corner) MakeSlot(Transform parent, Vector2 anchor, Vector2 pos, float size, UnityAction onClick)
    {
        var frame = MakeImage(parent, "Slot", new Color(0.4f, 0.32f, 0.25f));
        Place(frame.rectTransform, anchor, pos, new Vector2(size, size));
        var back = MakeImage(frame.transform, "Back", SlotBack);
        Stretch(back.rectTransform);
        back.rectTransform.offsetMin = new Vector2(4f, 4f);
        back.rectTransform.offsetMax = new Vector2(-4f, -4f);
        back.raycastTarget = false;
        var icon = MakeImage(frame.transform, "Icon", Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Stretch(icon.rectTransform);
        icon.rectTransform.offsetMin = new Vector2(10f, 10f);
        icon.rectTransform.offsetMax = new Vector2(-10f, -10f);
        icon.enabled = false;
        var corner = MakeText(frame.transform, "", 20, Color.white, new Vector2(1f, 0f), new Vector2(-6f, 4f), new Vector2(60f, 26f), TextAnchor.LowerRight);
        corner.rectTransform.pivot = new Vector2(1f, 0f);
        if (onClick != null)
        {
            var b = frame.gameObject.AddComponent<Button>();
            b.targetGraphic = frame;
            b.onClick.AddListener(onClick);
        }
        return (frame, icon, corner);
    }

    /// <summary>Hiện một món lên ô đồ (null / rỗng = ô trống).</summary>
    public static void Fill((Image frame, Image icon, Text corner) slot, ItemStack s, bool selected)
    {
        var def = s?.Def;
        slot.icon.enabled = def != null && def.icon != null;
        if (slot.icon.enabled) slot.icon.sprite = def.icon;
        slot.corner.text = def == null ? "" : s.count > 1 ? s.count.ToString() : s.level > 0 ? $"+{s.level}" : "";
        Color border = def != null ? ItemDefinition.RarityColor(def.rarity) : new Color(0.4f, 0.32f, 0.25f);
        slot.frame.color = selected ? Color.white : border;
    }

    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }
}
