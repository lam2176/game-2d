using UnityEngine;

/// <summary>
/// Lối vào / lối ra khu bí mật.
///   • Lối vào: nằm ẩn sau một vật đập vỡ được (<see cref="cover"/> — bụi tre rậm, vách đá nứt). Vật đó vỡ thì lối hiện ra
///     (cột sáng tím); dọn sạch khu đang đứng rồi đi vào thì sang khu bí mật (<see cref="ZodiacGameManager.EnterSecret"/>).
///   • Lối ra (<see cref="returns"/>): là "exit" của khu bí mật, bật khi dọn xong khu — đi vào thì về chỗ cũ.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SecretDoor : MonoBehaviour
{
    [Tooltip("Khu bí mật lối này dẫn tới (lối vào).")]
    public EncounterZone target;
    [Tooltip("Lối ra trong khu bí mật — đi vào thì quay về khu chính.")]
    public bool returns;
    [Tooltip("Vật che lối (Health) — vỡ thì lối mới hiện. Trống = hiện sẵn.")]
    public Health cover;
    [Tooltip("Cột sáng / dấu hiệu (nhấp nháy).")]
    public Transform beacon;

    bool used, revealed;
    float nextHint;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        revealed = cover == null;
        if (cover != null) cover.Died += Reveal;
        if (!revealed) SetVisible(false);
    }

    void OnEnable() => used = false;

    void Reveal()
    {
        revealed = true;
        SetVisible(true);
        ZodiacFx.Ring(transform.position + Vector3.up * 0.05f, 2.4f, new Color(0.75f, 0.5f, 1f, 0.8f), 0.7f, flat: true);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.lantern, 0.8f, 0f, transform.position);
        PickupToast.Show("Lộ ra một lối bí mật!", new Color(0.8f, 0.65f, 1f));
    }

    void SetVisible(bool on)
    {
        GetComponent<Collider>().enabled = on;
        if (beacon != null) beacon.gameObject.SetActive(on);
    }

    void Update()
    {
        if (beacon != null && beacon.gameObject.activeSelf)
            beacon.localScale = new Vector3(1f, 1f + 0.12f * Mathf.Sin(Time.time * 4f), 1f);
    }

    void OnTriggerEnter(Collider other) => TryUse(other);
    void OnTriggerStay(Collider other) => TryUse(other);   // đứng sẵn trong lối lúc dọn xong khu

    void TryUse(Collider other)
    {
        var gm = ZodiacGameManager.Instance;
        if (used || !revealed || gm == null || ZodiacPlayerController.FromCollider(other) == null) return;
        if (returns)
        {
            used = true;
            gm.LeaveSecret();
            return;
        }
        if (target == null) return;
        if (gm.Room != null && !gm.Room.IsCleared)
        {
            if (Time.time >= nextHint)
            {
                nextHint = Time.time + 3f;
                PickupToast.Show("Dọn sạch quái trong khu trước đã", new Color(0.8f, 0.65f, 1f));
            }
            return;
        }
        used = true;
        gm.EnterSecret(target, transform.position + Vector3.left * 1.2f, () => used = false);
    }
}
