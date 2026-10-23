using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tháp Mười Hai Canh Giờ — chế độ vượt tầng vô tận trong một đấu trường (khu duy nhất của scene tháp).
/// Mỗi tầng mang luật của một canh giờ theo thứ tự Tý → Hợi (qua tầng 12 thì lặp lại, quái mạnh hơn):
/// quái nhanh, trâu, toàn tinh anh, sương mù, lửa, độc, Khí dồi dào, hồi máu khi hạ quái, đá rơi, cấm bình thuốc,
/// bầy đông, có trùm. Cứ 6 tầng một boss. Qua tầng: thưởng đồng (mỗi 3 tầng thêm đồ quý), hồi 25% máu,
/// chọn Lên tầng / Rời tháp. Kỷ lục lưu trong <see cref="PlayerProfile.towerBest"/>.
/// </summary>
public class TowerRun : MonoBehaviour
{
    public struct Rule
    {
        public string hour, name, desc;
        public Rule(string hour, string name, string desc)
        {
            this.hour = hour;
            this.name = name;
            this.desc = desc;
        }
    }

    public static readonly Rule[] Rules =
    {
        new Rule("Tý", "Chuột chạy", "Quái nhanh gấp rưỡi"),
        new Rule("Sửu", "Trâu lì", "Quái trâu gần gấp đôi"),
        new Rule("Dần", "Hổ dữ", "Mọi quái đều là tinh anh"),
        new Rule("Mão", "Sương đêm", "Sương mù dày, nhìn không xa"),
        new Rule("Thìn", "Rồng lửa", "Lửa bùng lên dưới chân người chơi"),
        new Rule("Tỵ", "Rắn độc", "Nửa đàn quái có nọc độc"),
        new Rule("Ngọ", "Ngựa phi", "Khí tự hồi rất nhanh — tha hồ tung chiêu"),
        new Rule("Mùi", "Dê hiền", "Hạ mỗi quái hồi 4% máu"),
        new Rule("Thân", "Khỉ nghịch", "Nhũ đá rơi khắp đấu trường"),
        new Rule("Dậu", "Gà gáy", "Không được dùng bình thuốc"),
        new Rule("Tuất", "Chó bầy", "Quái đông gấp đôi nhưng yếu hơn"),
        new Rule("Hợi", "Heo rừng", "Mỗi đợt có một quái trùm"),
    };

    public EncounterZone arena;
    [Tooltip("Lính đất nung, lính khiên, bọ gai, ma trơi, cóc tía, thạch sư — theo thứ tự mở dần.")]
    public GameObject[] enemies;
    public GameObject[] bosses;

    int floor;
    Rule rule;
    readonly List<Health> alive = new List<Health>();
    ZodiacGameManager gm;
    GameObject choicePanel, bossBar;
    RectTransform bossFill;
    Health bossHealth;
    bool choiceMade, climb;
    float fogStart, fogEnd;
    readonly List<GameObject> ruleObjects = new List<GameObject>();

    public static int CurrentFloor { get; private set; }

    int RuleIndex => (floor - 1) % Rules.Length;
    int Cycle => (floor - 1) / Rules.Length;

    IEnumerator Start()
    {
        gm = ZodiacGameManager.Instance;
        fogStart = RenderSettings.fogStartDistance;
        fogEnd = RenderSettings.fogEndDistance;
        var ph = gm != null && gm.player != null ? gm.player.GetComponent<Health>() : null;
        if (ph != null) ph.Died += OnPlayerDied;
        yield return new WaitForSeconds(1.5f);
        floor = 0;
        while (gm != null && gm.Current == ZodiacGameManager.State.Playing)
        {
            floor++;
            CurrentFloor = floor;
            yield return Floor();
            if (gm.Current != ZodiacGameManager.State.Playing) yield break;
            yield return Choice();
            if (!climb)
            {
                Finish();
                gm.GoToVillage();
                yield break;
            }
        }
    }

    // ───────── Một tầng ─────────

    IEnumerator Floor()
    {
        rule = Rules[RuleIndex];
        if (gm.roomBanner != null)
        {
            gm.roomBanner.text = $"Tầng {floor}\n<size=34>Canh {rule.hour} · {rule.name} — {rule.desc}</size>";
            if (gm.roomBanner.TryGetComponent(out TimedFade fade)) fade.Restart();
        }
        BeginRule();
        yield return new WaitForSeconds(1.6f);

        bool bossFloor = floor % 6 == 0 && bosses != null && bosses.Length > 0;
        if (bossFloor) SpawnBoss();
        int waves = Mathf.Min(4, 2 + floor / 6) - (bossFloor ? 1 : 0);
        for (int w = 0; w < waves; w++)
        {
            SpawnWave(w);
            yield return new WaitForSeconds(1.2f);
            while (AliveCount() > 0 && gm.Current == ZodiacGameManager.State.Playing) yield return null;
            if (gm.Current != ZodiacGameManager.State.Playing) { EndRule(); yield break; }
            yield return new WaitForSeconds(0.6f);
        }
        while (bossHealth != null && !bossHealth.IsDead && gm.Current == ZodiacGameManager.State.Playing) yield return null;
        while (AliveCount() > 0 && gm.Current == ZodiacGameManager.State.Playing) yield return null;
        EndRule();
        if (gm.Current == ZodiacGameManager.State.Playing) Reward();
    }

    int AliveCount()
    {
        alive.RemoveAll(h => h == null || h.IsDead || !h.gameObject.activeInHierarchy);
        return alive.Count;
    }

    float HpScale => (1f + 0.12f * floor) * ZodiacDifficulty.HealthScale * (RuleIndex == 1 ? 1.8f : RuleIndex == 10 ? 0.5f : 1f);
    float DamageScale => (1f + 0.06f * floor) * ZodiacDifficulty.DamageScale;

    void SpawnWave(int wave)
    {
        int pool = floor <= 2 ? 3 : floor <= 5 ? 5 : enemies.Length;
        int count = Mathf.Min(3 + floor / 2, 8) * (RuleIndex == 10 ? 2 : 1);
        bool turretPlaced = false;
        for (int i = 0; i < count; i++)
        {
            var prefab = enemies[Random.Range(0, Mathf.Min(pool, enemies.Length))];
            if (prefab == null) continue;
            bool turret = prefab.name.Contains("ThachSu");
            if (turret && turretPlaced) prefab = enemies[0];
            turretPlaced |= turret;
            bool mini = RuleIndex == 11 && i == 0;
            StartCoroutine(SpawnLater(prefab, i * 0.3f, mini));
        }
    }

    Vector3 SpawnPoint(GameObject prefab)
    {
        Vector3 p = gm.player != null ? gm.player.transform.position : arena.entry;
        bool flyer = prefab.name.Contains("MaTroi");
        if (prefab.name.Contains("ThachSu"))
            return new Vector3(Random.Range(arena.xMin + 3f, arena.xMax - 3f), 0.05f, ZodiacGameManager.LaneMaxZ - 0.5f);
        for (int tries = 0; tries < 8; tries++)
        {
            float x = Random.Range(arena.xMin + 1.2f, arena.xMax - 1.2f);
            if (Mathf.Abs(x - p.x) < 3f) continue;
            return new Vector3(x, flyer ? 2.6f : 0.05f, Random.Range(ZodiacGameManager.LaneMinZ + 0.5f, ZodiacGameManager.LaneMaxZ - 0.5f));
        }
        return new Vector3(p.x > (arena.xMin + arena.xMax) / 2f ? arena.xMin + 1.5f : arena.xMax - 1.5f, flyer ? 2.6f : 0.05f, p.z);
    }

    IEnumerator SpawnLater(GameObject prefab, float delay, bool mini)
    {
        yield return new WaitForSeconds(delay);
        Vector3 at = SpawnPoint(prefab);
        var ground = new Vector3(at.x, 0.02f, at.z);
        ZodiacFx.Ring(ground, 1.2f, new Color(0.35f, 0.2f, 0.4f, 0.8f), 0.5f, flat: true);
        yield return new WaitForSeconds(0.5f);
        if (gm.Current != ZodiacGameManager.State.Playing) yield break;
        var go = Instantiate(prefab, at, Quaternion.identity);
        if (go.TryGetComponent(out EnemyBase e))
        {
            e.alwaysAggro = true;
            e.ApplyScaling(HpScale, DamageScale, 1f + floor * 0.05f);
            if (RuleIndex == 0) e.speedScale *= 1.5f;
            if (mini) MiniBoss.Make(e, prefab, Track);
            else if (RuleIndex == 2 || Random.value < 0.08f + floor * 0.015f)
                EliteAffix.Make(e, EliteAffix.RandomKind(), prefab, Track);
            else if (RuleIndex == 5 && Random.value < 0.5f)
                EliteAffix.Make(e, EliteAffix.Kind.Venom, prefab, Track);
            Track(e.Health);
            if (RuleIndex == 7) e.Health.Died += HealOnKill;
        }
        ZodiacFx.Puff(at + Vector3.up * 0.6f, new Color(0.6f, 0.5f, 0.7f, 0.8f), 1.6f);
    }

    void Track(Health h)
    {
        if (h != null) alive.Add(h);
    }

    void HealOnKill()
    {
        var ph = gm.player != null ? gm.player.GetComponent<Health>() : null;
        if (ph != null) ph.Heal(Mathf.Max(1, ph.maxHealth * 4 / 100));
    }

    void SpawnBoss()
    {
        var prefab = bosses[(floor / 6 - 1) % bosses.Length];
        var at = new Vector3(Mathf.Lerp(arena.xMin, arena.xMax, 0.72f), 0.05f, (ZodiacGameManager.LaneMinZ + ZodiacGameManager.LaneMaxZ) / 2f);
        var go = Instantiate(prefab, at, Quaternion.identity);
        if (!go.TryGetComponent(out BossBase boss)) return;
        boss.alwaysAggro = true;
        boss.ApplyScaling(0.6f + 0.08f * floor, DamageScale, 1f);
        bossHealth = boss.Health;
        Track(bossHealth);
        BuildBossBar(boss.bossName);
        boss.Activate();
        if (arena.bossMusic != null) ZodiacAudio.PlayMusic(arena.bossMusic, 0.6f);
    }

    // ───────── Luật canh giờ ─────────

    void BeginRule()
    {
        switch (RuleIndex)
        {
            case 3:
                RenderSettings.fogStartDistance = 3f;
                RenderSettings.fogEndDistance = 17f;
                break;
            case 4: StartCoroutine(FireRain()); break;
            case 6: StartCoroutine(EnergyFlow()); break;
            case 8:
                for (int k = 0; k < 2; k++) ruleObjects.Add(CreateFallingRock(k));
                break;
            case 9: PlayerEquipment.QuickUseBlocked = true; break;
        }
    }

    void EndRule()
    {
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
        PlayerEquipment.QuickUseBlocked = false;
        foreach (var o in ruleObjects) if (o != null) Destroy(o);
        ruleObjects.Clear();
        ruleToken++;
        if (bossBar != null)
        {
            Destroy(bossBar);
            if (gm != null) ZodiacAudio.PlayMusic(gm.music);
        }
        bossHealth = null;
    }

    /// <summary>Đổi mỗi khi hết luật — các vòng lặp lửa / Khí của luật cũ thấy khác thì tự dừng.</summary>
    int ruleToken;

    IEnumerator FireRain()
    {
        int token = ruleToken;
        yield return new WaitForSeconds(2f);
        while (token == ruleToken && gm.Current == ZodiacGameManager.State.Playing)
        {
            if (gm.player != null)
            {
                Vector3 p = gm.player.transform.position;
                StartCoroutine(Fire(new Vector3(p.x, 0f, p.z), token));
            }
            yield return new WaitForSeconds(Mathf.Max(2.2f, 4f - Cycle * 0.5f));
        }
    }

    /// <summary>Vòng báo 1 giây rồi bùng lửa cháy 4 giây (đứng vào là bỏng).</summary>
    IEnumerator Fire(Vector3 at, int token)
    {
        ZodiacFx.Ring(at + Vector3.up * 0.03f, 2f, new Color(1f, 0.4f, 0.1f, 0.8f), 1f, flat: true);
        yield return new WaitForSeconds(1f);
        if (token != ruleToken) yield break;
        var go = new GameObject("TowerFire");
        go.transform.position = at;
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(1.8f, 0.6f, 1.6f);
        box.center = new Vector3(0f, 0.3f, 0f);
        go.AddComponent<Hazard>().damage = Mathf.Max(1, ZodiacBalance.Unit / 2);
        var glow = ZodiacFx.Shape("Flames", true, at + Vector3.up * 0.04f, new Vector2(1.8f, 1.6f), new Color(1f, 0.4f, 0.1f, 0.85f), 13, go.transform);
        glow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        ZodiacLights.AddPoint(go, new Color(1f, 0.45f, 0.15f), 4f, 2.2f);
        ruleObjects.Add(go);
        for (float t = 0f; t < 4f && token == ruleToken; t += Time.deltaTime)
        {
            if (Random.value < 0.2f) ZodiacParticles.Sparks(at + Vector3.up * 0.2f, new Color(1f, 0.5f, 0.15f), 2);
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    IEnumerator EnergyFlow()
    {
        int token = ruleToken;
        var energy = gm.player != null ? gm.player.GetComponent<PlayerEnergy>() : null;
        while (token == ruleToken && energy != null)
        {
            energy.Gain(5);
            yield return new WaitForSeconds(1f);
        }
    }

    GameObject CreateFallingRock(int k)
    {
        var go = new GameObject("TowerStalactite");
        go.transform.position = new Vector3((arena.xMin + arena.xMax) / 2f, 0f, 0f);
        var trap = go.AddComponent<FallingStalactite>();
        trap.xMin = arena.xMin;
        trap.xMax = arena.xMax;
        trap.alwaysActive = true;
        trap.interval = new Vector2(2.5f + k, 4.5f + k);
        var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(rock.GetComponent<Collider>());
        rock.name = "Rock";
        rock.transform.SetParent(go.transform, false);
        rock.transform.localScale = new Vector3(0.45f, 1.4f, 0.45f);
        rock.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        var holder = new GameObject("RockHolder").transform;
        holder.SetParent(go.transform, false);
        rock.transform.SetParent(holder, false);
        rock.transform.localPosition = Vector3.up * 0.7f;
        trap.rock = holder;
        return go;
    }

    // ───────── Thưởng & lựa chọn ─────────

    static readonly string[] Treasures = { "manh_huyet_dan", "manh_khi_dan", "ho_phach", "bua_ho_phu", "ngoc_lam", "ngoc_do", "bua_bao_ho", "ngoc_may_man" };

    void Reward()
    {
        var p = PlayerProfile.Current;
        int coins = 8 + floor * 4;
        p.AddCoins(coins);
        string extra = "";
        Vector3 at = gm.player != null ? gm.player.transform.position + Vector3.up : arena.entry;
        if (floor % 3 == 0)
        {
            var def = ItemDatabase.Get(Treasures[Random.Range(0, Treasures.Length)]);
            if (def != null)
            {
                LootPickup.Spawn(ItemAffixes.Roll(def), 0, at);
                extra = $" + {def.displayName}";
            }
        }
        var ph = gm.player != null ? gm.player.GetComponent<Health>() : null;
        if (ph != null) ph.Heal(ph.maxHealth / 4);
        bool record = p.RecordTower(floor, gm.player != null && gm.player.Character != null ? gm.player.Character.name : "");
        PickupToast.Show($"Qua tầng {floor}! +{coins} đồng{extra}" + (record ? "  · KỶ LỤC MỚI" : ""), new Color(1f, 0.85f, 0.35f));
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.win, 0.6f, 0f);
    }

    IEnumerator Choice()
    {
        yield return new WaitForSeconds(1.2f);   // để kịp hút đồ rơi
        choiceMade = false;
        BuildChoice();
        Time.timeScale = 0f;
        gm.player.SetControlEnabled(false);
        while (!choiceMade) yield return null;
        Time.timeScale = 1f;
        if (choicePanel != null) Destroy(choicePanel);
        if (gm.player != null) gm.player.SetControlEnabled(true);
    }

    void BuildChoice()
    {
        var canvas = ZodiacUi.Canvas(transform, "TowerChoice", 450);
        choicePanel = canvas.gameObject;
        var next = Rules[floor % Rules.Length];
        var (_, panel) = ZodiacUi.Window(canvas, $"QUA TẦNG {floor}", new Vector2(900f, 520f));
        var mid = new Vector2(0.5f, 0.5f);
        int best = PlayerProfile.Current.towerBest;
        ZodiacUi.MakeText(panel, $"Kỷ lục: tầng {best}\n\nTầng {floor + 1} — Canh {next.hour} · <b>{next.name}</b>\n<size=24>{next.desc}</size>" +
                                 ((floor + 1) % 6 == 0 ? "\n<color=#ff8a7a>Có boss!</color>" : ""),
                          28, ZodiacUi.Ink, mid, new Vector2(0f, 40f), new Vector2(820f, 220f));
        var up = ZodiacUi.MakeButton(panel, $"Lên tầng {floor + 1}", mid, new Vector2(-200f, -170f), new Vector2(340f, 70f), () => { climb = true; choiceMade = true; });
        ZodiacUi.MakeButton(panel, "Rời tháp (về làng)", mid, new Vector2(200f, -170f), new Vector2(340f, 70f), () => { climb = false; choiceMade = true; });
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(up.gameObject);
    }

    void BuildBossBar(string title)
    {
        var canvas = ZodiacUi.Canvas(transform, "TowerBossBar", 90);
        bossBar = canvas.gameObject;
        var back = ZodiacUi.MakeImage(canvas, "Back", new Color(0.15f, 0.1f, 0.1f, 0.85f));
        back.raycastTarget = false;
        ZodiacUi.Place(back.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(900f, 32f));
        var f = ZodiacUi.MakeImage(back.transform, "Fill", new Color(0.55f, 0.85f, 1f));
        f.raycastTarget = false;
        bossFill = f.rectTransform;
        ZodiacUi.Stretch(bossFill);
        bossFill.pivot = new Vector2(0f, 0.5f);
        bossFill.offsetMin = new Vector2(4f, 4f);
        bossFill.offsetMax = new Vector2(-4f, -4f);
        ZodiacUi.MakeText(back.transform, title, 32, Color.white, new Vector2(0.5f, 1f), new Vector2(0f, 24f), new Vector2(900f, 44f));
    }

    void Update()
    {
        if (bossFill != null && bossHealth != null)
            bossFill.localScale = new Vector3(Mathf.Clamp01((float)bossHealth.Current / Mathf.Max(1, bossHealth.maxHealth)), 1f, 1f);
    }

    void OnPlayerDied()
    {
        int reached = Mathf.Max(0, floor - 1);
        var p = PlayerProfile.Current;
        p.RecordTower(reached, gm.player != null && gm.player.Character != null ? gm.player.Character.name : "");
        if (gm.loseDetailText != null) gm.loseDetailText.text = $"Tháp: qua được {reached} tầng · kỷ lục {p.towerBest}";
        Finish();
    }

    void Finish()
    {
        EndRule();
        CurrentFloor = 0;
    }

    void OnDestroy()
    {
        PlayerEquipment.QuickUseBlocked = false;
        if (Time.timeScale == 0f && choicePanel != null) Time.timeScale = 1f;
    }
}
