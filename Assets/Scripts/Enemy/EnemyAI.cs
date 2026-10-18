using UnityEngine;

/// <summary>
/// Quái đơn giản: tuần tra quanh điểm xuất hiện, thấy người chơi thì đuổi và đánh, chết thì hồi sinh sau một lúc.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats))]
public class EnemyAI : MonoBehaviour
{
    public float patrolDistance = 3f;
    public float moveSpeed = 2.5f;
    public float detectRange = 5f;
    public float attackRange = 1f;
    public float attackCooldown = 1.2f;
    public float respawnDelay = 5f;
    public LayerMask playerLayer;

    Rigidbody2D rb;
    CharacterStats stats;
    Vector2 spawnPos;
    int patrolDir = 1;
    float nextAttackTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<CharacterStats>();
        spawnPos = transform.position;
        stats.OnDied += HandleDied;
    }

    void FixedUpdate()
    {
        if (stats.IsDead) return;

        Collider2D player = Physics2D.OverlapCircle(transform.position, detectRange, playerLayer);
        if (player != null && player.TryGetComponent(out CharacterStats target) && !target.IsDead)
        {
            float dx = player.transform.position.x - transform.position.x;
            if (Mathf.Abs(dx) > attackRange)
            {
                Move(Mathf.Sign(dx));
            }
            else
            {
                Move(0);
                if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + attackCooldown;
                    target.TakeDamage(stats.damage, stats);
                }
            }
            return;
        }

        float offset = transform.position.x - spawnPos.x;
        if (offset > patrolDistance) patrolDir = -1;
        else if (offset < -patrolDistance) patrolDir = 1;
        Move(patrolDir);
    }

    void Move(float dir)
    {
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        if (dir != 0)
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * Mathf.Sign(dir);
            transform.localScale = s;
        }
    }

    void HandleDied(CharacterStats self, CharacterStats killer)
    {
        rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);
        EnemyRespawner.Schedule(this, respawnDelay);
    }

    public void Respawn()
    {
        transform.position = spawnPos;
        gameObject.SetActive(true);
        stats.Revive();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
