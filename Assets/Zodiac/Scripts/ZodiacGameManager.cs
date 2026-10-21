using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Trạng thái màn chơi: đang chơi → thắng (chạm cổng đích) hoặc thua (Health người chơi về 0).
/// Màn chia thành các khu (<see cref="rooms"/>), mỗi khu vừa một khung hình: vào khu thì camera khoá, hiện tên khu,
/// quái kéo tới; dọn xong đi ra mép phải là màn hình tối lại và chuyển sang khu kế (<see cref="NextRoom"/>).
/// Cổng đích chỉ hiện khi dọn xong khu cuối (và hạ boss nếu có).
/// Thua có thể "Hồi sinh tại đèn lồng": tải lại màn và đặt người chơi ở đèn lồng đã thắp xa nhất —
/// các khu phía trước đèn lồng coi như đã dọn.
/// Giữ giới hạn mặt đường (làn trong cùng / ngoài cùng) và khu đang đánh cho quái dùng chung.
/// Đếm đồng xu, cập nhật HUD, hiện bảng thắng/thua. Phím F5 chơi lại nhanh (R là Tuyệt kỹ).
/// </summary>
public class ZodiacGameManager : MonoBehaviour
{
    public enum State { Playing, Won, Lost }

    public static ZodiacGameManager Instance { get; private set; }

    /// <summary>Z làn ngoài cùng (gần camera) và trong cùng của mặt đường.</summary>
    public static float LaneMinZ => Instance != null ? Instance.laneMinZ : -3f;
    public static float LaneMaxZ => Instance != null ? Instance.laneMaxZ : 3f;
    /// <summary>Mép trái / phải khu đang đánh (quái không chạy ra ngoài khung hình).</summary>
    public static float RoomMinX => Instance != null && Instance.Room != null ? Instance.Room.xMin : -1000f;
    public static float RoomMaxX => Instance != null && Instance.Room != null ? Instance.Room.xMax : 1000f;

    public ZodiacPlayerController player;
    public PlayerTrigger goal;
    public string startSceneName = "Zodiac_Start";
    [Tooltip("Scene màn tiếp theo. Trống = màn cuối, thắng thì về màn hình chính.")]
    public string nextSceneName = "";
    public string levelTitle = "";
    [Tooltip("Các khu theo thứ tự. Cổng đích hiện khi dọn xong khu cuối.")]
    public EncounterZone[] rooms = System.Array.Empty<EncounterZone>();

    [Header("Mặt đường")]
    public float laneMinZ = -3f;
    public float laneMaxZ = 3f;

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
    [Tooltip("Tên khu hiện giữa màn hình khi vào khu.")]
    public Text roomBanner;

    public State Current { get; private set; } = State.Playing;
    public Checkpoint ActiveCheckpoint { get; private set; }
    public int RoomIndex { get; private set; } = -1;
    public EncounterZone Room => RoomIndex >= 0 && RoomIndex < rooms.Length ? rooms[RoomIndex] : null;

    int collected, total, coinsTowardHeal;
    /// <summary>Chỗ hồi sinh (đèn lồng) khi tải lại màn — dịch chuyển hẳn ở Start (Rigidbody đã sẵn sàng).</summary>
    Vector3? respawnAt;
    Checkpoint[] checkpoints;
    Health playerHealth;

    /// <summary>Hồi sinh sau khi tải lại scene: (tên scene, chỉ số đèn lồng).</summary>
    static string continueScene;
    static int continueCheckpoint = -1;

    void Awake()
    {
        Instance = this;
        // Người chơi đặt ở đèn lồng trước khi các vùng giao tranh chạy Start (để biết vùng nào đã qua)
        checkpoints = FindObjectsByType<Checkpoint>().OrderBy(c => c.index).ToArray();
        if (continueScene == SceneManager.GetActiveScene().name && continueCheckpoint >= 0)
        {
            var cp = checkpoints.FirstOrDefault(c => c.index == continueCheckpoint);
            if (cp != null)
            {
                foreach (var c in checkpoints) if (c.index <= cp.index) c.SetLit(true);
                ActiveCheckpoint = cp;
                respawnAt = cp.SpawnPosition;
            }
        }
        continueScene = null;
        continueCheckpoint = -1;
    }

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
        playerHealth = player.GetComponent<Health>();
        if (playerHealth != null) playerHealth.Died += () => Finish(State.Lost, "Bạn đã bị hạ gục");
        foreach (var cp in checkpoints) cp.Reached += OnCheckpoint;

        // Hồi sinh ở đèn lồng: dịch chuyển qua Rigidbody (chỉ gán transform thì Rigidbody nội suy kéo nhân vật
        // về chỗ xuất phát trong khi camera khoá vào khu có đèn lồng → nhân vật "biến mất" khỏi khung hình).
        if (respawnAt.HasValue) player.Teleport(respawnAt.Value);
        if (rooms.Length > 0)
        {
            if (goal != null) goal.gameObject.SetActive(false);
            rooms[rooms.Length - 1].Cleared += _ => ShowGoal();
            // Khu đang đứng (hồi sinh ở đèn lồng giữa màn) — các khu trước coi như đã dọn
            int start = 0;
            for (int i = 0; i < rooms.Length; i++)
                if (player.transform.position.x >= rooms[i].xMin) start = i;
            for (int i = 0; i < start; i++) rooms[i].MarkCleared();
            EnterRoom(start);
        }

        winPanel.SetActive(false);
        losePanel.SetActive(false);
        RefreshHud();
        ZodiacAudio.PlayMusic(music);
    }

    void Update()
    {
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
            if (playerHealth.Heal(ZodiacBalance.Unit) > 0) ZodiacFx.Ring(player.BodyCenter, 1.5f, new Color(0.5f, 1f, 0.6f, 0.8f));
        }
        RefreshHud();
    }

    void OnCheckpoint(Checkpoint cp)
    {
        if (ActiveCheckpoint == null || cp.index > ActiveCheckpoint.index) ActiveCheckpoint = cp;
        playerHealth?.Heal(ZodiacBalance.Unit);
        ZodiacFx.Ring(cp.transform.position + Vector3.up * 1.5f, 2f, new Color(1f, 0.6f, 0.2f, 0.7f));
    }

    void ShowGoal()
    {
        if (goal == null) return;
        goal.gameObject.SetActive(true);
        ZodiacFx.Ring(goal.transform.position, 4f, new Color(1f, 0.9f, 0.4f, 0.8f), 0.8f, flat: true);
        GoIndicator.Show();
    }

    /// <summary>Vào khu: khoá camera vào khu, hiện tên khu, bắt đầu đợt quái.</summary>
    public void EnterRoom(int index)
    {
        RoomIndex = Mathf.Clamp(index, 0, rooms.Length - 1);
        var room = rooms[RoomIndex];
        var follow = FindAnyObjectByType<ZodiacCameraFollow>();
        if (follow != null)
        {
            follow.Lock(room.xMin, room.xMax);
            follow.SnapToTarget();
        }
        if (roomBanner != null)
        {
            roomBanner.text = $"Khu {RoomIndex + 1}/{rooms.Length}" + (string.IsNullOrEmpty(room.title) ? "" : $"\n<size=34>{room.title}</size>");
            if (roomBanner.TryGetComponent(out TimedFade fade)) fade.Restart();
        }
        room.Begin();
    }

    /// <summary>Đi vào lối ra của khu đã dọn: tối màn hình, chuyển sang khu kế.</summary>
    public void NextRoom()
    {
        if (Current != State.Playing || RoomIndex + 1 >= rooms.Length) return;
        int next = RoomIndex + 1;
        var target = rooms[next];
        player.SetControlEnabled(false);
        ScreenFader.Blink(() =>
        {
            if (Current != State.Playing) return;
            player.SetControlEnabled(true);
            player.Teleport(target.entry);
            player.SetFacing(1f);
            playerHealth?.SetInvulnerable(1f);
            EnterRoom(next);
        }, 0.35f, "Đang tới khu tiếp theo…" + (string.IsNullOrEmpty(target.title) ? "" : $"\n<size=32>{target.title}</size>"), 0.5f);
    }

    /// <summary>Dính bẫy (gai, lửa): trừ máu, bị đẩy ra khỏi bẫy.</summary>
    public void HazardHit(int damage, Vector3 source)
    {
        if (Current != State.Playing || playerHealth == null) return;
        if (playerHealth.TakeDamage(new Hit(damage, source, 0.8f)))
        {
            var res = ZodiacResources.Get;
            if (res != null) ZodiacAudio.Play(res.spike, 0.9f);
        }
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
