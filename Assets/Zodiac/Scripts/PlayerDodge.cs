using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Né lăn cho mọi con giáp — action Player/Dodge (L, Ctrl trái, LB tay cầm).
/// Lăn nhanh theo hướng phím di chuyển (không bấm thì theo hướng nhìn), bất tử suốt lúc lăn, đi xuyên quái và đạn.
/// Trên không được né 1 lần cho tới khi chạm đất (như cú lướt trên không của Dead Cells / Celeste).
/// Chuột vẫn giữ chiêu Độn Ảnh riêng — xa hơn, nhanh hơn lăn thường.
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
    /// <summary>Vừa bắt đầu lăn (hướng: -1/1).</summary>
    public event Action<float> Dodged;

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
        if (!controller.ControlEnabled || !IsReady || controller.IsOverridden || (skill != null && skill.IsBusy)) return false;
        if (direction == 0f)
        {
            float x = moveAction != null ? moveAction.ReadValue<Vector2>().x : 0f;
            direction = Mathf.Abs(x) > 0.3f ? Mathf.Sign(x) : controller.FacingDirection;
        }
        if (!controller.IsGrounded) airDodgeUsed = true;
        StartCoroutine(Roll(Mathf.Sign(direction)));
        return true;
    }

    IEnumerator Roll(float dir)
    {
        IsDodging = true;
        controller.SetFacing(dir);
        controller.SetOverride(true, 0f);
        var rb = controller.Body;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.swing, 0.5f, 0.15f);
        ZodiacParticles.Dust((Vector2)transform.position + Vector2.up * 0.05f, new Vector2(-dir, 0f), 6);
        Dodged?.Invoke(dir);

        float nextImage = 0f;
        for (float t = 0f; t < duration && controller.ControlEnabled; t += Time.fixedDeltaTime)
        {
            health?.SetInvulnerable(0.08f);
            // Giảm tốc ở cuối cú lăn cho mượt
            float k = 1f - Mathf.Pow(t / duration, 3f) * 0.6f;
            rb.linearVelocity = new Vector2(dir * speed * k, 0f);
            if (t >= nextImage)
            {
                var c = controller.Character != null ? controller.Character.themeColor : Color.white;
                ZodiacFx.Afterimage(controller.spriteRenderer, new Color(c.r, c.g, c.b, 0.35f));
                nextImage = t + 0.06f;
            }
            yield return new WaitForFixedUpdate();
        }
        controller.SetOverride(false);
        rb.linearVelocity = new Vector2(dir * speed * 0.3f, 0f);
        IsDodging = false;
        readyTime = Time.time + cooldown;
    }

    void OnDisable()
    {
        if (IsDodging && controller != null) controller.SetOverride(false);
        IsDodging = false;
    }
}
