using System;
using UnityEngine;

/// <summary>Vật phẩm thu thập (tạm là đồng xu placeholder). Nhấp nhô tại chỗ, người chơi chạm vào thì biến mất.</summary>
[RequireComponent(typeof(Collider2D))]
public class Collectible : MonoBehaviour
{
    public float bobHeight = 0.15f;
    public float bobSpeed = 2.5f;

    public event Action<Collectible> Collected;

    Vector3 restPosition;
    float phase;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        restPosition = transform.localPosition;
        phase = transform.position.x;
    }

    void Update() =>
        transform.localPosition = restPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight);

    void OnTriggerEnter2D(Collider2D other)
    {
        if (ZodiacPlayerController.FromCollider(other) == null) return;
        gameObject.SetActive(false);
        Collected?.Invoke(this);
    }
}
