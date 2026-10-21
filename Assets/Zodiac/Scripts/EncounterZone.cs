using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vùng giao tranh của beat 'em up: người chơi đi tới <see cref="triggerX"/> thì tường vô hình dựng lên hai đầu đấu trường
/// [<see cref="xMin"/>, <see cref="xMax"/>], camera khoá khung, quái kéo tới theo từng đợt (<see cref="waves"/>) —
/// đợt sau chỉ tới khi đợt trước bị hạ hết. Xong mọi đợt (và boss nếu có) thì mở tường, camera theo lại và hiện "ĐI TIẾP ➜".
/// Tường chỉ chặn người chơi — quái từ ngoài mép màn hình vẫn đi vào được.
/// Hồi sinh ở đèn lồng phía sau vùng đã qua thì vùng đó coi như đã dọn xong.
/// </summary>
public class EncounterZone : MonoBehaviour
{
    [Serializable]
    public class Spawn
    {
        public GameObject prefab;
        public Vector3 position;
        [Tooltip("Ra sau đầu đợt bao lâu (giây).")]
        public float delay;
    }

    [Serializable]
    public class Wave
    {
        public Spawn[] spawns = Array.Empty<Spawn>();
    }

    public float xMin, xMax;
    [Tooltip("Người chơi đi tới X này thì bắt đầu.")]
    public float triggerX;
    public Wave[] waves = Array.Empty<Wave>();
    [Tooltip("Tường vô hình hai đầu đấu trường (bật khi đang đánh).")]
    public GameObject[] walls = Array.Empty<GameObject>();

    [Header("Boss (không bắt buộc)")]
    public BossFoxSpirit boss;
    public GameObject bossBar;
    public Text bossNameText;
    public AudioClip bossMusic;

    public bool IsActive { get; private set; }
    public bool IsCleared { get; private set; }
    /// <summary>Số quái của vùng còn sống (để HUD / kiểm tra).</summary>
    public int AliveCount => alive.FindAll(h => h != null && !h.IsDead && h.gameObject.activeInHierarchy).Count;
    public event Action<EncounterZone> Cleared;

    readonly List<Health> alive = new List<Health>();
    ZodiacCameraFollow follow;

    void Awake()
    {
        SetWalls(false);
        if (bossBar != null) bossBar.SetActive(false);
    }

    void Start()
    {
        follow = FindAnyObjectByType<ZodiacCameraFollow>();
        var player = ZodiacPlayerController.Current;
        if (player != null && player.transform.position.x > xMax)
        {
            // Hồi sinh ở đèn lồng phía sau: vùng này đã dọn rồi
            IsCleared = true;
            if (boss != null) boss.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (IsActive || IsCleared) return;
        var player = ZodiacPlayerController.Current;
        if (player != null && player.ControlEnabled && player.transform.position.x >= triggerX) Begin();
    }

    void Begin()
    {
        IsActive = true;
        SetWalls(true);
        if (follow != null) follow.Lock(xMin, xMax);
        if (boss != null)
        {
            if (bossBar != null) bossBar.SetActive(true);
            if (bossNameText != null) bossNameText.text = boss.bossName;
            ZodiacAudio.PlayMusic(bossMusic, 0.6f);
            boss.Activate();
        }
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        foreach (var wave in waves)
        {
            foreach (var spawn in wave.spawns) StartCoroutine(SpawnLater(spawn));
            float lastDelay = 0f;
            foreach (var spawn in wave.spawns) lastDelay = Mathf.Max(lastDelay, spawn.delay);
            yield return new WaitForSeconds(lastDelay + 0.1f);
            while (AliveCount > 0) yield return null;
            yield return new WaitForSeconds(0.6f);
        }
        if (boss != null) while (!boss.Health.IsDead) yield return null;
        Clear();
    }

    IEnumerator SpawnLater(Spawn spawn)
    {
        if (spawn.delay > 0f) yield return new WaitForSeconds(spawn.delay);
        if (spawn.prefab == null) yield break;
        var go = Instantiate(spawn.prefab, spawn.position, Quaternion.identity);
        if (go.TryGetComponent(out EnemyBase enemy)) enemy.alwaysAggro = true;
        if (go.TryGetComponent(out Health h)) alive.Add(h);
    }

    void Clear()
    {
        IsActive = false;
        IsCleared = true;
        SetWalls(false);
        if (follow != null) follow.Unlock();
        if (bossBar != null) bossBar.SetActive(false);
        if (boss != null && ZodiacGameManager.Instance != null) ZodiacAudio.PlayMusic(ZodiacGameManager.Instance.music);
        if (boss == null) GoIndicator.Show();
        Cleared?.Invoke(this);
    }

    void SetWalls(bool on)
    {
        foreach (var w in walls) if (w != null) w.SetActive(on);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(xMin, 0f, -3f), new Vector3(xMin, 0f, 3f));
        Gizmos.DrawLine(new Vector3(xMax, 0f, -3f), new Vector3(xMax, 0f, 3f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(triggerX, 0f, -3f), new Vector3(triggerX, 2f, -3f));
        if (waves == null) return;
        foreach (var w in waves)
            foreach (var s in w.spawns)
                Gizmos.DrawWireSphere(s.position, 0.4f);
    }
}
