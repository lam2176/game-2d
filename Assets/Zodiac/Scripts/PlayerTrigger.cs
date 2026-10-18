using System;
using UnityEngine;

/// <summary>Vùng trigger báo khi người chơi chạm vào — dùng cho đích đến và vùng rơi xuống vực.</summary>
[RequireComponent(typeof(Collider2D))]
public class PlayerTrigger : MonoBehaviour
{
    public event Action<ZodiacPlayerController> Entered;

    void Awake() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = ZodiacPlayerController.FromCollider(other);
        if (player != null) Entered?.Invoke(player);
    }
}
