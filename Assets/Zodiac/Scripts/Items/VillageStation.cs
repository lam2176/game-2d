using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Một điểm trong Làng Trung Tâm (Lò rèn, Tiệm thuốc, Bàn thờ tổ, Cổng làng): người chơi đứng gần thì hiện chữ
/// "F: …" trên đầu (theo phím Tương tác đã đổi / nút tay cầm), bấm phím đó để mở. Bàn thờ mở thẳng trang Tu luyện của hành trang.
/// </summary>
public class VillageStation : MonoBehaviour
{
    public enum Kind { Forge, Shop, Altar, Gate, Tower }

    public Kind kind;
    [Min(0.5f)] public float radius = 2.4f;
    [Tooltip("Chữ gợi ý trên đầu (bật khi đứng gần).")]
    public GameObject prompt;

    InputAction interact;
    bool near;
    Text promptText;
    string promptTemplate;

    void Awake()
    {
        interact = InputSystem.actions.FindAction("Player/Interact");
        if (kind == Kind.Forge && GetComponent<ForgeAmbience>() == null) gameObject.AddComponent<ForgeAmbience>();
        if (prompt == null) return;
        prompt.SetActive(false);
        promptText = prompt.GetComponentInChildren<Text>(true);
        if (promptText == null) return;
        promptTemplate = promptText.text;
        if (promptTemplate.StartsWith("F:")) promptTemplate = "{Interact}" + promptTemplate.Substring(1);   // scene dựng trước khi có đổi phím
        RefreshPrompt();
    }

    void OnEnable()
    {
        interact?.Enable();
        ZodiacInput.Changed += RefreshPrompt;
    }

    void OnDisable() => ZodiacInput.Changed -= RefreshPrompt;

    void RefreshPrompt()
    {
        if (promptText != null) promptText.text = ZodiacInput.Format(promptTemplate);
    }

    void Update()
    {
        var player = ZodiacPlayerController.Current;
        if (player == null) return;
        Vector3 d = player.transform.position - transform.position;
        near = Mathf.Abs(d.x) <= radius && player.ControlEnabled;
        if (prompt != null && prompt.activeSelf != near) prompt.SetActive(near);
        if (!near || VillageWindow.IsOpen || InventoryWindow.IsOpen || PauseMenu.IsOpen || StoryUi.IsOpen) return;
        if (Time.frameCount == VillageWindow.ClosedFrame) return;   // F vừa đóng cửa sổ thì không mở lại ngay
        if (interact == null || !interact.WasPressedThisFrame()) return;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.lantern, 0.4f, 0.05f);
        switch (kind)
        {
            case Kind.Forge: VillageWindow.Open(VillageWindow.Mode.Forge); break;
            case Kind.Shop: VillageWindow.Open(VillageWindow.Mode.Shop); break;
            case Kind.Gate: VillageWindow.Open(VillageWindow.Mode.Gate); break;
            case Kind.Tower: VillageWindow.Open(VillageWindow.Mode.Tower); break;
            default: InventoryWindow.OpenTraining(); break;
        }
    }
}
