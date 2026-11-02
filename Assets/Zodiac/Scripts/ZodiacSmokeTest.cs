using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Kiểm tra khói tự động cho bản build: chạy game với tham số <c>-zodiacSmoke</c> thì lần lượt mở mọi scene trong
/// Build Settings, cho người chơi bất tử, đi qua từng khu (cả khu bí mật), mở thử các cửa sổ (cài đặt, hành trang,
/// sổ ghi chép, làng, tháp), ép thắng từng màn (lời kết, kết hồi, kết thúc), ghi mọi lỗi / exception vào
/// <c>smoke-report.txt</c> cạnh file exe (điện thoại: persistentDataPath, chạy bằng
/// <c>adb shell am start -n com.tunglam.zodiac/com.unity3d.player.UnityPlayerGameActivity -e unity -zodiacSmoke</c>) rồi tự thoát. Dùng hồ sơ tạm riêng — không đụng ô lưu / tài khoản thật.
/// Không có tham số thì không làm gì.
/// </summary>
public class ZodiacSmokeTest : MonoBehaviour
{
    readonly List<string> errors = new List<string>();
    readonly StringBuilder log = new StringBuilder();
    string currentScene = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-zodiacSmoke") < 0) return;
        var go = new GameObject("ZodiacSmokeTest");
        DontDestroyOnLoad(go);
        go.AddComponent<ZodiacSmokeTest>();
    }

    void OnEnable() => Application.logMessageReceived += OnLog;
    void OnDisable() => Application.logMessageReceived -= OnLog;

    void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        string line = $"[{currentScene}] {type}: {message}\n    {stack.Split('\n')[0]}";
        if (errors.Count < 300 && !errors.Contains(line)) errors.Add(line);
    }

    void Note(string s)
    {
        log.AppendLine($"{Time.realtimeSinceStartup,7:0.0}s  {s}");
    }

    IEnumerator Start()
    {
        PlayerProfile.UseSmokeFile();
        yield return null;
        int count = SceneManager.sceneCountInBuildSettings;
        string only = Arg("-zodiacOnly");
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            currentScene = Path.GetFileNameWithoutExtension(path);
            if (only != null && currentScene != only) continue;
            Note($"Mở {currentScene}");
            SceneManager.LoadScene(i);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            yield return Exercise();
        }
        WriteReport();
        Application.Quit(errors.Count == 0 ? 0 : 1);
    }

    IEnumerator Exercise()
    {
        var gm = ZodiacGameManager.Instance;
        StoryUi.Next();
        if (gm == null)
        {
            // Màn hình chính: mở thử Cài đặt rồi đóng
            SettingsWindow.Open();
            yield return new WaitForSecondsRealtime(0.5f);
            FindAnyObjectByType<SettingsWindow>()?.Close();
            yield break;
        }
        var player = gm.player;
        var health = player != null ? player.GetComponent<Health>() : null;
        health?.SetInvulnerable(100000f);
        Note($"  khu: {gm.rooms.Length}, hub: {gm.isHub}, tháp: {FindAnyObjectByType<TowerRun>() != null}");

        if (gm.isHub)
        {
            foreach (VillageWindow.Mode m in System.Enum.GetValues(typeof(VillageWindow.Mode)))
            {
                VillageWindow.Open(m);
                yield return new WaitForSecondsRealtime(0.4f);
                SkipStory();
                var w = FindAnyObjectByType<VillageWindow>();
                if (w != null) w.SendMessage("Close", SendMessageOptions.DontRequireReceiver);
                yield return null;
            }
            InventoryWindow.OpenTraining();
            yield return new WaitForSecondsRealtime(0.3f);
            FindAnyObjectByType<InventoryWindow>()?.ShowJournal();
            yield return new WaitForSecondsRealtime(0.3f);
            FindAnyObjectByType<InventoryWindow>()?.SendMessage("Close", SendMessageOptions.DontRequireReceiver);
            yield break;
        }

        // Đi qua từng khu: dịch chuyển tới lối vào, chờ quái sinh, boss kích hoạt
        for (int r = 0; r < gm.rooms.Length; r++)
        {
            var room = gm.rooms[r];
            if (room == null) continue;
            player.Teleport(room.entry);
            gm.EnterRoom(r);
            for (float t = 0f; t < 3f; t += 0.25f)
            {
                SkipStory();
                health?.SetInvulnerable(100000f);
                yield return new WaitForSecondsRealtime(0.25f);
            }
            Note($"  khu {r + 1} '{room.title}': quái còn {room.AliveCount}, boss: {(room.boss != null ? room.boss.bossName : "-")}");
        }
        // Khu bí mật
        foreach (var door in FindObjectsByType<SecretDoor>(FindObjectsInactive.Include))
        {
            if (door.returns || door.target == null) continue;
            gm.EnterSecret(door.target, door.transform.position, null);
            for (float t = 0f; t < 2.5f; t += 0.25f)
            {
                SkipStory();
                yield return new WaitForSecondsRealtime(0.25f);
            }
            Note($"  khu bí mật '{door.target.title}': quái còn {door.target.AliveCount}");
            gm.LeaveSecret();
            yield return new WaitForSecondsRealtime(1.2f);
        }
        // Ép thắng màn (trừ tháp): đi qua lời kết màn, kết hồi, kết thúc riêng; kiểm Ác Mộng + số kết thúc
        if (FindAnyObjectByType<TowerRun>() == null)
        {
            bool nmBefore = PlayerProfile.Current.nightmareUnlocked;
            int endBefore = PlayerProfile.Current.endingsSeen.Count;
            gm.SmokeWin();
            int panels = 0;
            for (float t = 0f; t < 8f && !gm.winPanel.activeSelf; t += 0.1f)
            {
                if (StoryUi.IsOpen) { StoryUi.Next(); panels++; }
                yield return new WaitForSecondsRealtime(0.1f);
            }
            var p = PlayerProfile.Current;
            bool final = string.IsNullOrEmpty(gm.nextSceneName);
            bool clear = final && gm.act >= 4;
            Note($"  thắng màn {gm.levelIndex + 1} (hồi {gm.act}{(final ? ", cuối hồi" : "")}): bảng thắng {(gm.winPanel.activeSelf ? "hiện" : "KHÔNG hiện")}, " +
                 $"{panels} lần bấm truyện, mở tới màn {p.unlockedLevels}, Ác Mộng {(p.nightmareUnlocked ? "mở" : "khoá")}, kết thúc {p.endingsSeen.Count}");
            if (!gm.winPanel.activeSelf) errors.Add($"[{currentScene}] Thắng màn nhưng bảng thắng không hiện");
            if (clear ? !p.nightmareUnlocked : p.nightmareUnlocked != nmBefore)
                errors.Add($"[{currentScene}] Ác Mộng {(p.nightmareUnlocked ? "mở" : "khoá")} sai sau màn {gm.levelIndex + 1}");
            if (p.endingsSeen.Count - endBefore != (clear ? 1 : 0))
                errors.Add($"[{currentScene}] Số kết thúc {p.endingsSeen.Count} sai sau màn {gm.levelIndex + 1}");
        }
        // Tháp: để chạy một lúc (sinh quái, luật tầng); -zodiacOnly thì chạy lâu hơn và chụp màn hình
        if (FindAnyObjectByType<TowerRun>() != null)
        {
            float duration = Arg("-zodiacOnly") != null ? 40f : 8f;
            for (float t = 0f; t < duration; t += 0.5f)
            {
                health?.SetInvulnerable(100000f);
                if (Mathf.Repeat(t, 4f) < 0.01f) Shot($"{currentScene}_{t:00}");
                yield return new WaitForSecondsRealtime(0.5f);
            }
            Note($"  tháp: tầng {TowerRun.CurrentFloor}, camera {Describe(Camera.main)}");
        }
    }

    static string Arg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    /// <summary>Chụp màn hình khi chạy có đồ hoạ (cạnh exe / persistentDataPath trên điện thoại).</summary>
    void Shot(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        string dir = Application.isMobilePlatform ? Application.persistentDataPath : Path.GetDirectoryName(Application.dataPath);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"smoke_{name}.png"));
    }

    static string Describe(Camera c) => c == null ? "không có" :
        $"{c.transform.position} xoá {c.clearFlags} màu {c.backgroundColor}, sương {(RenderSettings.fog ? $"{RenderSettings.fogStartDistance}-{RenderSettings.fogEndDistance} {RenderSettings.fogColor}" : "tắt")}";

    static void SkipStory()
    {
        for (int k = 0; k < 40 && StoryUi.IsOpen; k++) StoryUi.Next();
    }

    void WriteReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Kiểm tra khói — {System.DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Lỗi: {errors.Count}");
        foreach (var e in errors) sb.AppendLine(e);
        sb.AppendLine();
        sb.Append(log);
        // Điện thoại: thư mục APK chỉ đọc → ghi vào persistentDataPath, in thêm ra log (adb logcat -s Unity)
        string dir = Application.isMobilePlatform ? Application.persistentDataPath : Path.GetDirectoryName(Application.dataPath);
        Debug.Log("[ZodiacSmoke]\n" + sb);
        try { File.WriteAllText(Path.Combine(dir, "smoke-report.txt"), sb.ToString(), Encoding.UTF8); }
        catch (System.Exception e) { Debug.LogWarning("[ZodiacSmoke] Không ghi được báo cáo: " + e.Message); }
    }
}
