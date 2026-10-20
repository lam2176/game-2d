using UnityEngine;
using UnityEngine.UI;

/// <summary>Biển gỗ hướng dẫn: hiện khung chữ (Canvas world-space trên đầu biển) khi người chơi đứng gần.</summary>
public class HintSign : MonoBehaviour
{
    [TextArea] public string message;
    [Min(0f)] public float showDistance = 3.5f;
    public GameObject bubble;
    public Text text;

    void Start()
    {
        if (text != null) text.text = message;
        if (bubble != null) bubble.SetActive(false);
    }

    void Update()
    {
        var player = ZodiacPlayerController.Current;
        if (player == null || bubble == null) return;
        Vector2 d = player.transform.position - transform.position;
        bool show = Mathf.Abs(d.x) <= showDistance && Mathf.Abs(d.y) <= 3f;
        if (bubble.activeSelf != show) bubble.SetActive(show);
    }
}
