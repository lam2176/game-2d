using System;
using UnityEngine;

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
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsLit || ZodiacPlayerController.FromCollider(other) == null) return;
        SetLit(true);
        Reached?.Invoke(this);
    }

    void Update()
    {
        if (glow != null && IsLit)
            glow.transform.localScale = Vector3.one * (1.6f + Mathf.Sin(Time.time * 3f) * 0.15f);
    }
}
