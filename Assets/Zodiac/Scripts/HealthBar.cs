using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh máu: co giãn trục X của <see cref="fill"/> theo máu còn lại.
/// Dùng chung cho HUD (fill là RectTransform pivot trái) và thanh trên đầu quái (fill là Transform đặt ở mép trái).
/// </summary>
public class HealthBar : MonoBehaviour
{
    public Health health;
    [Tooltip("Transform được co giãn trục X — gốc/pivot phải nằm ở mép trái thanh.")]
    public Transform fill;
    [Tooltip("Chữ hiển thị số máu (không bắt buộc).")]
    public Text label;
    [Tooltip("Ẩn visualRoot khi máu đầy hoặc khi đã chết (dùng cho quái).")]
    public bool hideWhenFull;
    public GameObject visualRoot;

    void Start()
    {
        health.Changed += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (health != null) health.Changed -= Refresh;
    }

    void Refresh()
    {
        fill.localScale = new Vector3(health.Normalized, 1f, 1f);
        if (label != null) label.text = $"Máu {health.Current}/{health.maxHealth}";
        if (visualRoot != null)
            visualRoot.SetActive(!health.IsDead && !(hideWhenFull && health.Current == health.maxHealth));
    }
}
