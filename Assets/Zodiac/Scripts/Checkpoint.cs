using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Đèn lồng checkpoint: người chơi chạm vào thì đèn sáng, trở thành điểm hồi sinh khi rơi vực / dính gai
/// và khi bấm "Hồi sinh" sau lúc thua. Chạm đèn mới cũng hồi 1 máu (như ghế nghỉ của Hollow Knight, nhẹ hơn).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Thứ tự trong màn (0 = điểm xuất phát).")]
    public int index;
    public SpriteRenderer lamp;
    public Color offColor = new Color(0.35f, 0.15f, 0.12f);
    public Color onColor = new Color(1f, 0.35f, 0.15f);
    public GameObject glow;
    [Tooltip("Đèn 2D của đèn lồng — mờ khi chưa thắp, sáng khi đã thắp.")]
    public Light2D lampLight;
    public float offIntensity = 0.35f;
    public float onIntensity = 1.4f;

    public bool IsLit { get; private set; }
    public Vector2 SpawnPosition => transform.position;

    public event Action<Checkpoint> Reached;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        SetLit(false);
    }

    public void SetLit(bool value)
    {
        IsLit = value;
        if (lamp != null) lamp.color = value ? onColor : offColor;
        if (glow != null) glow.SetActive(value);
        if (lampLight != null) lampLight.intensity = value ? onIntensity : offIntensity;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsLit || ZodiacPlayerController.FromCollider(other) == null) return;
        SetLit(true);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.lantern, 0.8f, 0f);
        ZodiacParticles.Sparkle(lamp != null ? lamp.transform.position : transform.position, onColor, 16, 0.5f);
        Reached?.Invoke(this);
    }

    void Update()
    {
        if (glow != null && IsLit)
            glow.transform.localScale = Vector3.one * (1.6f + Mathf.Sin(Time.time * 3f) * 0.15f);
        if (lampLight != null && IsLit)   // lửa nến chập chờn
            lampLight.intensity = onIntensity * (0.9f + 0.1f * Mathf.PerlinNoise(Time.time * 4f, index));
    }
}
