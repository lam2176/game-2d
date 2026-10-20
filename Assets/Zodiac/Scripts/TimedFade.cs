using UnityEngine;
using UnityEngine.UI;

/// <summary>Chữ/ảnh UI hiện trong delay giây rồi mờ dần trong fadeTime giây và tự tắt (banner tên màn).</summary>
[RequireComponent(typeof(Graphic))]
public class TimedFade : MonoBehaviour
{
    [Min(0f)] public float delay = 2.5f;
    [Min(0.01f)] public float fadeTime = 1f;

    Graphic graphic;
    Color baseColor;
    float startTime;

    void Awake()
    {
        graphic = GetComponent<Graphic>();
        baseColor = graphic.color;
    }

    void OnEnable() => startTime = Time.time;

    void Update()
    {
        float t = (Time.time - startTime - delay) / fadeTime;
        if (t <= 0f) return;
        graphic.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - Mathf.Clamp01(t)));
        if (t >= 1f) gameObject.SetActive(false);
    }
}
