using UnityEngine;

/// <summary>
/// Viên chưởng bay thẳng, gây sát thương cho mục tiêu đầu tiên trúng phải.
/// Prefab cần: SpriteRenderer, Rigidbody2D (Kinematic), Collider2D (Is Trigger).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class KiBlast : MonoBehaviour
{
    public float speed = 14f;
    public float lifetime = 2f;

    int damage;
    CharacterStats owner;
    LayerMask targetLayer;

    public void Launch(Vector2 direction, int damage, CharacterStats owner, LayerMask targetLayer)
    {
        this.damage = damage;
        this.owner = owner;
        this.targetLayer = targetLayer;

        GetComponent<Rigidbody2D>().linearVelocity = direction.normalized * speed;
        if (direction.x < 0)
        {
            Vector3 s = transform.localScale;
            s.x = -Mathf.Abs(s.x);
            transform.localScale = s;
        }
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((targetLayer.value & (1 << other.gameObject.layer)) == 0) return;
        if (other.TryGetComponent(out CharacterStats target) && target != owner)
        {
            target.TakeDamage(damage, owner);
            Destroy(gameObject);
        }
    }
}
