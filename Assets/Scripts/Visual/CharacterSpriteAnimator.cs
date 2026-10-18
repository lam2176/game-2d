using UnityEngine;

/// <summary>
/// Đổi sprite theo trạng thái (đứng, chạy, nhảy, bay, đánh), nhún nhẹ khi đứng, nghiêng khi bay, nháy đỏ khi trúng đòn.
/// Đặt trên object con "Visual" có SpriteRenderer; Rigidbody2D/CharacterStats/PlayerController2D nằm ở object cha.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CharacterSpriteAnimator : MonoBehaviour
{
    public Sprite[] idle;
    public Sprite[] run;
    public Sprite[] jump;
    public Sprite[] fly;
    public Sprite[] attack;

    public float fps = 8f;
    public float attackShowTime = 0.2f;
    public float bobAmount = 0.04f;
    public float flyTilt = 15f;
    [Tooltip("Bật cho quái kiểu slime: co giãn thay vì đổi frame")]
    public bool squash;

    SpriteRenderer sr;
    Rigidbody2D rb;
    PlayerController2D player;
    CharacterStats stats;
    Vector3 basePos;
    float flashUntil;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponentInParent<Rigidbody2D>();
        player = GetComponentInParent<PlayerController2D>();
        stats = GetComponentInParent<CharacterStats>();
        basePos = transform.localPosition;
        if (stats != null) stats.OnDamaged += () => flashUntil = Time.time + 0.12f;
    }

    void Update()
    {
        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
        bool moving = speed > 0.1f;
        Sprite[] clip = idle;
        float tilt = 0f;

        if (player != null)
        {
            if (Time.time - player.LastAttackTime < attackShowTime && Has(attack)) clip = attack;
            else if (player.IsFlying) { clip = Has(fly) ? fly : idle; tilt = moving ? -flyTilt : 0f; }
            else if (!player.IsGrounded) clip = Has(jump) ? jump : idle;
            else if (moving && Has(run)) clip = run;
        }

        if (Has(clip)) sr.sprite = clip[(int)(Time.time * fps) % clip.Length];

        float wave = Mathf.Sin(Time.time * Mathf.PI * (moving ? 3f : 1.5f));
        transform.localPosition = basePos + Vector3.up * wave * bobAmount;
        transform.localRotation = Quaternion.Euler(0, 0, tilt);
        if (squash)
        {
            float d = wave * (moving ? 0.1f : 0.04f);
            transform.localScale = new Vector3(1f + d, 1f - d, 1f);
        }

        if (stats != null && stats.IsDead) sr.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        else sr.color = Time.time < flashUntil ? new Color(1f, 0.4f, 0.4f) : Color.white;
    }

    static bool Has(Sprite[] s) => s != null && s.Length > 0;
}
