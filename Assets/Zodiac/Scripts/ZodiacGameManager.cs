using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Trạng thái màn chơi: đang chơi → thắng (chạm đích) hoặc thua (rơi xuống vực: chạm KillZone hoặc thấp hơn
/// fallLimitY; hoặc Health của người chơi về 0).
/// Đếm vật phẩm, cập nhật HUD, hiện bảng thắng/thua. Phím R chơi lại nhanh.
/// </summary>
public class ZodiacGameManager : MonoBehaviour
{
    public enum State { Playing, Won, Lost }

    public ZodiacPlayerController player;
    public PlayerTrigger goal;
    public PlayerTrigger killZone;
    public string startSceneName = "Zodiac_Start";
    [Tooltip("Dự phòng cho KillZone: nhân vật xuống thấp hơn độ cao này là thua.")]
    public float fallLimitY = -10f;

    [Header("UI")]
    public Text collectText;
    public GameObject winPanel;
    public Text winDetailText;
    public GameObject losePanel;
    [Tooltip("Dòng lý do thua trong LosePanel (rơi vực / bị hạ gục).")]
    public Text loseDetailText;

    public State Current { get; private set; } = State.Playing;

    int collected, total;

    void Start()
    {
        var items = FindObjectsByType<Collectible>();
        total = items.Length;
        foreach (var item in items) item.Collected += OnCollected;
        goal.Entered += _ => Finish(State.Won);
        killZone.Entered += _ => Finish(State.Lost, FellText);
        if (player.TryGetComponent(out Health playerHealth))
            playerHealth.Died += () => Finish(State.Lost, "Bạn đã bị hạ gục");

        winPanel.SetActive(false);
        losePanel.SetActive(false);
        RefreshHud();
    }

    void Update()
    {
        if (Current == State.Playing && player.transform.position.y < fallLimitY) Finish(State.Lost, FellText);
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Restart();
    }

    void OnCollected(Collectible item)
    {
        if (Current != State.Playing) return;
        collected++;
        RefreshHud();
    }

    const string FellText = "Bạn đã rơi xuống vực";

    void Finish(State result, string loseReason = null)
    {
        if (Current != State.Playing) return;
        Current = result;
        player.SetControlEnabled(false);
        if (result == State.Won)
        {
            winDetailText.text = $"Thu được {collected}/{total} đồng xu";
            winPanel.SetActive(true);
        }
        else
        {
            if (loseDetailText != null && loseReason != null) loseDetailText.text = loseReason;
            losePanel.SetActive(true);
        }
    }

    void RefreshHud() => collectText.text = $"Đồng xu: {collected}/{total}";

    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public void BackToMenu() => SceneManager.LoadScene(startSceneName);
}
