using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Một điểm trong Làng Trung Tâm (Lò rèn, Tiệm thuốc, Bàn thờ tổ, Cổng làng): người chơi đứng gần thì hiện chữ
/// "F: …" trên đầu, bấm F (hoặc nút Y / North tay cầm) để mở. Bàn thờ mở thẳng trang Tu luyện của hành trang.
/// </summary>
public class VillageStation : MonoBehaviour
{
    public enum Kind { Forge, Shop, Altar, Gate }

    public Kind kind;
    [Min(0.5f)] public float radius = 2.4f;
    [Tooltip("Chữ gợi ý trên đầu (bật khi đứng gần).")]
    public GameObject prompt;

    InputAction interact;
    bool near;

    void Awake()
    {
        interact = InputSystem.actions.FindAction("Player/Interact");
        if (prompt != null) prompt.SetActive(false);
    }

    void OnEnable() => interact?.Enable();

    void Update()
    {
        var player = ZodiacPlayerController.Current;
        if (player == null) return;
        Vector3 d = player.transform.position - transform.position;
        near = Mathf.Abs(d.x) <= radius && player.ControlEnabled;
        if (prompt != null && prompt.activeSelf != near) prompt.SetActive(near);
        if (!near || VillageWindow.IsOpen || InventoryWindow.IsOpen || PauseMenu.IsOpen || StoryUi.IsOpen) return;
        if (Time.frameCount == VillageWindow.ClosedFrame) return;   // F vừa đóng cửa sổ thì không mở lại ngay
        var kb = Keyboard.current;
        bool pressed = (kb != null && kb.fKey.wasPressedThisFrame) || (interact != null && interact.WasPressedThisFrame());
        if (!pressed) return;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.lantern, 0.4f, 0.05f);
        switch (kind)
        {
            case Kind.Forge: VillageWindow.Open(VillageWindow.Mode.Forge); break;
            case Kind.Shop: VillageWindow.Open(VillageWindow.Mode.Shop); break;
            case Kind.Gate: VillageWindow.Open(VillageWindow.Mode.Gate); break;
            default: InventoryWindow.OpenTraining(); break;
        }
    }
}
