using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

/// <summary>
/// Cần ảo nổi: đặt ngón ở đâu trong vùng trái màn hình thì đế cần hiện ở đó, kéo để đi (giả lập cần trái tay cầm).
/// Đẩy gần hết cỡ thì tự giữ nút Chạy (<see cref="sprint"/>) — vành sáng vàng. Thả tay: đế mờ dần về chỗ nghỉ.
/// </summary>
public class TouchStick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [InputControl(layout = "Vector2")]
    [SerializeField] string path = "<Gamepad>/leftStick";

    public RectTransform stickBase;
    public RectTransform knob;
    public Image ring;
    public TouchButton sprint;
    public float range = 120f;
    [Range(0f, 1f)] public float sprintThreshold = 0.92f;

    RectTransform zone;
    Vector2 restPosition;
    Vector2 origin;
    bool dragging;
    int pointerId = int.MinValue;
    CanvasGroup group;

    static readonly Color RingIdle = new Color(1f, 1f, 1f, 0.55f);
    static readonly Color RingSprint = new Color(1f, 0.8f, 0.3f, 1f);

    protected override string controlPathInternal
    {
        get => path;
        set => path = value;
    }

    void Awake()
    {
        zone = (RectTransform)transform;
        restPosition = stickBase.anchoredPosition;
        group = stickBase.GetComponent<CanvasGroup>();
        if (group == null) group = stickBase.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (dragging) return;
        dragging = true;
        pointerId = e.pointerId;
        origin = Local(e);
        stickBase.anchoredPosition = origin;
        Move(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (dragging && e.pointerId == pointerId) Move(e);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId == pointerId) Release();
    }

    void Move(PointerEventData e)
    {
        var delta = Vector2.ClampMagnitude(Local(e) - origin, range);
        knob.anchoredPosition = delta;
        var value = delta / range;
        SendValueToControl(value);
        bool run = value.magnitude >= sprintThreshold;
        if (sprint != null) sprint.SetPressed(run);
        if (ring != null) ring.color = run ? RingSprint : RingIdle;
    }

    void Release()
    {
        dragging = false;
        pointerId = int.MinValue;
        knob.anchoredPosition = Vector2.zero;
        SendValueToControl(Vector2.zero);
        if (sprint != null) sprint.SetPressed(false);
        if (ring != null) ring.color = RingIdle;
    }

    /// <summary>Toạ độ chạm trong vùng cần (cùng gốc với đế cần), giữ đế nằm trọn trong vùng.</summary>
    Vector2 Local(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, e.pressEventCamera, out var p);
        var r = zone.rect;
        var half = stickBase.rect.size * 0.5f;
        p.x = Mathf.Clamp(p.x, r.xMin + half.x * 0.6f, r.xMax - half.x * 0.6f);
        p.y = Mathf.Clamp(p.y, r.yMin + half.y * 0.6f, r.yMax - half.y * 0.6f);
        return p;
    }

    protected override void OnDisable()
    {
        if (dragging) Release();
        base.OnDisable();
    }

    void Update()
    {
        if (!dragging)
            stickBase.anchoredPosition = Vector2.Lerp(stickBase.anchoredPosition, restPosition, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f));
        group.alpha = Mathf.MoveTowards(group.alpha, dragging ? 1f : 0.55f, Time.unscaledDeltaTime * 4f);
    }
}
