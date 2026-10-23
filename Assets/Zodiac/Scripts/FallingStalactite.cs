using UnityEngine;

/// <summary>
/// Nhũ đá rơi (Hang Đá Vôi): khi khu đang đánh, cứ vài giây lại chọn chỗ người chơi đang đứng — vòng bóng đỏ hiện dưới đất,
/// nhũ đá mọc ra trên trần và rung lên, rồi rơi thẳng xuống. Trúng ai (người chơi hay quái) cũng mất máu; vỡ thành mảnh,
/// một lúc sau mọc lại. Né: đổi làn / lăn ra khỏi vòng — còn có thể dụ quái đứng vào vòng.
/// </summary>
public class FallingStalactite : MonoBehaviour
{
    public float xMin, xMax;
    [Tooltip("Hình nhũ đá (mũi nhọn quay xuống), tâm ở chân mũi.")]
    public Transform rock;
    public Vector2 interval = new Vector2(3.5f, 6.5f);
    public float warning = 1.0f;
    public float dropHeight = 7f;
    public float radius = 0.9f;
    [Min(1)] public int damage = ZodiacBalance.Unit;
    public Color debrisColor = new Color(0.8f, 0.75f, 0.65f);

    enum State { Waiting, Warning, Falling }

    State state;
    float next, stateStart, velocity;
    Vector3 target;

    void Start()
    {
        next = Time.time + Random.Range(interval.x, interval.y);
        if (rock != null) rock.gameObject.SetActive(false);
    }

    bool RoomActive
    {
        get
        {
            var gm = ZodiacGameManager.Instance;
            var room = gm != null ? gm.Room : null;
            return room != null && room.IsActive && gm.Current == ZodiacGameManager.State.Playing &&
                   Mathf.Abs(room.xMin - xMin) < 0.5f;   // chỉ rơi ở khu chứa nó, lúc còn quái
        }
    }

    void Update()
    {
        if (rock == null) return;
        switch (state)
        {
            case State.Waiting:
                if (Time.time < next || !RoomActive) return;
                var player = ZodiacPlayerController.Current;
                if (player == null) return;
                Vector3 p = player.transform.position;
                target = new Vector3(Mathf.Clamp(p.x, xMin + 0.8f, xMax - 0.8f), 0f, p.z);
                rock.position = target + Vector3.up * dropHeight;
                rock.localScale = Vector3.one * 0.2f;
                rock.gameObject.SetActive(true);
                ZodiacFx.Ring(target + Vector3.up * 0.03f, radius * 2f, new Color(1f, 0.3f, 0.2f, 0.8f), warning, flat: true);
                state = State.Warning;
                stateStart = Time.time;
                break;

            case State.Warning:
                float k = Mathf.Clamp01((Time.time - stateStart) / warning);
                rock.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, Mathf.Min(1f, k * 2f));
                rock.position = target + Vector3.up * dropHeight + new Vector3(Mathf.Sin(Time.time * 60f) * 0.05f * k, 0f, 0f);
                if (k >= 1f)
                {
                    state = State.Falling;
                    velocity = 0f;
                }
                break;

            case State.Falling:
                velocity += 40f * Time.deltaTime;
                rock.position += Vector3.down * velocity * Time.deltaTime;
                if (rock.position.y > target.y + 0.1f) return;
                Impact();
                break;
        }
    }

    void Impact()
    {
        rock.gameObject.SetActive(false);
        state = State.Waiting;
        next = Time.time + Random.Range(interval.x, interval.y);
        ZodiacParticles.Debris(target + Vector3.up * 0.4f, debrisColor, 14, 0.6f);
        ZodiacFx.Puff(target + Vector3.up * 0.3f, new Color(0.8f, 0.75f, 0.7f, 0.7f), 1.3f);
        GameFeel.Shake(0.12f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.enemyDeath, 0.9f, 0.15f, target);

        var player = ZodiacPlayerController.Current;
        if (player != null && Near(player.transform.position) && player.transform.position.y < target.y + 0.8f)
            ZodiacGameManager.Instance?.HazardHit(damage, target);
        foreach (var e in FindObjectsByType<EnemyBase>())
            if (e.isActiveAndEnabled && !e.Health.IsDead && Near(e.transform.position))
                e.Health.TakeDamage(new Hit(damage * 2, target, 1f));   // dụ quái đứng vào vòng thì đau hơn
    }

    bool Near(Vector3 p) => new Vector2(p.x - target.x, p.z - target.z).sqrMagnitude <= radius * radius;
}
