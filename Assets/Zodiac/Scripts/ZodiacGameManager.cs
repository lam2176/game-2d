using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Trạng thái màn chơi: đang chơi → thắng (chạm cổng đích) hoặc thua (Health người chơi về 0).
/// Rơi xuống vực (KillZone / thấp hơn fallLimitY) hoặc dính gai: mất 1 máu và hồi sinh ở đèn lồng gần nhất
/// (như Hollow Knight / Celeste) — chỉ thua khi hết máu.
/// Thua có thể "Hồi sinh tại đèn lồng": tải lại màn và đặt người chơi ở đèn lồng đã thắp xa nhất.
/// Màn có boss thì cổng đích chỉ hiện khi boss bị hạ.
/// Đếm đồng xu, cập nhật HUD, hiện bảng thắng/thua. Phím F5 chơi lại nhanh (R là Tuyệt kỹ).
/// </summary>
public class ZodiacGameManager : MonoBehaviour
{
    public enum State { Playing, Won, Lost }

    public static ZodiacGameManager Instance { get; private set; }

    public ZodiacPlayerController player;
    public PlayerTrigger goal;
    public PlayerTrigger killZone;
    public string startSceneName = "Zodiac_Start";
    [Tooltip("Scene màn tiếp theo. Trống = màn cuối, thắng thì về màn hình chính.")]
    public string nextSceneName = "";
    public string levelTitle = "";
    [Tooltip("Dự phòng cho KillZone: nhân vật xuống thấp hơn độ cao này là rơi vực.")]
    public float fallLimitY = -10f;
    [Tooltip("Boss của màn (không bắt buộc) — cổng đích ẩn cho tới khi boss bị hạ.")]
    public BossFoxSpirit boss;
    [Min(0f)] public float respawnDelay = 0.45f;

    [Header("Âm thanh")]
    public AudioClip music;
    [Tooltip("Tiếng bước chân trên đá (màn hang) thay vì cỏ.")]
    public bool stoneFootsteps;

    [Header("UI")]
    public Text collectText;
    public GameObject winPanel;
    public Text winDetailText;
    public GameObject losePanel;
    [Tooltip("Dòng lý do thua trong LosePanel.")]
    public Text loseDetailText;
    public Text nextButtonLabel;

    public State Current { get; private set; } = State.Playing;
    public Checkpoint ActiveCheckpoint { get; private set; }

    int collected, total, coinsTowardHeal;
    Checkpoint[] checkpoints;
    Vector2 startPosition;
    bool respawning;
    Health playerHealth;

    /// <summary>Hồi sinh sau khi tải lại scene: (tên scene, chỉ số đèn lồng).</summary>
    static string continueScene;
    static int continueCheckpoint = -1;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        var items = FindObjectsByType<Collectible>();
        total = items.Length;
        foreach (var item in items) item.Collected += OnCollected;
        if (goal != null) goal.Entered += _ => Finish(State.Won);
        if (killZone != null) killZone.Entered += _ => HazardHit(ZodiacBalance.Unit, player.transform.position);
        playerHealth = player.GetComponent<Health>();
        if (playerHealth != null) playerHealth.Died += () => Finish(State.Lost, "Bạn đã bị hạ gục");

        startPosition = player.transform.position;
        checkpoints = FindObjectsByType<Checkpoint>().OrderBy(c => c.index).ToArray();
        foreach (var cp in checkpoints) cp.Reached += OnCheckpoint;

        if (boss != null)
        {
            if (goal != null) goal.gameObject.SetActive(false);
            boss.Health.Died += OnBossDefeated;
        }

        if (continueScene == SceneManager.GetActiveScene().name && continueCheckpoint >= 0)
        {
            var cp = checkpoints.FirstOrDefault(c => c.index == continueCheckpoint);
            if (cp != null)
            {
                foreach (var c in checkpoints) if (c.index <= cp.index) c.SetLit(true);
                ActiveCheckpoint = cp;
                player.Teleport(cp.SpawnPosition);
                var follow = FindAnyObjectByType<ZodiacCameraFollow>();
                if (follow != null) follow.SnapToTarget();
            }
        }
        continueScene = null;
        continueCheckpoint = -1;

        winPanel.SetActive(false);
        losePanel.SetActive(false);
        RefreshHud();
        ZodiacAudio.PlayMusic(music);
    }

    void Update()
    {
        if (Current == State.Playing && !respawning && player.transform.position.y < fallLimitY)
            HazardHit(ZodiacBalance.Unit, player.transform.position);
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame) Restart();
    }

    void OnCollected(Collectible item)
    {
        if (Current != State.Playing) return;
        collected++;
        var c = player.Character;
        if (c != null && c.coinsPerHeal > 0 && playerHealth != null && ++coinsTowardHeal >= c.coinsPerHeal)
        {
            coinsTowardHeal = 0;
            if (playerHealth.Heal(ZodiacBalance.Unit) > 0) ZodiacFx.Ring(player.BodyCollider.bounds.center, 1.5f, new Color(0.5f, 1f, 0.6f, 0.8f));
        }
        RefreshHud();
    }

    void OnCheckpoint(Checkpoint cp)
    {
        if (ActiveCheckpoint == null || cp.index > ActiveCheckpoint.index) ActiveCheckpoint = cp;
        playerHealth?.Heal(ZodiacBalance.Unit);
        ZodiacFx.Ring(cp.transform.position + Vector3.up * 1.5f, 2f, new Color(1f, 0.6f, 0.2f, 0.7f));
    }

    void OnBossDefeated()
    {
        if (goal == null) return;
        goal.gameObject.SetActive(true);
        ZodiacFx.Ring(goal.transform.position + Vector3.up * 2f, 4f, new Color(1f, 0.9f, 0.4f, 0.8f), 0.8f);
    }

    /// <summary>Rơi vực / dính gai: trừ máu (không đẩy lùi), còn sống thì đưa về đèn lồng gần nhất.</summary>
    public void HazardHit(int damage, Vector2 source)
    {
        if (Current != State.Playing || respawning) return;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.spike, 0.9f);
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(new Hit(damage, source, 0f));
            if (playerHealth.IsDead) return;   // Died đã gọi Finish
        }
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        respawning = true;
        player.SetControlEnabled(false);
        yield return new WaitForSeconds(respawnDelay * 0.4f);
        bool done = false;
        // Mờ đen → dịch chuyển về đèn lồng → sáng lại
        ScreenFader.Blink(() =>
        {
            if (Current == State.Playing)
            {
                player.SetControlEnabled(true);
                player.Teleport(ActiveCheckpoint != null ? ActiveCheckpoint.SpawnPosition : startPosition);
                playerHealth?.SetInvulnerable(1.2f);
                var follow = FindAnyObjectByType<ZodiacCameraFollow>();
                if (follow != null) follow.SnapToTarget();
            }
            done = true;
        }, respawnDelay * 0.3f);
        while (!done) yield return null;
        respawning = false;
    }

    void Finish(State result, string loseReason = null)
    {
        if (Current != State.Playing) return;
        Current = result;
        player.SetControlEnabled(false);
        var res = ZodiacResources.Get;
        ZodiacAudio.PlayMusic(null, 0.6f);
        if (res != null) ZodiacAudio.Play(result == State.Won ? res.win : res.lose, 1f, 0f);
        if (result == State.Won)
        {
            winDetailText.text = $"Thu được {collected}/{total} đồng xu";
            if (nextButtonLabel != null)
                nextButtonLabel.text = string.IsNullOrEmpty(nextSceneName) ? "Màn hình chính" : "Màn tiếp theo";
            winPanel.SetActive(true);
        }
        else
        {
            if (loseDetailText != null && loseReason != null) loseDetailText.text = loseReason;
            losePanel.SetActive(true);
        }
    }

    void RefreshHud()
    {
        if (collectText != null) collectText.text = $"Đồng xu: {collected}/{total}";
    }

    public void Restart() => ScreenFader.LoadScene(SceneManager.GetActiveScene().name);

    /// <summary>Tải lại màn và bắt đầu ở đèn lồng đã thắp xa nhất (không có thì như chơi lại).</summary>
    public void ContinueFromCheckpoint()
    {
        if (ActiveCheckpoint != null)
        {
            continueScene = SceneManager.GetActiveScene().name;
            continueCheckpoint = ActiveCheckpoint.index;
        }
        Restart();
    }

    public void NextLevel() =>
        ScreenFader.LoadScene(string.IsNullOrEmpty(nextSceneName) ? startSceneName : nextSceneName);

    public void BackToMenu() => ScreenFader.LoadScene(startSceneName);
}
