using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

/// <summary>
/// Nút cảm ứng giả lập một nút tay cầm (đường dẫn gán lúc dựng). Bấm thì lún xuống và sáng quầng;
/// <see cref="SetPressed"/> cho phép code khác giữ nút (cần ảo đẩy hết cỡ = giữ Chạy).
/// </summary>
public class TouchButton : OnScreenControl, IPointerDownHandler, IPointerUpHandler
{
    [InputControl(layout = "Button")]
    [SerializeField] string path;

    public Image glow;
    public float pressedScale = 0.88f;

    bool pointerDown, held;
    float scale = 1f;

    protected override string controlPathInternal
    {
        get => path;
        set => path = value;
    }

    public void OnPointerDown(PointerEventData e)
    {
        pointerDown = true;
        Send();
    }

    public void OnPointerUp(PointerEventData e)
    {
        pointerDown = false;
        Send();
    }

    /// <summary>Giữ / nhả nút từ code (không phụ thuộc ngón tay).</summary>
    public void SetPressed(bool value)
    {
        if (held == value) return;
        held = value;
        Send();
    }

    public bool Pressed => pointerDown || held;

    void Send() => SendValueToControl(Pressed ? 1f : 0f);

    protected override void OnDisable()
    {
        pointerDown = held = false;
        scale = 1f;
        transform.localScale = Vector3.one;
        if (glow != null) glow.color = new Color(1f, 1f, 1f, 0f);
        base.OnDisable();   // nhả nút giả lập
    }

    void Update()
    {
        scale = Mathf.MoveTowards(scale, pointerDown ? pressedScale : 1f, Time.unscaledDeltaTime * 3f);
        transform.localScale = Vector3.one * scale;
        if (glow != null) glow.color = new Color(1f, 0.85f, 0.45f, Mathf.MoveTowards(glow.color.a, Pressed ? 0.75f : 0f, Time.unscaledDeltaTime * 6f));
    }
}
