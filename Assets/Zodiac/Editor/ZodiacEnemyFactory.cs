using UnityEditor;
using UnityEngine;

/// <summary>
/// Tạo prefab quái trong Assets/Zodiac/Prefabs (chưa có sprite thật — ghép từ hình vuông/tròn placeholder):
///   Lính Đất Nung (đi bộ, đuổi, cắn) · Bọ Gai (tuần tra, lưng gai — không đạp được) · Ma Trơi (bay, lao tới)
///   Cóc Tía (nhảy cóc) · Thạch Sư (tượng phun lửa) · Lính Khiên (đỡ đòn phía trước) · Hồ Tinh (boss).
/// Prefab được tạo lại mỗi lần dựng prototype — chỉnh thông số quái ngay trong file này.
/// Thân quái chịu ánh sáng 2D; mắt, lửa, quầng sáng tự phát sáng (thấy rõ trong hang tối).
/// </summary>
public static class ZodiacEnemyFactory
{
    const string PrefabFolder = "Assets/Zodiac/Prefabs";

    public class Prefabs
    {
        public GameObject walker, spiky, flyer, hopper, turret, shield, boss;
    }

    static readonly Color BarBackColor = new Color(0.15f, 0.1f, 0.1f, 0.85f);
    static readonly Color EnemyBarColor = new Color(0.95f, 0.3f, 0.2f);

    static Sprite square, circle;
    static int groundLayer, playerLayer, enemyLayer;
    static PhysicsMaterial2D noFriction;

    public static Prefabs EnsurePrefabs(Sprite squareSprite, Sprite circleSprite, PhysicsMaterial2D material,
                                        int ground, int playerL, int enemy)
    {
        square = squareSprite;
        circle = circleSprite;
        noFriction = material;
        groundLayer = ground;
        playerLayer = playerL;
        enemyLayer = enemy;
        ZodiacPrototypeBuilder.EnsureFolder(PrefabFolder);
        AssetDatabase.DeleteAsset(PrefabFolder + "/Enemy_Placeholder.prefab");   // bản quái cũ (khối tím)

        var p = new Prefabs();
        p.walker = Ensure("Enemy_LinhDatNung", BuildWalker);
        p.spiky = Ensure("Enemy_BoGai", BuildSpiky);
        p.flyer = Ensure("Enemy_MaTroi", BuildFlyer);
        p.hopper = Ensure("Enemy_CocTia", BuildHopper);
        p.turret = Ensure("Enemy_ThachSu", BuildTurret);
        p.shield = Ensure("Enemy_LinhKhien", BuildShieldSoldier);
        p.boss = Ensure("Boss_HoTinh", () => BuildBoss(p.flyer));
        return p;
    }

    static GameObject Ensure(string name, System.Func<GameObject> build)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        var root = build();
        root.name = name;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ───────────────────────────── Từng loại quái ─────────────────────────────

    /// <summary>Lính Đất Nung: tượng gốm đỏ đội mũ trụ, mắt sáng — đi tuần, thấy người thì đuổi, gồng rồi lao cắn.</summary>
    static GameObject BuildWalker()
    {
        var root = Root("Walker", dynamic: true, hp: 3);
        AddBox(root, new Vector2(0.9f, 0.95f), new Vector2(0f, 0.475f));
        var v = Visual(root);
        Color clay = new Color(0.72f, 0.35f, 0.22f);
        Shape(v, "Body", false, new Vector2(0f, 0.45f), new Vector2(0.9f, 0.9f), clay, 8);
        Shape(v, "Belly", false, new Vector2(0.05f, 0.35f), new Vector2(0.6f, 0.45f), new Color(0.85f, 0.55f, 0.38f), 9);
        Shape(v, "Helmet", false, new Vector2(0f, 0.92f), new Vector2(1f, 0.22f), new Color(0.25f, 0.2f, 0.18f), 9);
        Shape(v, "Crest", true, new Vector2(-0.05f, 1.08f), new Vector2(0.25f, 0.25f), new Color(0.85f, 0.15f, 0.12f), 9);
        Eyes(v, new Vector2(0.18f, 0.65f), 0.2f, new Color(1f, 0.9f, 0.5f));
        Shape(v, "Spear", false, new Vector2(0.52f, 0.55f), new Vector2(0.08f, 1.1f), new Color(0.45f, 0.3f, 0.15f), 7);
        Shape(v, "SpearTip", false, new Vector2(0.52f, 1.12f), new Vector2(0.16f, 0.16f), new Color(0.8f, 0.8f, 0.85f), 7, 45f);
        var ai = root.AddComponent<EnemyPatrol>();
        Common(ai, v);
        HealthBar(root, 1.35f);
        Flash(root, v);
        return root;
    }

    /// <summary>
    /// Lính Khiên: lính đất nung to hơn cầm khiên đồng — đỡ mọi đòn phía trước (tia lửa + tiếng "keng", người chơi bật lại).
    /// Cách hạ: vòng ra sau lưng (lăn né qua), chém xuống/đạp từ trên đầu, hoặc làm choáng bằng chiêu để hạ khiên.
    /// Đi chậm, gồng lâu nhưng cắn đau. Tham khảo: lính khiên của Hollow Knight (Husk Guard), Dead Cells.
    /// </summary>
    static GameObject BuildShieldSoldier()
    {
        var root = Root("ShieldSoldier", dynamic: true, hp: 4);
        AddBox(root, new Vector2(1f, 1.2f), new Vector2(0f, 0.6f));
        var v = Visual(root);
        Color clay = new Color(0.6f, 0.3f, 0.22f);
        Shape(v, "Body", false, new Vector2(-0.05f, 0.55f), new Vector2(0.95f, 1.1f), clay, 8);
        Shape(v, "Belt", false, new Vector2(-0.05f, 0.45f), new Vector2(0.95f, 0.12f), new Color(0.3f, 0.2f, 0.15f), 9);
        Shape(v, "Helmet", false, new Vector2(-0.05f, 1.15f), new Vector2(1.05f, 0.25f), new Color(0.75f, 0.6f, 0.25f), 9);
        Shape(v, "Plume", true, new Vector2(-0.15f, 1.35f), new Vector2(0.3f, 0.3f), new Color(0.85f, 0.15f, 0.12f), 9);
        Eyes(v, new Vector2(0.15f, 0.85f), 0.18f, new Color(1f, 0.6f, 0.3f));
        var shield = new GameObject("Shield").transform;
        shield.SetParent(v, false);
        shield.localPosition = new Vector3(0.58f, 0.6f, 0f);
        Shape(shield, "Plate", true, Vector2.zero, new Vector2(0.4f, 1.15f), new Color(0.8f, 0.62f, 0.25f), 11);
        Shape(shield, "Rim", true, Vector2.zero, new Vector2(0.26f, 0.95f), new Color(0.62f, 0.45f, 0.18f), 12);
        Shape(shield, "Boss", true, Vector2.zero, new Vector2(0.16f, 0.16f), new Color(0.95f, 0.85f, 0.45f), 13);
        var ai = root.AddComponent<EnemyPatrol>();
        Common(ai, v);
        ai.guardFront = true;
        ai.shield = shield;
        ai.moveSpeed = 1.4f;
        ai.chaseSpeed = 2.2f;
        ai.windupTime = 0.6f;
        ai.attackDamage = 2;
        ai.attackCooldown = 1.6f;
        ai.knockbackTaken = 0.5f;
        ai.attackBoxOffset = new Vector2(0.85f, 0.5f);
        ai.attackBoxSize = new Vector2(0.9f, 0.8f);
        HealthBar(root, 1.65f);
        Flash(root, v);
        return root;
    }

    /// <summary>Bọ Gai: mai cứng xanh thẫm, lưng chĩa gai trắng — đạp lên là bị đâm (như Spiny của Mario).</summary>
    static GameObject BuildSpiky()
    {
        var root = Root("Spiky", dynamic: true, hp: 3);
        AddBox(root, new Vector2(1f, 0.7f), new Vector2(0f, 0.35f));
        var v = Visual(root);
        Shape(v, "Shell", true, new Vector2(0f, 0.38f), new Vector2(1.1f, 0.8f), new Color(0.15f, 0.3f, 0.35f), 8);
        Shape(v, "Belly", false, new Vector2(0f, 0.12f), new Vector2(1f, 0.2f), new Color(0.75f, 0.65f, 0.35f), 9);
        for (int i = 0; i < 4; i++)
            Shape(v, "Spike", false, new Vector2(-0.36f + i * 0.24f, 0.78f - Mathf.Abs(i - 1.5f) * 0.06f), new Vector2(0.2f, 0.2f),
                  new Color(0.95f, 0.95f, 0.85f), 7, 45f);
        Shape(v, "Head", true, new Vector2(0.48f, 0.3f), new Vector2(0.35f, 0.32f), new Color(0.2f, 0.4f, 0.45f), 9);
        Eyes(v, new Vector2(0.52f, 0.36f), 0.1f, new Color(1f, 0.4f, 0.3f));
        var ai = root.AddComponent<EnemyPatrol>();
        Common(ai, v);
        ai.spiky = true;
        ai.chases = false;
        ai.moveSpeed = 1.5f;
        ai.attackDamage = 0;
        HealthBar(root, 1.1f);
        Flash(root, v);
        return root;
    }

    /// <summary>Ma Trơi: đốm lửa xanh lơ lửng, quầng sáng to dần khi sắp lao tới.</summary>
    static GameObject BuildFlyer()
    {
        var root = Root("Flyer", dynamic: true, hp: 2);
        var rb = root.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        var col = root.AddComponent<CircleCollider2D>();
        col.radius = 0.38f;
        col.sharedMaterial = noFriction;
        col.excludeLayers = 1 << playerLayer;
        var v = Visual(root);
        var glow = Shape(v, "Glow", true, Vector2.zero, new Vector2(1.5f, 1.5f), new Color(0.4f, 0.9f, 1f, 0.35f), 7);
        Shape(v, "Flame", true, new Vector2(-0.12f, 0.12f), new Vector2(0.5f, 0.75f), new Color(0.45f, 0.95f, 0.95f), 8, 20f);
        Shape(v, "Core", true, Vector2.zero, new Vector2(0.75f, 0.75f), new Color(0.75f, 1f, 1f), 9);
        Eyes(v, new Vector2(0.1f, 0.05f), 0.12f, new Color(0.1f, 0.2f, 0.35f));
        foreach (var part in new[] { glow, v.Find("Flame").gameObject, v.Find("Core").gameObject }) Unlit(part);
        ZodiacLights.AddPoint(root, new Color(0.45f, 0.95f, 1f), 3.5f, 1f);
        var ai = root.AddComponent<EnemyFlyer>();
        Common(ai, v);
        ai.glow = glow.GetComponent<SpriteRenderer>();
        ai.stompBounce = 13f;
        HealthBar(root, 0.75f);
        Flash(root, v, except: glow.transform);
        return root;
    }

    /// <summary>Cóc Tía: cóc to bè màu tím, ngồi thụp xuống rồi bật nhảy vòng cung.</summary>
    static GameObject BuildHopper()
    {
        var root = Root("Hopper", dynamic: true, hp: 3);
        AddBox(root, new Vector2(1f, 0.75f), new Vector2(0f, 0.375f));
        var v = Visual(root);
        Shape(v, "Body", true, new Vector2(0f, 0.4f), new Vector2(1.15f, 0.85f), new Color(0.5f, 0.3f, 0.6f), 8);
        Shape(v, "Belly", true, new Vector2(0.1f, 0.3f), new Vector2(0.75f, 0.45f), new Color(0.85f, 0.75f, 0.5f), 9);
        Shape(v, "Spot1", true, new Vector2(-0.25f, 0.6f), new Vector2(0.18f, 0.14f), new Color(0.35f, 0.2f, 0.45f), 9);
        Shape(v, "Spot2", true, new Vector2(-0.4f, 0.4f), new Vector2(0.14f, 0.12f), new Color(0.35f, 0.2f, 0.45f), 9);
        Shape(v, "EyeBumpBack", true, new Vector2(0.05f, 0.82f), new Vector2(0.3f, 0.3f), new Color(0.5f, 0.3f, 0.6f), 8);
        Shape(v, "EyeBumpFront", true, new Vector2(0.35f, 0.82f), new Vector2(0.3f, 0.3f), new Color(0.5f, 0.3f, 0.6f), 8);
        Shape(v, "EyeBack", true, new Vector2(0.08f, 0.85f), new Vector2(0.18f, 0.18f), new Color(1f, 0.85f, 0.2f), 9);
        Shape(v, "EyeFront", true, new Vector2(0.38f, 0.85f), new Vector2(0.18f, 0.18f), new Color(1f, 0.85f, 0.2f), 9);
        Shape(v, "PupilBack", false, new Vector2(0.1f, 0.85f), new Vector2(0.05f, 0.12f), Color.black, 10);
        Shape(v, "PupilFront", false, new Vector2(0.4f, 0.85f), new Vector2(0.05f, 0.12f), Color.black, 10);
        var ai = root.AddComponent<EnemyHopper>();
        Common(ai, v);
        HealthBar(root, 1.25f);
        Flash(root, v);
        return root;
    }

    /// <summary>Thạch Sư: tượng sư tử đá canh đền, miệng nạp lửa rồi phun cầu lửa.</summary>
    static GameObject BuildTurret()
    {
        var root = Root("Turret", dynamic: false, hp: 5);
        AddBox(root, new Vector2(1.2f, 1.5f), new Vector2(0f, 0.75f));
        var v = Visual(root);
        Color stone = new Color(0.55f, 0.55f, 0.6f);
        Shape(v, "Pedestal", false, new Vector2(0f, 0.2f), new Vector2(1.3f, 0.4f), new Color(0.4f, 0.4f, 0.45f), 8);
        Shape(v, "Body", false, new Vector2(-0.1f, 0.75f), new Vector2(0.9f, 0.8f), stone, 8);
        Shape(v, "Mane", true, new Vector2(0.15f, 1.15f), new Vector2(0.95f, 0.85f), new Color(0.42f, 0.4f, 0.48f), 8);
        Shape(v, "Face", true, new Vector2(0.3f, 1.12f), new Vector2(0.6f, 0.55f), new Color(0.65f, 0.65f, 0.7f), 9);
        Eyes(v, new Vector2(0.35f, 1.25f), 0.1f, new Color(1f, 0.5f, 0.2f));
        var mouth = Shape(v, "Mouth", true, new Vector2(0.55f, 1f), new Vector2(0.35f, 0.35f), new Color(0.3f, 0.1f, 0.08f), 10);
        Unlit(mouth);
        ZodiacLights.AddPoint(mouth, new Color(1f, 0.5f, 0.2f), 2.5f, 0.6f);
        var ai = root.AddComponent<EnemyTurret>();
        Common(ai, v);
        ai.mouth = mouth.GetComponent<SpriteRenderer>();
        ai.knockbackTaken = 0f;
        HealthBar(root, 1.85f);
        Flash(root, v, except: mouth.transform);
        return root;
    }

    /// <summary>Hồ Tinh: cáo chín đuôi khổng lồ — đuôi xoè hình quạt, sáng xanh khi sắp ra đòn.</summary>
    static GameObject BuildBoss(GameObject minion)
    {
        var root = Root("Boss", dynamic: true, hp: 30);
        root.GetComponent<Rigidbody2D>().mass = 5f;
        AddBox(root, new Vector2(1.8f, 2f), new Vector2(0f, 1f));
        var v = Visual(root);
        Color fur = new Color(0.95f, 0.55f, 0.2f);

        var tailRoot = new GameObject("Tails").transform;
        tailRoot.SetParent(v, false);
        tailRoot.localPosition = new Vector3(-0.7f, 0.8f, 0f);
        var tails = new SpriteRenderer[9];
        for (int i = 0; i < tails.Length; i++)
        {
            var pivot = new GameObject($"Tail {i + 1}").transform;
            pivot.SetParent(tailRoot, false);
            pivot.localRotation = Quaternion.Euler(0f, 0f, (i - 4) * 14f + 30f);
            var t = Shape(pivot, "Fur", true, new Vector2(-0.9f, 0f), new Vector2(1.9f, 0.5f), new Color(1f, 0.85f, 0.6f), 6);
            Shape(pivot, "Tip", true, new Vector2(-1.7f, 0f), new Vector2(0.45f, 0.4f), Color.white, 6);
            tails[i] = t.GetComponent<SpriteRenderer>();
            Unlit(t);
        }
        ZodiacLights.AddPoint(tailRoot.gameObject, new Color(0.6f, 0.85f, 1f), 5f, 0.7f);
        Shape(v, "Body", true, new Vector2(-0.1f, 0.85f), new Vector2(1.9f, 1.5f), fur, 8);
        Shape(v, "Chest", true, new Vector2(0.4f, 0.9f), new Vector2(0.8f, 1f), new Color(1f, 0.95f, 0.85f), 9);
        Shape(v, "LegBack", false, new Vector2(-0.55f, 0.2f), new Vector2(0.35f, 0.4f), new Color(0.6f, 0.3f, 0.12f), 7);
        Shape(v, "LegFront", false, new Vector2(0.5f, 0.2f), new Vector2(0.35f, 0.4f), new Color(0.6f, 0.3f, 0.12f), 7);
        Shape(v, "Head", true, new Vector2(0.7f, 1.75f), new Vector2(1.1f, 0.95f), fur, 10);
        Shape(v, "Snout", true, new Vector2(1.2f, 1.6f), new Vector2(0.55f, 0.4f), new Color(1f, 0.92f, 0.8f), 11);
        Shape(v, "Nose", true, new Vector2(1.45f, 1.65f), new Vector2(0.15f, 0.12f), new Color(0.15f, 0.1f, 0.1f), 12);
        Shape(v, "EarBack", false, new Vector2(0.4f, 2.2f), new Vector2(0.35f, 0.35f), new Color(0.85f, 0.4f, 0.15f), 9, 45f);
        Shape(v, "EarFront", false, new Vector2(0.85f, 2.25f), new Vector2(0.35f, 0.35f), new Color(0.85f, 0.4f, 0.15f), 9, 45f);
        Unlit(Shape(v, "Eye", false, new Vector2(0.95f, 1.85f), new Vector2(0.22f, 0.08f), new Color(0.3f, 0.95f, 1f), 12, -12f));
        Shape(v, "Mark", true, new Vector2(0.6f, 2.05f), new Vector2(0.14f, 0.14f), new Color(0.8f, 0.1f, 0.15f), 12);

        var ai = root.AddComponent<BossFoxSpirit>();
        Common(ai, v);
        ai.tails = tails;
        ai.minionPrefab = minion;
        ai.knockbackTaken = 0f;
        ai.stunTime = 0f;
        ai.stunMultiplier = 0.35f;
        ai.stompBounce = 14f;
        ai.deathFadeTime = 1.2f;
        Flash(root, v, except: tailRoot);
        return root;
    }

    // ───────────────────────────── Dựng chung ─────────────────────────────

    static GameObject Root(string name, bool dynamic, int hp)
    {
        var root = new GameObject(name) { layer = enemyLayer };
        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = dynamic ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        root.AddComponent<Health>().maxHealth = hp;
        return root;
    }

    static void AddBox(GameObject root, Vector2 size, Vector2 offset)
    {
        var body = root.AddComponent<BoxCollider2D>();
        body.size = size;
        body.offset = offset;
        body.sharedMaterial = noFriction;
        body.excludeLayers = 1 << playerLayer;   // không đẩy/đứng lên người chơi; sát thương chạm do script quét
    }

    static Transform Visual(GameObject root)
    {
        var visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);
        return visual;
    }

    static void Common(EnemyBase ai, Transform visual)
    {
        ai.groundLayer = 1 << groundLayer;
        ai.playerLayer = 1 << playerLayer;
        ai.visual = visual;
    }

    static void Flash(GameObject root, Transform visual, Transform except = null)
    {
        var flash = root.AddComponent<HitFlash>();
        var renderers = new System.Collections.Generic.List<SpriteRenderer>(visual.GetComponentsInChildren<SpriteRenderer>());
        if (except != null) renderers.RemoveAll(r => r.transform.IsChildOf(except));
        flash.renderers = renderers.ToArray();
        flash.flashColor = Color.white;
    }

    static GameObject Shape(Transform parent, string name, bool round, Vector2 localPosition, Vector2 size, Color color,
                            int order, float rotation = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = round ? circle : square;
        sr.color = color;
        sr.sortingOrder = order;
        sr.sharedMaterial = ZodiacVisualSetup.LitMaterial;
        return go;
    }

    static GameObject Unlit(GameObject go)
    {
        go.GetComponent<SpriteRenderer>().sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
        return go;
    }

    /// <summary>Hai mắt tự phát sáng — nhìn thấy quái trong bóng tối.</summary>
    static void Eyes(Transform v, Vector2 front, float size, Color color)
    {
        Unlit(Shape(v, "EyeFront", true, front, Vector2.one * size, color, 10));
        Unlit(Shape(v, "EyeBack", true, front + new Vector2(-size * 1.4f, 0f), Vector2.one * size, color, 10));
    }

    /// <summary>Thanh máu trên đầu: không nằm trong Visual nên không bị lật; ẩn khi máu đầy.</summary>
    static void HealthBar(GameObject root, float height)
    {
        var barRoot = new GameObject("HealthBar").transform;
        barRoot.SetParent(root.transform, false);
        barRoot.localPosition = new Vector3(0f, height, 0f);
        var bar = new GameObject("Bar").transform;
        bar.SetParent(barRoot, false);
        ZodiacPrototypeBuilder.CreateDecor("Back", bar, square, Vector2.zero, new Vector2(1f, 0.14f), BarBackColor, 20);
        var fillPivot = new GameObject("FillPivot").transform;
        fillPivot.SetParent(bar, false);
        fillPivot.localPosition = new Vector3(-0.5f, 0f, 0f);
        ZodiacPrototypeBuilder.CreateDecor("Fill", fillPivot, square, new Vector2(0.5f, 0f), new Vector2(1f, 0.1f), EnemyBarColor, 21);
        var healthBar = barRoot.gameObject.AddComponent<HealthBar>();
        healthBar.health = root.GetComponent<Health>();
        healthBar.fill = fillPivot;
        healthBar.hideWhenFull = true;
        healthBar.visualRoot = bar.gameObject;
        foreach (var sr in bar.GetComponentsInChildren<SpriteRenderer>(true)) sr.sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
    }
}
