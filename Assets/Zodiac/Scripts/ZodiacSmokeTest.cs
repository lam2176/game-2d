using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Kiểm tra khói tự động cho bản build: chạy game với tham số <c>-zodiacSmoke</c> thì lần lượt mở mọi scene trong
/// Build Settings, cho người chơi bất tử, đi qua từng khu (cả khu bí mật), mở thử các cửa sổ (cài đặt, hành trang,
/// sổ ghi chép, làng, tháp), ghi mọi lỗi / exception vào <c>smoke-report.txt</c> cạnh file exe rồi tự thoát.
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
        yield return null;
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            currentScene = Path.GetFileNameWithoutExtension(path);
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
        // Tháp: để chạy một lúc (sinh quái, luật tầng)
        if (FindAnyObjectByType<TowerRun>() != null)
            for (float t = 0f; t < 8f; t += 0.5f)
            {
                health?.SetInvulnerable(100000f);
                yield return new WaitForSecondsRealtime(0.5f);
            }
    }

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
        string dir = Path.GetDirectoryName(Application.dataPath);
        File.WriteAllText(Path.Combine(dir, "smoke-report.txt"), sb.ToString(), Encoding.UTF8);
    }
}
