using System;
using UnityEngine;

/// <summary>Vùng trigger báo khi người chơi chạm vào — dùng cho cổng đích.</summary>
[RequireComponent(typeof(Collider))]
public class PlayerTrigger : MonoBehaviour
{
    public event Action<ZodiacPlayerController> Entered;

    void Awake() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        var player = ZodiacPlayerController.FromCollider(other);
        if (player != null) Entered?.Invoke(player);
    }
}
