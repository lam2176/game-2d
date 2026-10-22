using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Biển gỗ hướng dẫn: hiện khung chữ (Canvas world-space trên đầu biển) khi người chơi đứng gần.
/// Chữ có thể chứa {Attack}, {Jump}… — thay bằng phím đang dùng (<see cref="ZodiacInput.Format"/>).
/// </summary>
public class HintSign : MonoBehaviour
{
    [TextArea] public string message;
    [Min(0f)] public float showDistance = 3.5f;
    public GameObject bubble;
    public Text text;

    void OnEnable() => ZodiacInput.Changed += RefreshText;
    void OnDisable() => ZodiacInput.Changed -= RefreshText;

    void Start()
    {
        RefreshText();
        if (bubble != null) bubble.SetActive(false);
    }

    void RefreshText()
    {
        if (text != null) text.text = ZodiacInput.Format(message);
    }

    void Update()
    {
        var player = ZodiacPlayerController.Current;
        if (player == null || bubble == null) return;
        Vector3 d = player.transform.position - transform.position;
        bool show = Mathf.Abs(d.x) <= showDistance;
        if (bubble.activeSelf != show) bubble.SetActive(show);
    }
}
