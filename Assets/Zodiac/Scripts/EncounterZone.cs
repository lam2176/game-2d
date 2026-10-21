using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một khu (phòng) của màn beat 'em up: vừa khít một khung hình [<see cref="xMin"/>, <see cref="xMax"/>], tường hai đầu luôn đóng,
/// camera đứng yên nên người chơi luôn thấy hết quái. Vào khu (<see cref="ZodiacGameManager.EnterRoom"/>) thì sau một nhịp
/// quái hiện ra ngay trong khu (có vòng khói báo trước) theo từng đợt (<see cref="waves"/>) — đợt sau chỉ tới khi đợt trước
/// bị hạ hết. Dọn xong (và hạ boss nếu có) thì mở lối ra ở mép phải (<see cref="exit"/>) và hiện "ĐI TIẾP →";
/// đi vào lối ra là màn hình tối lại và chuyển sang khu tiếp theo.
/// Hồi sinh ở đèn lồng của khu sau thì khu này coi như đã dọn.
/// Quái sinh ra nhân máu / sát thương theo độ khó (<see cref="ZodiacDifficulty"/>) và có thể thành tinh anh (<see cref="EliteAffix"/>);
/// dọn xong thì chấm hạng khu (<see cref="ComboCounter"/>).
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

    [Tooltip("Tên khu — hiện lên khi vào khu.")]
    public string title = "";
    public float xMin, xMax;
    [Tooltip("Chỗ người chơi xuất hiện khi chuyển sang khu này.")]
    public Vector3 entry;
    public Wave[] waves = Array.Empty<Wave>();
    [Tooltip("Lối ra mép phải (RoomExit) — bật khi dọn xong. Khu cuối không có.")]
    public GameObject exit;
    [Tooltip("Chờ bao lâu sau khi vào khu thì quái mới hiện (giây).")]
    public float startDelay = 1.2f;

    [Header("Boss (không bắt buộc)")]
    public BossFoxSpirit boss;
    public GameObject bossBar;
    public Text bossNameText;
    public AudioClip bossMusic;

    public bool IsActive { get; private set; }
    public bool IsCleared { get; private set; }
    /// <summary>Số quái của khu còn sống (để HUD / kiểm tra).</summary>
    public int AliveCount => alive.FindAll(h => h != null && !h.IsDead && h.gameObject.activeInHierarchy).Count;
    public event Action<EncounterZone> Cleared;

    readonly List<Health> alive = new List<Health>();

    void Awake()
    {
        if (exit != null) exit.SetActive(false);
        if (bossBar != null) bossBar.SetActive(false);
    }

    /// <summary>Hồi sinh ở khu phía sau: đánh dấu đã dọn, không sinh quái.</summary>
    public void MarkCleared()
    {
        IsCleared = true;
        if (boss != null) boss.gameObject.SetActive(false);
        if (exit != null) exit.SetActive(true);
    }

    /// <summary>Người chơi vừa vào khu.</summary>
    public void Begin()
    {
        if (IsActive || IsCleared) return;
        IsActive = true;
        int count = boss != null ? 6 : 0;
        foreach (var w in waves) count += w.spawns.Length;
        if (count > 0) ComboCounter.BeginRoom(count);
        if (boss != null)
        {
            boss.ApplyScaling(ZodiacDifficulty.HealthScale, ZodiacDifficulty.DamageScale, ZodiacDifficulty.LootScale);
            if (bossBar != null) bossBar.SetActive(true);
            if (bossNameText != null) bossNameText.text = boss.bossName;
            ZodiacAudio.PlayMusic(bossMusic, 0.6f);
        }
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        yield return new WaitForSeconds(startDelay);
        if (boss != null) boss.Activate();
        foreach (var wave in waves)
        {
            float lastDelay = 0f;
            foreach (var spawn in wave.spawns)
            {
                StartCoroutine(SpawnLater(spawn));
                lastDelay = Mathf.Max(lastDelay, spawn.delay);
            }
            yield return new WaitForSeconds(lastDelay + SpawnWarning + 0.1f);
            while (AliveCount > 0) yield return null;
            yield return new WaitForSeconds(0.6f);
        }
        if (boss != null) while (!boss.Health.IsDead) yield return null;
        Clear();
    }

    /// <summary>Vòng khói báo chỗ quái sắp hiện (giây).</summary>
    const float SpawnWarning = 0.5f;

    IEnumerator SpawnLater(Spawn spawn)
    {
        if (spawn.delay > 0f) yield return new WaitForSeconds(spawn.delay);
        if (spawn.prefab == null) yield break;
        var ground = new Vector3(spawn.position.x, 0.02f, spawn.position.z);
        ZodiacFx.Ring(ground, 1.2f, new Color(0.35f, 0.2f, 0.4f, 0.8f), SpawnWarning, flat: true);
        ZodiacParticles.Dust(ground + Vector3.up * 0.1f, Vector3.up, 8, new Color(0.4f, 0.3f, 0.45f, 0.8f));
        yield return new WaitForSeconds(SpawnWarning);
        var go = Instantiate(spawn.prefab, spawn.position, Quaternion.identity);
        if (go.TryGetComponent(out EnemyBase enemy))
        {
            enemy.alwaysAggro = true;
            enemy.ApplyScaling(ZodiacDifficulty.HealthScale, ZodiacDifficulty.DamageScale, ZodiacDifficulty.LootScale);
            if (UnityEngine.Random.value < ZodiacDifficulty.EliteChance || ForceElite)
                EliteAffix.Make(enemy, EliteAffix.RandomKind(), spawn.prefab, Track);
        }
        if (go.TryGetComponent(out Health h)) alive.Add(h);
        ZodiacFx.Puff(spawn.position + Vector3.up * 0.6f, new Color(0.6f, 0.5f, 0.7f, 0.8f), 1.6f);
    }

    /// <summary>Thêm quái vào khu đang đánh (phân thân của tinh anh) — khu chưa dọn xong khi nó còn sống.</summary>
    public void Track(Health h)
    {
        if (h != null) alive.Add(h);
    }

    /// <summary>Mọi quái sinh ra đều là tinh anh (kiểm thử).</summary>
    public static bool ForceElite;

    void Clear()
    {
        IsActive = false;
        IsCleared = true;
        var player = ZodiacPlayerController.Current;
        ComboCounter.EndRoom(player != null ? player.transform.position : entry);
        if (bossBar != null) bossBar.SetActive(false);
        if (boss != null && ZodiacGameManager.Instance != null) ZodiacAudio.PlayMusic(ZodiacGameManager.Instance.music);
        if (exit != null)
        {
            exit.SetActive(true);
            GoIndicator.Show();
        }
        Cleared?.Invoke(this);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(xMin, 0f, -3f), new Vector3(xMin, 0f, 3f));
        Gizmos.DrawLine(new Vector3(xMax, 0f, -3f), new Vector3(xMax, 0f, 3f));
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(entry, 0.4f);
        if (waves == null) return;
        Gizmos.color = Color.yellow;
        foreach (var w in waves)
            foreach (var s in w.spawns)
                Gizmos.DrawWireSphere(s.position, 0.4f);
    }
}
