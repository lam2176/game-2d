using UnityEngine;
using UnityEngine.UI;

/// <summary>Chữ "ĐI TIẾP ➜" nhấp nháy ở mép phải HUD sau khi dọn xong một vùng giao tranh (kiểu "GO!" của beat 'em up).</summary>
[RequireComponent(typeof(Graphic))]
public class GoIndicator : MonoBehaviour
{
    [Min(0.1f)] public float duration = 3f;

    static GoIndicator instance;
    Graphic graphic;
    float showUntil;

    void Awake()
    {
        instance = this;
        graphic = GetComponent<Graphic>();
        graphic.enabled = false;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Show()
    {
        if (instance == null) return;
        instance.showUntil = Time.time + instance.duration;
        instance.graphic.enabled = true;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.lantern, 0.6f, 0f);
    }

    void Update()
    {
        if (!graphic.enabled) return;
        if (Time.time >= showUntil) { graphic.enabled = false; return; }
        var c = graphic.color;
        c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 5f));
        graphic.color = c;
        transform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.time * 10f));
    }
}
