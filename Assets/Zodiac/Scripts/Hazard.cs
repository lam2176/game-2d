using UnityEngine;

/// <summary>
/// Vùng nguy hiểm trên mặt đường (bãi chông, than hồng): người chơi đứng/đi vào bị trừ máu và đẩy bật ra.
/// Nhảy qua thì không sao — chỉ tính khi chân còn sát mặt đất.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Hazard : MonoBehaviour
{
    [Min(1)] public int damage = ZodiacBalance.Unit;

    [Tooltip("Chân cao hơn mặt bẫy từng này (unit) thì coi như nhảy qua.")]
    public float clearance = 0.35f;

    void Awake() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerEnter(Collider other) => Touch(other);
    void OnTriggerStay(Collider other) => Touch(other);

    void Touch(Collider other)
    {
        var player = ZodiacPlayerController.FromCollider(other);
        if (player == null || !player.ControlEnabled) return;
        if (player.transform.position.y > transform.position.y + clearance) return;
        var manager = ZodiacGameManager.Instance;
        if (manager != null) manager.HazardHit(damage, transform.position);
    }
}
