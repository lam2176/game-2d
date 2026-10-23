using UnityEngine;

/// <summary>
/// Vũ khí nhặt trên đất (đặc trưng beat 'em up): đòn gánh, đèn lồng, chum vại nằm ven đường. Đứng gần hiện chữ
/// "F: nhặt …", bấm phím Tương tác để cầm (<see cref="PlayerWeapon"/>). Dùng vài lần thì vỡ.
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    public enum Kind { Pole, Lantern, Jar }

    public Kind kind;
    [Tooltip("Hình (được chuyển vào tay người chơi khi cầm).")]
    public Transform visual;
    public float radius = 1.3f;

    GameObject prompt;
    float bob;

    public static string NameOf(Kind k) => k switch
    {
        Kind.Pole => "Đòn gánh",
        Kind.Lantern => "Đèn lồng",
        _ => "Chum vại",
    };

    /// <summary>Số lần đánh trước khi vỡ (chum: ném một lần).</summary>
    public static int UsesOf(Kind k) => k switch
    {
        Kind.Pole => 6,
        Kind.Lantern => 5,
        _ => 1,
    };

    void Start() => bob = Random.Range(0f, 6f);

    void Update()
    {
        var player = ZodiacPlayerController.Current;
        if (player == null) return;
        Vector3 d = player.transform.position - transform.position;
        var weapon = player.GetComponent<PlayerWeapon>();
        var grab = player.GetComponent<PlayerGrab>();
        bool near = new Vector2(d.x, d.z).sqrMagnitude <= radius * radius && player.ControlEnabled && player.IsGrounded &&
                    (weapon == null || !weapon.IsHolding) && (grab == null || !grab.IsHolding);
        if (near && prompt == null)
            prompt = ZodiacUi.WorldPrompt(transform, ZodiacInput.Format("{Interact}: nhặt " + NameOf(kind).ToLower()), 1.6f);
        if (prompt != null && prompt.activeSelf != near) prompt.SetActive(near);
        if (visual != null) visual.localPosition = Vector3.up * (0.03f * Mathf.Sin(Time.time * 2f + bob));
        if (!near || InventoryWindow.IsOpen || PauseMenu.IsOpen || VillageWindow.IsOpen) return;
        var interact = ZodiacInput.Find("Interact");
        if (interact == null || !interact.WasPressedThisFrame() || Time.frameCount == PlayerWeapon.LastDropFrame) return;
        if (weapon == null) weapon = player.gameObject.AddComponent<PlayerWeapon>();
        weapon.Take(this);
    }

    /// <summary>Người chơi cầm lên: hình chuyển vào tay, object này tắt.</summary>
    public Transform Detach()
    {
        var v = visual;
        visual = null;
        if (prompt != null) Destroy(prompt);
        gameObject.SetActive(false);
        return v;
    }

    /// <summary>Thả xuống đất (bị quật ngã / bấm Tương tác lần nữa): hình trở về, nằm lại chỗ mới.</summary>
    public void Restore(Transform v, Vector3 at)
    {
        transform.position = new Vector3(at.x, 0f, at.z);
        visual = v;
        if (v != null)
        {
            v.SetParent(transform, false);
            v.localPosition = Vector3.zero;
            v.localRotation = Quaternion.identity;
        }
        gameObject.SetActive(true);
    }
}
