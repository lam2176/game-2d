using System;
using UnityEngine;

/// <summary>Một dòng bảng rơi đồ: vật phẩm, tỉ lệ rơi (0..1), số lượng.</summary>
[Serializable]
public class LootEntry
{
    public ItemDefinition item;
    [Range(0f, 1f)] public float chance = 0.3f;
    [Min(1)] public int min = 1;
    [Min(1)] public int max = 1;
}

/// <summary>
/// Đồ rơi trên đất (quái chết, lính đánh rơi khiên/giáo): nảy ra rồi nằm nhấp nhô, có cột sáng màu theo độ hiếm,
/// người chơi lại gần thì bị hút về và cất vào túi (<see cref="PlayerProfile"/>). Túi đầy thì nằm lại chờ.
/// Đồng xu rơi thì cộng thẳng vào tiền.
/// </summary>
public class LootPickup : MonoBehaviour
{
    /// <summary>Món đồ (đã tung dòng phụ) — null khi là đồng xu.</summary>
    public ItemStack stack;
    ItemDefinition item;
    [Tooltip("Số đồng (khi là đồng xu rơi).")]
    public int coins;

    const float MagnetRadius = 1.8f;
    const float PickRadius = 0.6f;

    Vector3 velocity;
    bool resting, attracted;
    float age, bobPhase, retryAt;
    Transform visual;

    /// <summary>Rải đồ từ bảng rơi tại vị trí (quái chết).</summary>
    /// <param name="scale">Nhân tỉ lệ rơi đồ và số xu (độ khó, quái tinh anh).</param>
    public static void DropFrom(LootEntry[] table, Vector2Int coinRange, Vector3 position, float scale = 1f)
    {
        if (table != null)
            foreach (var e in table)
            {
                if (e == null || e.item == null || UnityEngine.Random.value > e.chance * scale) continue;
                int n = UnityEngine.Random.Range(e.min, e.max + 1);
                if (e.item.Stackable) Spawn(ItemAffixes.Roll(e.item, n), 0, position);
                else for (int k = 0; k < n; k++) Spawn(ItemAffixes.Roll(e.item), 0, position);   // trang bị: mỗi món một bộ dòng phụ
            }
        int c = coinRange.y > 0 ? Mathf.RoundToInt(UnityEngine.Random.Range(coinRange.x, coinRange.y + 1) * scale) : 0;
        for (int i = 0; i < c; i++) Spawn(null, 1, position);
    }

    /// <summary>Tạo một món rơi (stack = null và coins &gt; 0 → đồng xu).</summary>
    public static LootPickup Spawn(ItemStack stack, int coins, Vector3 position)
    {
        var go = new GameObject(stack != null ? "Loot " + stack.DisplayName : "Loot Coin");
        go.transform.position = position + Vector3.up * 0.6f;
        var p = go.AddComponent<LootPickup>();
        p.stack = stack;
        p.item = stack?.Def;
        p.coins = coins;
        float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        p.velocity = new Vector3(Mathf.Cos(a) * 1.8f, UnityEngine.Random.Range(5f, 7f), Mathf.Sin(a) * 1.2f);
        p.bobPhase = UnityEngine.Random.value * 6f;
        p.Build();
        return p;
    }

    void Build()
    {
        var tilt = new GameObject("Tilt").transform;
        tilt.SetParent(transform, false);
        tilt.gameObject.AddComponent<Billboard>().full = true;
        visual = tilt;
        Color rarity = item != null ? ItemDefinition.RarityColor(item.rarity) : new Color(1f, 0.85f, 0.3f);
        if (item != null)
        {
            // Quầng nền màu độ hiếm + biểu tượng
            var halo = ZodiacFx.Shape("Halo", true, transform.position, Vector2.one * 0.75f, new Color(rarity.r, rarity.g, rarity.b, 0.35f), 19, tilt);
            halo.sprite = ZodiacFx.SoftCircle;
            halo.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            var icon = ZodiacFx.Shape("Icon", false, transform.position, Vector2.one, Color.white, 20, tilt);
            icon.sprite = item.icon != null ? item.icon : ZodiacFx.Circle;
            if (item.icon == null) icon.color = rarity;
            Vector2 size = icon.sprite.bounds.size;
            float s = 0.55f / Mathf.Max(0.01f, Mathf.Max(size.x, size.y));
            icon.transform.localScale = Vector3.one * s;
            icon.transform.localPosition = Vector3.zero;
            // Cột sáng mảnh từ đất lên — thấy đồ từ xa, màu theo độ hiếm
            if (item.rarity >= ItemRarity.Good)
            {
                var beam = ZodiacFx.Shape("Beam", false, transform.position, new Vector2(0.06f, 2.2f), new Color(rarity.r, rarity.g, rarity.b, 0.45f), 18, tilt);
                beam.transform.localPosition = new Vector3(0f, 0.9f, 0.02f);
            }
            ZodiacLights.AddPoint(gameObject, rarity, 2f, 0.8f);
        }
        else
        {
            var coin = ZodiacFx.Shape("Coin", true, transform.position, Vector2.one * 0.3f, new Color(1f, 0.82f, 0.2f), 20, tilt);
            coin.transform.localPosition = Vector3.zero;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        var player = ZodiacPlayerController.Current;
        Vector3 target = player != null ? player.BodyCenter : transform.position;
        float dist = player != null ? Vector3.Distance(target, transform.position) : float.MaxValue;
        float magnet = MagnetRadius + (player != null && player.Character != null ? player.Character.coinMagnetRadius * (item == null ? 1f : 0.5f) : 0f);
        if (age > 0.5f && Time.time >= retryAt && player != null && player.ControlEnabled && dist <= magnet) attracted = true;

        if (attracted)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, (6f + age * 2f) * dt);
            if (dist <= PickRadius) Collect();
            return;
        }
        if (!resting)
        {
            velocity.y -= 22f * dt;
            transform.position += velocity * dt;
            float floor = 0.45f;
            if (transform.position.y <= floor && velocity.y < 0f)
            {
                transform.position = new Vector3(transform.position.x, floor, transform.position.z);
                if (velocity.y < -3f) velocity = new Vector3(velocity.x * 0.4f, -velocity.y * 0.35f, velocity.z * 0.4f);
                else resting = true;
            }
            KeepInRoom();
        }
        else if (visual != null)
        {
            visual.localPosition = Vector3.up * (Mathf.Sin(Time.time * 3f + bobPhase) * 0.08f);
        }
    }

    void KeepInRoom()
    {
        var p = transform.position;
        p.x = Mathf.Clamp(p.x, ZodiacGameManager.RoomMinX + 0.5f, ZodiacGameManager.RoomMaxX - 0.5f);
        p.z = Mathf.Clamp(p.z, ZodiacGameManager.LaneMinZ + 0.3f, ZodiacGameManager.LaneMaxZ - 0.3f);
        transform.position = p;
    }

    void Collect()
    {
        var profile = PlayerProfile.Current;
        var res = ZodiacResources.Get;
        if (item == null)
        {
            profile.AddCoins(coins);
            if (res != null) ZodiacAudio.Play(res.coin, 0.45f, 0.08f);
            ZodiacParticles.Sparkle(transform.position, new Color(1f, 0.85f, 0.3f), 5, 0.2f);
            Destroy(gameObject);
            return;
        }
        int left = profile.Add(stack);
        if (left > 0)
        {
            // Túi đầy: rơi lại xuống, thử lại sau
            stack.count = left;
            attracted = false;
            resting = false;
            velocity = new Vector3(0f, 4f, 0f);
            retryAt = Time.time + 3f;
            PickupToast.Show("Túi đầy!", new Color(1f, 0.5f, 0.4f));
            return;
        }
        if (res != null) ZodiacAudio.Play(res.lantern, 0.5f, 0.05f);
        ZodiacParticles.Sparkle(transform.position, ItemDefinition.RarityColor(item.rarity), 10, 0.3f);
        Destroy(gameObject);
    }
}
