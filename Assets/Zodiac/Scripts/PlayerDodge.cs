using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Né lăn cho mọi con giáp — action Player/Dodge (L, Ctrl trái, LB tay cầm).
/// Lăn nhanh theo hướng phím di chuyển — cả đổi làn chéo / vào sâu (không bấm thì theo hướng nhìn),
/// bất tử suốt lúc lăn, đi xuyên quái và đạn. Trên không được né 1 lần cho tới khi chạm đất.
/// </summary>
[RequireComponent(typeof(ZodiacPlayerController))]
public class PlayerDodge : MonoBehaviour
{
    [Min(0f)] public float speed = 15f;
    [Min(0.05f)] public float duration = 0.26f;
    [Tooltip("Từ lúc kết thúc lăn tới khi lăn được lần nữa (giây).")]
    [Min(0f)] public float cooldown = 0.45f;

    ZodiacPlayerController controller;
    PlayerSkill skill;
    Health health;
    InputAction dodgeAction, moveAction;
    float readyTime;
    bool airDodgeUsed;

    public bool IsDodging { get; private set; }
    public bool IsReady => !IsDodging && Time.time >= readyTime && (controller.IsGrounded || !airDodgeUsed);
    /// <summary>0 = lăn được, 1 = vừa lăn (cho ô W trên HUD).</summary>
    public float CooldownNormalized => IsDodging ? 1f : Mathf.Clamp01((readyTime - Time.time) / Mathf.Max(0.01f, cooldown));
    /// <summary>Vừa bắt đầu lăn (hướng ngang: -1/1).</summary>
    public event Action<float> Dodged;
    /// <summary>Hướng lăn hiện tại trên mặt phẳng XZ.</summary>
    public Vector3 Direction { get; private set; } = Vector3.right;

    void Awake()
    {
        controller = GetComponent<ZodiacPlayerController>();
        skill = GetComponent<PlayerSkill>();
        health = GetComponent<Health>();
        dodgeAction = InputSystem.actions.FindAction("Player/Dodge");
        moveAction = InputSystem.actions.FindAction("Player/Move");
        if (dodgeAction == null) Debug.LogWarning("Thiếu action Player/Dodge — không lăn né được.", this);
    }

    void OnEnable() => dodgeAction?.Enable();

    void Update()
    {
        if (controller.IsGrounded) airDodgeUsed = false;
        if (dodgeAction != null && dodgeAction.WasPressedThisFrame()) TryDodge();
    }

    /// <summary>Lăn nếu được. direction = 0 → theo phím di chuyển / hướng nhìn.</summary>
    public bool TryDodge(float direction = 0f)
    {
        if (!controller.ControlEnabled || !IsReady || controller.IsOverridden || controller.IsKnockedDown ||
            (skill != null && skill.IsBusy)) return false;
        Vector3 dir;
        if (direction != 0f) dir = new Vector3(Mathf.Sign(direction), 0f, 0f);
        else
        {
            Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            dir = input.sqrMagnitude > 0.09f ? new Vector3(input.x, 0f, input.y).normalized : new Vector3(controller.FacingDirection, 0f, 0f);
        }
        if (!controller.IsGrounded) airDodgeUsed = true;
        StartCoroutine(Roll(dir));
        return true;
    }

    IEnumerator Roll(Vector3 dir)
    {
        IsDodging = true;
        Direction = dir;
        if (Mathf.Abs(dir.x) > 0.1f) controller.SetFacing(dir.x);
        controller.SetOverride(true, 0f);
        var rb = controller.Body;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.swing, 0.5f, 0.15f);
        ZodiacParticles.Dust(transform.position + Vector3.up * 0.05f, -dir, 6);
        Dodged?.Invoke(Mathf.Abs(dir.x) > 0.1f ? Mathf.Sign(dir.x) : controller.FacingDirection);

        float nextImage = 0f;
        for (float t = 0f; t < duration && controller.ControlEnabled; t += Time.fixedDeltaTime)
        {
            health?.SetInvulnerable(0.08f);
            // Giảm tốc ở cuối cú lăn cho mượt
            float k = 1f - Mathf.Pow(t / duration, 3f) * 0.6f;
            // Lăn trên không không bay lên tiếp (giữ tốc độ rơi, bỏ phần đang bay lên)
            rb.linearVelocity = new Vector3(dir.x * speed * k, Mathf.Min(rb.linearVelocity.y, 0f), dir.z * speed * k * 0.8f);
            if (t >= nextImage)
            {
                var c = controller.Character != null ? controller.Character.themeColor : Color.white;
                ZodiacFx.Afterimage(controller.spriteRenderer, new Color(c.r, c.g, c.b, 0.35f));
                nextImage = t + 0.06f;
            }
            yield return new WaitForFixedUpdate();
        }
        controller.SetOverride(false);
        if (!rb.isKinematic) rb.linearVelocity = new Vector3(dir.x * speed * 0.3f, rb.linearVelocity.y, dir.z * speed * 0.2f);
        IsDodging = false;
        readyTime = Time.time + cooldown;
    }

    void OnDisable()
    {
        if (IsDodging && controller != null) controller.SetOverride(false);
        IsDodging = false;
    }
}
