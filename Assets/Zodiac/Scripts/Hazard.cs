using UnityEngine;

/// <summary>
/// Vùng nguy hiểm (gai): người chơi chạm vào bị trừ máu rồi được đưa về đèn lồng gần nhất
/// (giống gai trong Hollow Knight / Celeste) — không bị đẩy văng lung tung giữa bãi gai.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Hazard : MonoBehaviour
{
    [Min(1)] public int damage = ZodiacBalance.Unit;

    void Awake() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other) => Touch(other);
    void OnTriggerStay2D(Collider2D other) => Touch(other);

    void Touch(Collider2D other)
    {
        var player = ZodiacPlayerController.FromCollider(other);
        if (player == null || !player.ControlEnabled) return;
        var manager = ZodiacGameManager.Instance;
        if (manager != null) manager.HazardHit(damage, transform.position);
    }
}
