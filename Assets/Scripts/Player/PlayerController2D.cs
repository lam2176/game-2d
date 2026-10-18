using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Điều khiển nhân vật 2D: chạy, nhảy, bay (nhấn Jump khi đang trên không), đánh thường, bắn chưởng.
/// Dùng project-wide actions trong Assets/InputSystem_Actions.inputactions.
/// Phím mặc định: WASD/mũi tên di chuyển, Space nhảy/bay, chuột trái đánh, E bắn chưởng, Shift chạy nhanh.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Di chuyển")]
    public float moveSpeed = 6f;
    public float sprintMultiplier = 1.6f;
    public float jumpForce = 12f;
    public float flySpeed = 7f;
    public float flyKiPerSecond = 3f;

    [Header("Kiểm tra chạm đất")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Đánh thường")]
    public float meleeRange = 1f;
    public float meleeCooldown = 0.35f;
    public LayerMask enemyLayer;

    [Header("Chưởng")]
    public KiBlast kiBlastPrefab;
    public Transform firePoint;
    public float kiBlastCost = 15f;
    public float kiBlastCooldown = 0.6f;
    public float kiBlastDamageMultiplier = 2f;

    Rigidbody2D rb;
    CharacterStats stats;
    InputAction moveAction, jumpAction, attackAction, blastAction, sprintAction;

    bool isFlying;
    bool facingRight = true;
    float nextMeleeTime, nextBlastTime;
    float defaultGravity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<CharacterStats>();
        defaultGravity = rb.gravityScale;

        moveAction = InputSystem.actions.FindAction("Player/Move");
        jumpAction = InputSystem.actions.FindAction("Player/Jump");
        attackAction = InputSystem.actions.FindAction("Player/Attack");
        blastAction = InputSystem.actions.FindAction("Player/Interact");
        sprintAction = InputSystem.actions.FindAction("Player/Sprint");
    }

    public bool IsFlying => isFlying;
    public float LastAttackTime { get; private set; } = -10f;

    public bool IsGrounded =>
        groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

    void Update()
    {
        if (stats.IsDead) return;

        if (jumpAction.WasPressedThisFrame())
        {
            if (isFlying) SetFlying(false);
            else if (IsGrounded) rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            else SetFlying(true);
        }

        if (isFlying && (IsGrounded || !stats.TrySpendKi(flyKiPerSecond * Time.deltaTime)))
            SetFlying(false);

        if (attackAction.WasPressedThisFrame() && Time.time >= nextMeleeTime) Melee();
        if (blastAction.WasPressedThisFrame() && Time.time >= nextBlastTime) FireKiBlast();
    }

    void FixedUpdate()
    {
        if (stats.IsDead) { rb.linearVelocity = Vector2.zero; return; }

        Vector2 input = moveAction.ReadValue<Vector2>();
        float speed = moveSpeed * (sprintAction.IsPressed() ? sprintMultiplier : 1f);

        if (isFlying)
            rb.linearVelocity = input * flySpeed;
        else
            rb.linearVelocity = new Vector2(input.x * speed, rb.linearVelocity.y);

        if (input.x > 0.01f && !facingRight) Flip();
        else if (input.x < -0.01f && facingRight) Flip();
    }

    void SetFlying(bool value)
    {
        isFlying = value;
        rb.gravityScale = value ? 0f : defaultGravity;
        if (value) rb.linearVelocity = Vector2.zero;
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }

    void Melee()
    {
        nextMeleeTime = Time.time + meleeCooldown;
        LastAttackTime = Time.time;
        Vector2 center = (Vector2)transform.position + (facingRight ? Vector2.right : Vector2.left) * meleeRange * 0.5f;
        foreach (var hit in Physics2D.OverlapCircleAll(center, meleeRange * 0.5f, enemyLayer))
        {
            if (hit.TryGetComponent(out CharacterStats target) && target != stats)
                target.TakeDamage(stats.damage, stats);
        }
    }

    void FireKiBlast()
    {
        if (kiBlastPrefab == null || !stats.TrySpendKi(kiBlastCost)) return;
        nextBlastTime = Time.time + kiBlastCooldown;
        LastAttackTime = Time.time;

        Transform origin = firePoint != null ? firePoint : transform;
        KiBlast blast = Instantiate(kiBlastPrefab, origin.position, Quaternion.identity);
        blast.Launch(facingRight ? Vector2.right : Vector2.left,
                     Mathf.RoundToInt(stats.damage * kiBlastDamageMultiplier), stats, enemyLayer);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 dir = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Gizmos.DrawWireSphere((Vector2)transform.position + dir * meleeRange * 0.5f, meleeRange * 0.5f);
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
