using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Đếm đòn liên hoàn (mọi đòn trúng quái, cách nhau không quá <see cref="Window"/> giây; trúng đòn thì mất chuỗi)
/// hiện ở mép phải màn hình, và chấm điểm từng khu khi dọn xong: S / A / B / C theo thời gian, chuỗi dài nhất, máu mất.
/// Đạt S thưởng thêm xu và một món hiếm, A thưởng xu. Tự tạo lúc cần (không phải đặt vào scene).
/// </summary>
public class ComboCounter : MonoBehaviour
{
    const float Window = 2.5f;
    static readonly string[] RankNames = { "S", "A", "B", "C" };
    static readonly Color[] RankColors =
    {
        new Color(1f, 0.82f, 0.25f), new Color(0.55f, 0.9f, 1f), new Color(0.62f, 0.9f, 0.6f), new Color(0.8f, 0.72f, 0.6f),
    };
    static readonly string[] SRewards = { "manh_huyet_dan", "manh_khi_dan", "ngoc_may_man", "binh_mau_lon" };

    static ComboCounter instance;

    Text countText, labelText, rankText, rankDetail;
    GameObject comboGroup, rankGroup;
    int count;
    float lastHit, pop, rankUntil;
    Health playerHealth;

    // Khu đang chấm
    bool roomActive;
    float roomStart;
    int roomBestCombo, roomDamage, roomEnemies;

    /// <summary>Chuỗi hiện tại.</summary>
    public static int Count => instance != null ? instance.count : 0;

    static ComboCounter Get
    {
        get
        {
            if (instance != null) return instance;
            if (ZodiacPlayerController.Current == null) return null;
            instance = new GameObject("ComboCounter").AddComponent<ComboCounter>();
            return instance;
        }
    }

    /// <summary>Một đòn vừa trúng quái.</summary>
    public static void Register() => Get?.OnHit();

    /// <summary>Bắt đầu chấm điểm khu (số quái của khu để tính thời gian chuẩn).</summary>
    public static void BeginRoom(int enemies)
    {
        var c = Get;
        if (c == null) return;
        c.roomActive = true;
        c.roomStart = Time.time;
        c.roomBestCombo = c.count;
        c.roomDamage = 0;
        c.roomEnemies = Mathf.Max(1, enemies);
    }

    /// <summary>Khu vừa dọn xong: chấm hạng, hiện chữ lớn và thưởng.</summary>
    public static void EndRoom(Vector3 rewardAt)
    {
        var c = instance;
        if (c == null || !c.roomActive) return;
        c.roomActive = false;
        c.ShowRank(rewardAt);
    }

    void Awake()
    {
        var canvas = ZodiacUi.Canvas(transform, "ComboCanvas", 30);
        canvas.GetComponent<GraphicRaycaster>().enabled = false;

        comboGroup = new GameObject("Combo", typeof(RectTransform));
        comboGroup.transform.SetParent(canvas, false);
        ZodiacUi.Place((RectTransform)comboGroup.transform, new Vector2(1f, 0.5f), new Vector2(-170f, 120f), new Vector2(300f, 160f));
        countText = ZodiacUi.MakeText(comboGroup.transform, "", 84, ZodiacUi.Gold, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(300f, 100f));
        countText.fontStyle = FontStyle.Bold;
        labelText = ZodiacUi.MakeText(comboGroup.transform, "ĐÒN LIÊN HOÀN", 26, ZodiacUi.Ink, new Vector2(0.5f, 0.5f), new Vector2(0f, -45f), new Vector2(300f, 36f));
        comboGroup.SetActive(false);

        rankGroup = new GameObject("Rank", typeof(RectTransform));
        rankGroup.transform.SetParent(canvas, false);
        ZodiacUi.Place((RectTransform)rankGroup.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(900f, 260f));
        rankText = ZodiacUi.MakeText(rankGroup.transform, "", 150, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(400f, 180f));
        rankText.fontStyle = FontStyle.Bold;
        rankDetail = ZodiacUi.MakeText(rankGroup.transform, "", 28, ZodiacUi.Ink, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(900f, 80f));
        rankGroup.SetActive(false);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (playerHealth != null) playerHealth.Damaged -= OnPlayerHurt;
    }

    void Update()
    {
        var player = ZodiacPlayerController.Current;
        if (player != null && playerHealth == null)
        {
            playerHealth = player.GetComponent<Health>();
            playerHealth.Damaged += OnPlayerHurt;
        }
        if (count > 0 && Time.time > lastHit + Window) count = 0;
        bool show = count >= 3;
        if (comboGroup.activeSelf != show) comboGroup.SetActive(show);
        if (show)
        {
            pop = Mathf.MoveTowards(pop, 0f, Time.unscaledDeltaTime * 4f);
            comboGroup.transform.localScale = Vector3.one * (1f + pop * 0.35f);
            float left = 1f - (Time.time - lastHit) / Window;
            countText.color = Color.Lerp(ZodiacUi.Muted, count >= 20 ? new Color(1f, 0.45f, 0.25f) : ZodiacUi.Gold, left);
        }
        if (rankGroup.activeSelf && Time.unscaledTime > rankUntil) rankGroup.SetActive(false);
        else if (rankGroup.activeSelf)
        {
            float t = Mathf.Clamp01((rankUntil - Time.unscaledTime) / 0.4f);
            rankGroup.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.9f, 1f - t);
        }
    }

    void OnHit()
    {
        count = Time.time > lastHit + Window ? 1 : count + 1;
        lastHit = Time.time;
        pop = 1f;
        countText.text = count.ToString();
        if (roomActive) roomBestCombo = Mathf.Max(roomBestCombo, count);
    }

    void OnPlayerHurt(Hit hit)
    {
        count = 0;
        if (roomActive) roomDamage += hit.amount;
    }

    void ShowRank(Vector3 rewardAt)
    {
        float time = Time.time - roomStart;
        float par = 6f + 3.5f * roomEnemies;
        int maxHp = playerHealth != null ? playerHealth.maxHealth : 100;
        float hurt = (float)roomDamage / maxHp;
        float score = 40f * Mathf.Clamp01(par / Mathf.Max(1f, time))
                    + 35f * (1f - Mathf.Clamp01(hurt * 2f))
                    + 25f * Mathf.Clamp01(roomBestCombo / (roomEnemies * 2.5f));
        int rank = score >= 85f ? 0 : score >= 70f ? 1 : score >= 50f ? 2 : 3;
        LastRank = RankNames[rank];

        string reward = "";
        if (rank <= 1)
        {
            int coins = rank == 0 ? 10 : 5;
            for (int i = 0; i < coins; i++) LootPickup.Spawn(null, 1, rewardAt);
            reward = $" · thưởng {coins} xu";
            if (rank == 0)
            {
                var def = ItemDatabase.Get(SRewards[Random.Range(0, SRewards.Length)]);
                if (def != null)
                {
                    LootPickup.Spawn(ItemAffixes.Roll(def), 0, rewardAt);
                    reward += $" + {def.displayName}";
                }
            }
        }
        rankText.text = RankNames[rank];
        rankText.color = RankColors[rank];
        rankDetail.text = $"{Mathf.RoundToInt(time)} giây · chuỗi {roomBestCombo} đòn · mất {Mathf.RoundToInt(hurt * 100f)}% máu{reward}";
        rankGroup.SetActive(true);
        rankGroup.transform.localScale = Vector3.one;
        rankUntil = Time.unscaledTime + 2.6f;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(rank == 0 ? res.lantern : res.coin, 0.8f, 0f);
    }

    /// <summary>Hạng khu vừa chấm gần nhất (cho kiểm thử).</summary>
    public static string LastRank { get; private set; } = "";
}
