using UnityEngine;

/// <summary>
/// Đồ cầm tay quái đánh rơi (khiên vỡ, giáo văng): bay lên lộn vòng, rơi xuống nằm trên mặt đất, một lúc sau mờ dần rồi biến mất.
/// </summary>
public class GearDebris : MonoBehaviour
{
    public float lifetime = 3f;
    public float fadeTime = 0.8f;

    Vector3 velocity;
    float spin, age;
    bool resting;
    SpriteRenderer[] renderers;
    Color[] colors;

    /// <summary>Tách đồ khỏi quái và cho văng đi theo hướng away (trên mặt đường).</summary>
    public static void Launch(Transform gear, Vector3 away)
    {
        gear.SetParent(null, true);
        var d = gear.gameObject.AddComponent<GearDebris>();
        away.y = 0f;
        d.velocity = away.normalized * Random.Range(2.5f, 4f) + Vector3.up * Random.Range(5f, 7f);
        d.spin = Random.Range(360f, 720f) * (away.x >= 0f ? -1f : 1f);
    }

    void Start()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
        colors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) colors[i] = renderers[i].color;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        if (!resting)
        {
            velocity.y -= 25f * dt;
            transform.position += velocity * dt;
            transform.Rotate(0f, 0f, spin * dt, Space.Self);
            if (transform.position.y <= 0.1f && velocity.y < 0f)
            {
                resting = true;
                transform.position = new Vector3(transform.position.x, 0.1f, transform.position.z);
                ZodiacParticles.Dust(transform.position, Vector3.zero, 4);
            }
        }
        float a = 1f - Mathf.Clamp01((age - (lifetime - fadeTime)) / fadeTime);
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = new Color(colors[i].r, colors[i].g, colors[i].b, colors[i].a * a);
        if (age >= lifetime) Destroy(gameObject);
    }
}
