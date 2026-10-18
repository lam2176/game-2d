using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Trạng thái màn chơi: đang chơi → thắng (chạm đích) hoặc thua (rơi xuống vực).
/// Đếm vật phẩm, cập nhật HUD, hiện bảng thắng/thua. Phím R chơi lại nhanh.
/// </summary>
public class ZodiacGameManager : MonoBehaviour
{
    public enum State { Playing, Won, Lost }

    public ZodiacPlayerController player;
    public PlayerTrigger goal;
    public PlayerTrigger killZone;
    public string startSceneName = "Zodiac_Start";

    [Header("UI")]
    public Text collectText;
    public GameObject winPanel;
    public Text winDetailText;
    public GameObject losePanel;

    public State Current { get; private set; } = State.Playing;

    int collected, total;

    void Start()
    {
        var items = FindObjectsByType<Collectible>();
        total = items.Length;
        foreach (var item in items) item.Collected += OnCollected;
        goal.Entered += _ => Finish(State.Won);
        killZone.Entered += _ => Finish(State.Lost);

        winPanel.SetActive(false);
        losePanel.SetActive(false);
        RefreshHud();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Restart();
    }

    void OnCollected(Collectible item)
    {
        if (Current != State.Playing) return;
        collected++;
        RefreshHud();
    }

    void Finish(State result)
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
            losePanel.SetActive(true);
        }
    }

    void RefreshHud() => collectText.text = $"Đồng xu: {collected}/{total}";

    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public void BackToMenu() => SceneManager.LoadScene(startSceneName);
}
