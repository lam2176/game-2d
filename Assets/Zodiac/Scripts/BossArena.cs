using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Đấu trường boss: người chơi bước vào vùng trigger thì cổng đá hạ xuống sau lưng, camera khoá trong đấu trường,
/// boss thức dậy và thanh máu boss hiện trên HUD. Boss bị hạ thì cổng mở, camera theo lại bình thường.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossArena : MonoBehaviour
{
    public BossFoxSpirit boss;
    [Tooltip("Các bức tường bật lên khi trận đấu bắt đầu.")]
    public GameObject[] gates;
    [Tooltip("Vùng camera được nhìn trong lúc đánh boss.")]
    public Rect cameraBounds;
    public GameObject bossBar;
    public Text bossNameText;

    ZodiacCameraFollow follow;
    Rect savedBounds;
    bool started;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        SetGates(false);
        if (bossBar != null) bossBar.SetActive(false);
    }

    void Start()
    {
        follow = FindAnyObjectByType<ZodiacCameraFollow>();
        if (boss != null) boss.Health.Died += EndFight;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (started || boss == null || boss.Health.IsDead || ZodiacPlayerController.FromCollider(other) == null) return;
        started = true;
        SetGates(true);
        if (follow != null)
        {
            savedBounds = follow.bounds;
            follow.bounds = cameraBounds;
        }
        if (bossBar != null) bossBar.SetActive(true);
        if (bossNameText != null) bossNameText.text = boss.bossName;
        boss.Activate();
    }

    void EndFight()
    {
        SetGates(false);
        if (follow != null && started) follow.bounds = savedBounds;
        if (bossBar != null) bossBar.SetActive(false);
    }

    void SetGates(bool closed)
    {
        if (gates == null) return;
        foreach (var g in gates) if (g != null) g.SetActive(closed);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(cameraBounds.center, cameraBounds.size);
    }
}
