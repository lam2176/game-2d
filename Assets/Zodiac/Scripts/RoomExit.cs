using UnityEngine;

/// <summary>
/// Lối ra ở mép phải khu đã dọn: cột sáng nhấp nháy, người chơi đi vào thì chuyển sang khu tiếp theo
/// (màn hình tối lại, hiện "Đang tới khu tiếp theo…").
/// </summary>
[RequireComponent(typeof(Collider))]
public class RoomExit : MonoBehaviour
{
    [Tooltip("Cột sáng / mũi tên chỉ lối ra (nhấp nháy).")]
    public Transform beacon;

    bool used;

    void Awake() => GetComponent<Collider>().isTrigger = true;

    void OnEnable() => used = false;

    void Update()
    {
        if (beacon != null) beacon.localScale = new Vector3(1f, 1f + 0.12f * Mathf.Sin(Time.time * 5f), 1f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (used || ZodiacPlayerController.FromCollider(other) == null || ZodiacGameManager.Instance == null) return;
        used = true;
        ZodiacGameManager.Instance.NextRoom();
    }
}
