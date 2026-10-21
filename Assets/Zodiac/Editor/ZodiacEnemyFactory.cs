using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Tạo prefab quái trong Assets/Zodiac/Prefabs. Có ảnh vẽ trong Assets/Zodiac/Art/Enemies (<see cref="ZodiacEnemyArt"/>) thì dùng ảnh,
/// thiếu ảnh thì ghép tạm từ hình vuông/tròn placeholder:
///   Lính Đất Nung (xáp lại, gồng, lao đánh) · Lính Khiên (đỡ đòn phía trước) · Bọ Gai (lăn dọc làn)
///   Ma Trơi (bay, lao chéo xuống) · Cóc Tía (nhảy đè, sóng chấn) · Thạch Sư (tượng phun lửa) · Hồ Tinh (boss).
/// Prefab được tạo lại mỗi lần dựng prototype — chỉnh thông số quái ngay trong file này.
///
/// Cấu trúc (thế giới 3D, sprite đứng): Root (Rigidbody + collider có bề dày theo làn) → Tilt (ngả theo góc cúi camera)
/// → Visual (EnemyBase.visual, lật trái/phải) → các hình. Các lớp hình xếp lệch nhau theo Z (lớp trên gần camera hơn)
/// để không nhấp nháy khi trùng mặt phẳng. Thân quái nhận đèn và đổ bóng (Sprite Lit 3D); mắt, lửa, quầng sáng tự phát sáng.
/// Mỗi quái có bóng tròn dưới chân và thanh máu luôn quay về camera.
/// </summary>
public static class ZodiacEnemyFactory
{
    const string PrefabFolder = "Assets/Zodiac/Prefabs";
    /// <summary>Khoảng cách Z giữa hai lớp sortingOrder liền nhau (unit).</summary>
    const float LayerDepth = 0.004f;

    public class Prefabs
    {
        public GameObject walker, shield, roller, flyer, hopper, turret, boss;
    }

    static readonly Color BarBackColor = new Color(0.15f, 0.1f, 0.1f, 0.85f);
    static readonly Color EnemyBarColor = new Color(0.95f, 0.3f, 0.2f);

    static Sprite square, circle;
    static int groundLayer, playerLayer, enemyLayer;
    static PhysicsMaterial noFriction;

    public static Prefabs EnsurePrefabs(Sprite squareSprite, Sprite circleSprite, PhysicsMaterial material,
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
        AssetDatabase.DeleteAsset(PrefabFolder + "/Enemy_BoGai.prefab");         // Bọ Gai bản platformer (tuần tra)

        var p = new Prefabs();
        p.walker = Ensure("Enemy_LinhDatNung", BuildWalker);
        p.shield = Ensure("Enemy_LinhKhien", BuildShieldSoldier);
        p.roller = Ensure("Enemy_BoGaiLan", BuildRoller);
        p.flyer = Ensure("Enemy_MaTroi", BuildFlyer);
        p.hopper = Ensure("Enemy_CocTia", BuildHopper);
        p.turret = Ensure("Enemy_ThachSu", BuildTurret);
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

    /// <summary>Lính Đất Nung: tượng gốm đỏ đội mũ trụ cầm giáo, mắt sáng — xáp lại, gồng rồi lao đâm.</summary>
    static GameObject BuildWalker()
    {
        var root = Root("Walker", kinematic: false, hp: 3);
        AddBox(root, new Vector3(0.9f, 1.2f, 0.7f));
        var v = Visual(root);
        Color clay = new Color(0.72f, 0.35f, 0.22f);
        Transform spear;
        if (Art(v, "LinhDatNung", 1.6f, Vector2.zero, 8) != null)
        {
            spear = new GameObject("Spear").transform;
            spear.SetParent(v, false);
            Art(spear, "Giao", 1.75f, new Vector2(0.45f, 0.85f), 12, -8f);
        }
        else
        {
        Shape(v, "Body", false, new Vector2(0f, 0.55f), new Vector2(0.9f, 1.1f), clay, 8);
        Shape(v, "Belly", false, new Vector2(0.05f, 0.45f), new Vector2(0.6f, 0.5f), new Color(0.85f, 0.55f, 0.38f), 9);
        Shape(v, "LegBack", false, new Vector2(-0.2f, 0.08f), new Vector2(0.25f, 0.2f), new Color(0.4f, 0.22f, 0.15f), 7);
        Shape(v, "LegFront", false, new Vector2(0.2f, 0.08f), new Vector2(0.25f, 0.2f), new Color(0.4f, 0.22f, 0.15f), 9);
        Shape(v, "Helmet", false, new Vector2(0f, 1.12f), new Vector2(1f, 0.24f), new Color(0.25f, 0.2f, 0.18f), 9);
        Shape(v, "Crest", true, new Vector2(-0.05f, 1.3f), new Vector2(0.25f, 0.25f), new Color(0.85f, 0.15f, 0.12f), 9);
        Eyes(v, new Vector2(0.18f, 0.85f), 0.2f, new Color(1f, 0.9f, 0.5f));
        spear = new GameObject("Spear").transform;
        spear.SetParent(v, false);
        Shape(spear, "Shaft", false, new Vector2(0.55f, 0.7f), new Vector2(0.08f, 1.3f), new Color(0.45f, 0.3f, 0.15f), 10);
        Shape(spear, "Tip", false, new Vector2(0.55f, 1.38f), new Vector2(0.16f, 0.16f), new Color(0.8f, 0.8f, 0.85f), 10, 45f);
        }
        var ai = root.AddComponent<EnemyBrawler>();
        Common(ai, v, clay);
        ai.gear = spear;   // còn nửa máu thì giáo văng → đánh tay không
        Finish(root, v, 1.8f, 1.1f);
        return root;
    }

    /// <summary>
    /// Lính Khiên: lính đất nung to hơn cầm khiên đồng — đỡ mọi đòn phía trước (tia lửa + tiếng "keng", người chơi bật lại).
    /// Cách hạ: đổi làn vòng ra sau lưng, đá bay từ trên xuống, hoặc làm choáng bằng chiêu để hạ khiên.
    /// Đi chậm, gồng lâu nhưng đánh đau (quật ngã).
    /// </summary>
    static GameObject BuildShieldSoldier()
    {
        var root = Root("ShieldSoldier", kinematic: false, hp: 4);
        AddBox(root, new Vector3(1f, 1.4f, 0.8f));
        var v = Visual(root);
        Color clay = new Color(0.6f, 0.3f, 0.22f);
        Transform shield;
        if (Art(v, "LinhKhien", 1.9f, Vector2.zero, 8) != null)
        {
            shield = new GameObject("Shield").transform;
            shield.SetParent(v, false);
            shield.localPosition = new Vector3(0.55f, 0.85f, -14 * LayerDepth);
            Art(shield, "Khien", 1.05f, Vector2.zero, 0, 0f, true, 0.55f);   // khiên tròn nhìn nghiêng → dẹt ngang
        }
        else
        {
        Shape(v, "Body", false, new Vector2(-0.05f, 0.65f), new Vector2(0.95f, 1.3f), clay, 8);
        Shape(v, "Belt", false, new Vector2(-0.05f, 0.55f), new Vector2(0.95f, 0.12f), new Color(0.3f, 0.2f, 0.15f), 9);
        Shape(v, "Helmet", false, new Vector2(-0.05f, 1.35f), new Vector2(1.05f, 0.27f), new Color(0.75f, 0.6f, 0.25f), 9);
        Shape(v, "Plume", true, new Vector2(-0.15f, 1.55f), new Vector2(0.3f, 0.3f), new Color(0.85f, 0.15f, 0.12f), 9);
        Eyes(v, new Vector2(0.15f, 1.05f), 0.18f, new Color(1f, 0.6f, 0.3f));
        shield = new GameObject("Shield").transform;
        shield.SetParent(v, false);
        shield.localPosition = new Vector3(0.58f, 0.7f, -12 * LayerDepth);
        Shape(shield, "Plate", true, Vector2.zero, new Vector2(0.45f, 1.25f), new Color(0.8f, 0.62f, 0.25f), 0);
        Shape(shield, "Rim", true, Vector2.zero, new Vector2(0.3f, 1.05f), new Color(0.62f, 0.45f, 0.18f), 1);
        Shape(shield, "Boss", true, Vector2.zero, new Vector2(0.16f, 0.16f), new Color(0.95f, 0.85f, 0.45f), 2);
        }
        var ai = root.AddComponent<EnemyBrawler>();
        Common(ai, v, clay);
        ai.guardFront = true;
        ai.shield = shield;
        ai.gear = shield;
        ai.gearDurability = 4;   // đỡ 4 đòn (hoặc còn nửa máu) thì khiên vỡ
        ai.moveSpeed = 1.9f;
        ai.windupTime = 0.65f;
        ai.attackDamage = 2 * ZodiacBalance.Unit;
        ai.attackKnockdown = true;
        ai.attackCooldown = 1.8f;
        ai.knockbackTaken = 0.5f;
        ai.attackBoxOffset = new Vector3(0.85f, 0.7f, 0f);
        ai.attackBoxSize = new Vector3(1f, 1f, 1f);
        Finish(root, v, 2.1f, 1.2f);
        return root;
    }

    /// <summary>Bọ Gai: mai cứng xanh thẫm chĩa gai trắng — cuộn tròn lăn dọc làn đâm người.</summary>
    static GameObject BuildRoller()
    {
        var root = Root("Roller", kinematic: false, hp: 3);
        AddBox(root, new Vector3(1f, 0.75f, 0.8f));
        var v = Visual(root);
        if (Art(v, "BoGai", 1.05f, Vector2.zero, 8) == null)
        {
        Shape(v, "Shell", true, new Vector2(0f, 0.42f), new Vector2(1.1f, 0.85f), new Color(0.15f, 0.3f, 0.35f), 8);
        Shape(v, "Belly", false, new Vector2(0f, 0.12f), new Vector2(1f, 0.2f), new Color(0.75f, 0.65f, 0.35f), 9);
        for (int i = 0; i < 4; i++)
            Shape(v, "Spike", false, new Vector2(-0.36f + i * 0.24f, 0.85f - Mathf.Abs(i - 1.5f) * 0.06f), new Vector2(0.2f, 0.2f),
                  new Color(0.95f, 0.95f, 0.85f), 7, 45f);
        Shape(v, "Head", true, new Vector2(0.48f, 0.32f), new Vector2(0.35f, 0.32f), new Color(0.2f, 0.4f, 0.45f), 9);
        Eyes(v, new Vector2(0.52f, 0.38f), 0.1f, new Color(1f, 0.4f, 0.3f));
        }
        var ai = root.AddComponent<EnemyRoller>();
        Common(ai, v, new Color(0.15f, 0.3f, 0.35f));
        Finish(root, v, 1.25f, 1.1f);
        return root;
    }

    /// <summary>Ma Trơi: đốm lửa xanh lơ lửng ngang tầm đầu, quầng sáng to dần khi sắp lao xuống.</summary>
    static GameObject BuildFlyer()
    {
        var root = Root("Flyer", kinematic: false, hp: 2);
        var col = root.AddComponent<SphereCollider>();
        col.radius = 0.4f;
        col.center = Vector3.zero;
        col.sharedMaterial = noFriction;
        col.excludeLayers = 1 << playerLayer;
        var v = Visual(root);
        var glow = Shape(v, "Glow", true, Vector2.zero, new Vector2(1.5f, 1.5f), new Color(0.4f, 0.9f, 1f, 0.35f), 7);
        Unlit(glow);
        if (Art(v, "MaTroi", 1.3f, Vector2.zero, 9, 0f, lit: false) == null)
        {
            Shape(v, "Flame", true, new Vector2(-0.12f, 0.12f), new Vector2(0.5f, 0.75f), new Color(0.45f, 0.95f, 0.95f), 8, 20f);
            Shape(v, "Core", true, Vector2.zero, new Vector2(0.75f, 0.75f), new Color(0.75f, 1f, 1f), 9);
            Eyes(v, new Vector2(0.1f, 0.05f), 0.12f, new Color(0.1f, 0.2f, 0.35f));
            foreach (var part in new[] { v.Find("Flame").gameObject, v.Find("Core").gameObject }) Unlit(part);
        }
        ZodiacLights.AddPoint(root, new Color(0.45f, 0.95f, 1f), 4.5f, 2f);
        var ai = root.AddComponent<EnemyFlyer>();
        Common(ai, v, new Color(0.45f, 0.95f, 0.95f));
        ai.glow = glow.GetComponent<SpriteRenderer>();
        HealthBar(root, 0.8f);
        Shadow(root, 0.8f);
        Flash(root, v, except: glow.transform);
        return root;
    }

    /// <summary>Cóc Tía: cóc to bè màu tím, ngồi thụp xuống rồi bật nhảy đè lên chỗ người chơi đứng.</summary>
    static GameObject BuildHopper()
    {
        var root = Root("Hopper", kinematic: false, hp: 3);
        AddBox(root, new Vector3(1.1f, 0.85f, 0.8f));
        var v = Visual(root);
        if (Art(v, "CocTia", 1.15f, Vector2.zero, 8) == null)
        {
        Shape(v, "Body", true, new Vector2(0f, 0.45f), new Vector2(1.2f, 0.9f), new Color(0.5f, 0.3f, 0.6f), 8);
        Shape(v, "Belly", true, new Vector2(0.1f, 0.33f), new Vector2(0.78f, 0.48f), new Color(0.85f, 0.75f, 0.5f), 9);
        Shape(v, "Spot1", true, new Vector2(-0.25f, 0.66f), new Vector2(0.18f, 0.14f), new Color(0.35f, 0.2f, 0.45f), 9);
        Shape(v, "Spot2", true, new Vector2(-0.42f, 0.44f), new Vector2(0.14f, 0.12f), new Color(0.35f, 0.2f, 0.45f), 9);
        Shape(v, "EyeBumpBack", true, new Vector2(0.05f, 0.9f), new Vector2(0.3f, 0.3f), new Color(0.5f, 0.3f, 0.6f), 8);
        Shape(v, "EyeBumpFront", true, new Vector2(0.35f, 0.9f), new Vector2(0.3f, 0.3f), new Color(0.5f, 0.3f, 0.6f), 8);
        Shape(v, "EyeBack", true, new Vector2(0.08f, 0.93f), new Vector2(0.18f, 0.18f), new Color(1f, 0.85f, 0.2f), 9);
        Shape(v, "EyeFront", true, new Vector2(0.38f, 0.93f), new Vector2(0.18f, 0.18f), new Color(1f, 0.85f, 0.2f), 9);
        Shape(v, "PupilBack", false, new Vector2(0.1f, 0.93f), new Vector2(0.05f, 0.12f), Color.black, 10);
        Shape(v, "PupilFront", false, new Vector2(0.4f, 0.93f), new Vector2(0.05f, 0.12f), Color.black, 10);
        }
        var ai = root.AddComponent<EnemyHopper>();
        Common(ai, v, new Color(0.5f, 0.3f, 0.6f));
        Finish(root, v, 1.35f, 1.3f);
        return root;
    }

    /// <summary>Thạch Sư: tượng sư tử đá canh đền, miệng nạp lửa rồi phun cầu lửa quét ngang các làn.</summary>
    static GameObject BuildTurret()
    {
        var root = Root("Turret", kinematic: true, hp: 5);
        AddBox(root, new Vector3(1.2f, 1.6f, 1f));
        var v = Visual(root);
        Color stone = new Color(0.55f, 0.55f, 0.6f);
        GameObject mouth;
        var lion = Art(v, "ThachSu", 2f, Vector2.zero, 8);
        if (lion != null)
        {
            // Miệng tượng (góc trên bên phải của ảnh) — đốm lửa sáng dần khi nạp
            Vector2 size = lion.transform.localScale.x * (Vector2)lion.sprite.bounds.size;
            mouth = Shape(v, "Mouth", true, new Vector2(size.x * 0.33f, size.y * 0.72f), new Vector2(0.3f, 0.3f), new Color(1f, 0.45f, 0.1f, 0.6f), 12);
        }
        else
        {
        Shape(v, "Pedestal", false, new Vector2(0f, 0.2f), new Vector2(1.3f, 0.4f), new Color(0.4f, 0.4f, 0.45f), 8);
        Shape(v, "Body", false, new Vector2(-0.1f, 0.75f), new Vector2(0.9f, 0.8f), stone, 8);
        Shape(v, "Mane", true, new Vector2(0.15f, 1.15f), new Vector2(0.95f, 0.85f), new Color(0.42f, 0.4f, 0.48f), 9);
        Shape(v, "Face", true, new Vector2(0.3f, 1.12f), new Vector2(0.6f, 0.55f), new Color(0.65f, 0.65f, 0.7f), 10);
        Eyes(v, new Vector2(0.35f, 1.25f), 0.1f, new Color(1f, 0.5f, 0.2f));
        mouth = Shape(v, "Mouth", true, new Vector2(0.55f, 1f), new Vector2(0.35f, 0.35f), new Color(0.3f, 0.1f, 0.08f), 12);
        }
        Unlit(mouth);
        ZodiacLights.AddPoint(mouth, new Color(1f, 0.5f, 0.2f), 3f, 1.2f);
        var ai = root.AddComponent<EnemyTurret>();
        Common(ai, v, stone);
        ai.mouth = mouth.GetComponent<SpriteRenderer>();
        ai.knockbackTaken = 0f;
        HealthBar(root, 2.2f);
        Shadow(root, 1.5f);
        Flash(root, v, except: mouth.transform);
        return root;
    }

    /// <summary>Hồ Tinh: cáo chín đuôi khổng lồ — đuôi xoè hình quạt, sáng xanh khi sắp ra đòn.</summary>
    static GameObject BuildBoss(GameObject minion)
    {
        var root = Root("Boss", kinematic: false, hp: 30);
        root.GetComponent<Rigidbody>().mass = 5f;
        AddBox(root, new Vector3(1.8f, 2f, 1.1f));
        var v = Visual(root);
        Color fur = new Color(0.95f, 0.55f, 0.2f);

        var tailRoot = new GameObject("Tails").transform;
        tailRoot.SetParent(v, false);
        tailRoot.localPosition = new Vector3(-0.7f, 0.8f, -5 * LayerDepth);
        var tails = new SpriteRenderer[9];
        for (int i = 0; i < tails.Length; i++)
        {
            var pivot = new GameObject($"Tail {i + 1}").transform;
            pivot.SetParent(tailRoot, false);
            pivot.localRotation = Quaternion.Euler(0f, 0f, (i - 4) * 14f + 30f);
            pivot.localPosition = new Vector3(0f, 0f, -i * LayerDepth * 0.2f);
            var t = Shape(pivot, "Fur", true, new Vector2(-0.9f, 0f), new Vector2(1.9f, 0.5f), new Color(1f, 0.85f, 0.6f), 0);
            Shape(pivot, "Tip", true, new Vector2(-1.7f, 0f), new Vector2(0.45f, 0.4f), Color.white, 1);
            tails[i] = t.GetComponent<SpriteRenderer>();
            Unlit(t);
        }
        ZodiacLights.AddPoint(tailRoot.gameObject, new Color(0.6f, 0.85f, 1f), 6f, 1.5f);
        Shape(v, "Body", true, new Vector2(-0.1f, 0.85f), new Vector2(1.9f, 1.5f), fur, 8);
        Shape(v, "Chest", true, new Vector2(0.4f, 0.9f), new Vector2(0.8f, 1f), new Color(1f, 0.95f, 0.85f), 9);
        Shape(v, "LegBack", false, new Vector2(-0.55f, 0.2f), new Vector2(0.35f, 0.4f), new Color(0.6f, 0.3f, 0.12f), 7);
        Shape(v, "LegFront", false, new Vector2(0.5f, 0.2f), new Vector2(0.35f, 0.4f), new Color(0.6f, 0.3f, 0.12f), 10);
        Shape(v, "Head", true, new Vector2(0.7f, 1.75f), new Vector2(1.1f, 0.95f), fur, 10);
        Shape(v, "Snout", true, new Vector2(1.2f, 1.6f), new Vector2(0.55f, 0.4f), new Color(1f, 0.92f, 0.8f), 11);
        Shape(v, "Nose", true, new Vector2(1.45f, 1.65f), new Vector2(0.15f, 0.12f), new Color(0.15f, 0.1f, 0.1f), 12);
        Shape(v, "EarBack", false, new Vector2(0.4f, 2.2f), new Vector2(0.35f, 0.35f), new Color(0.85f, 0.4f, 0.15f), 9, 45f);
        Shape(v, "EarFront", false, new Vector2(0.85f, 2.25f), new Vector2(0.35f, 0.35f), new Color(0.85f, 0.4f, 0.15f), 11, 45f);
        Unlit(Shape(v, "Eye", false, new Vector2(0.95f, 1.85f), new Vector2(0.22f, 0.08f), new Color(0.3f, 0.95f, 1f), 13, -12f));
        Shape(v, "Mark", true, new Vector2(0.6f, 2.05f), new Vector2(0.14f, 0.14f), new Color(0.8f, 0.1f, 0.15f), 12);

        var marker = new GameObject("LandingMarker");
        marker.transform.SetParent(root.transform, false);
        var markerSr = marker.AddComponent<SpriteRenderer>();
        markerSr.color = new Color(0.6f, 0.9f, 1f, 0.7f);
        markerSr.sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
        marker.SetActive(false);

        var ai = root.AddComponent<BossFoxSpirit>();
        Common(ai, v, fur);
        ai.tails = tails;
        ai.minionPrefab = minion;
        ai.landingMarker = markerSr;
        ai.knockbackTaken = 0f;
        ai.stunTime = 0f;
        ai.stunMultiplier = 0.35f;
        ai.deathFadeTime = 1.2f;
        Shadow(root, 2.4f);
        Flash(root, v, except: tailRoot);
        return root;
    }

    // ───────────────────────────── Dựng chung ─────────────────────────────

    static GameObject Root(string name, bool kinematic, int hp)
    {
        var root = new GameObject(name) { layer = enemyLayer };
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = kinematic;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        root.AddComponent<Health>().maxHealth = hp * ZodiacBalance.Unit;   // hp tính theo tim (thang cũ)
        return root;
    }

    /// <summary>Collider hộp đặt chân ở gốc toạ độ; không va chạm vật lý với người chơi (sát thương do đòn đánh quét).</summary>
    static void AddBox(GameObject root, Vector3 size)
    {
        var body = root.AddComponent<BoxCollider>();
        body.size = size;
        body.center = new Vector3(0f, size.y / 2f, 0f);
        body.sharedMaterial = noFriction;
        body.excludeLayers = 1 << playerLayer;
    }

    /// <summary>Root → Tilt (ngả theo camera) → Visual (lật trái/phải, EnemyBase điều khiển).</summary>
    static Transform Visual(GameObject root)
    {
        var tilt = new GameObject("Tilt");
        tilt.transform.SetParent(root.transform, false);
        tilt.AddComponent<Billboard>();
        var visual = new GameObject("Visual").transform;
        visual.SetParent(tilt.transform, false);
        return visual;
    }

    static void Common(EnemyBase ai, Transform visual, Color debris)
    {
        ai.groundLayer = 1 << groundLayer;
        ai.playerLayer = 1 << playerLayer;
        ai.visual = visual;
        ai.debrisColor = debris;
    }

    /// <summary>
    /// Ảnh vẽ sẵn (Art/Enemies/name.png) cao height unit, chân (pivot) ở localPosition. Không có ảnh thì trả null
    /// để nơi gọi ghép hình khối thay thế. squashX &lt; 1: dẹt ngang (khiên tròn nhìn nghiêng).
    /// </summary>
    static SpriteRenderer Art(Transform parent, string name, float height, Vector2 localPosition, int order, float rotation = 0f,
                              bool lit = true, float squashX = 1f)
    {
        var sprite = ZodiacEnemyArt.Get(name);
        if (sprite == null) return null;
        var go = Shape(parent, name, false, localPosition, Vector2.one, Color.white, order, rotation);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        float s = height / sprite.bounds.size.y;
        go.transform.localScale = new Vector3(s * squashX, s, 1f);
        if (!lit) Unlit(go);
        return sr;
    }

    /// <summary>Thanh máu + bóng tròn + nháy khi trúng đòn — phần chung của quái đứng trên đất.</summary>
    static void Finish(GameObject root, Transform visual, float barHeight, float shadowSize)
    {
        HealthBar(root, barHeight);
        Shadow(root, shadowSize);
        Flash(root, visual);
    }

    static void Flash(GameObject root, Transform visual, Transform except = null)
    {
        var flash = root.AddComponent<HitFlash>();
        var renderers = new System.Collections.Generic.List<SpriteRenderer>(visual.GetComponentsInChildren<SpriteRenderer>());
        if (except != null) renderers.RemoveAll(r => r.transform.IsChildOf(except));
        flash.renderers = renderers.ToArray();
        // Ảnh vẽ (màu trắng = giữ nguyên ảnh) nháy đỏ; hình khối tô màu thì nháy trắng
        bool art = renderers.Exists(r => r.sprite != square && r.sprite != circle);
        flash.flashColor = art ? new Color(1f, 0.45f, 0.45f) : Color.white;
    }

    /// <summary>Một hình phẳng; sortingOrder cao hơn = nằm trước (gần camera hơn một chút theo Z).</summary>
    static GameObject Shape(Transform parent, string name, bool round, Vector2 localPosition, Vector2 size, Color color,
                            int order, float rotation = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(localPosition.x, localPosition.y, -order * LayerDepth);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = round ? circle : square;
        sr.color = color;
        sr.sortingOrder = order;
        sr.sharedMaterial = ZodiacVisualSetup.SpriteLit3D;
        sr.shadowCastingMode = ShadowCastingMode.On;
        sr.receiveShadows = true;
        return go;
    }

    static GameObject Unlit(GameObject go)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
        sr.shadowCastingMode = ShadowCastingMode.Off;
        sr.sortingOrder += 20;   // vẽ sau phần thân (trong suốt, không ghi depth)
        return go;
    }

    /// <summary>Hai mắt tự phát sáng — nhìn thấy quái trong bóng tối.</summary>
    static void Eyes(Transform v, Vector2 front, float size, Color color)
    {
        Unlit(Shape(v, "EyeFront", true, front, Vector2.one * size, color, 14));
        Unlit(Shape(v, "EyeBack", true, front + new Vector2(-size * 1.4f, 0f), Vector2.one * size, color, 14));
    }

    static void Shadow(GameObject root, float size)
    {
        var go = new GameObject("BlobShadow");
        go.transform.SetParent(root.transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
        sr.sortingOrder = -5;
        var blob = go.AddComponent<BlobShadow>();
        blob.target = root.transform;
        blob.size = size;
        blob.groundLayers = 1 << groundLayer;
    }

    /// <summary>Thanh máu trên đầu: quay hẳn về camera, không lật theo quái; ẩn khi máu đầy.</summary>
    static void HealthBar(GameObject root, float height)
    {
        var barRoot = new GameObject("HealthBar").transform;
        barRoot.SetParent(root.transform, false);
        barRoot.localPosition = new Vector3(0f, height, 0f);
        barRoot.gameObject.AddComponent<Billboard>().full = true;
        var bar = new GameObject("Bar").transform;
        bar.SetParent(barRoot, false);
        var back = ZodiacPrototypeBuilder.CreateDecor("Back", bar, square, Vector2.zero, new Vector2(1f, 0.14f), BarBackColor, 40);
        var fillPivot = new GameObject("FillPivot").transform;
        fillPivot.SetParent(bar, false);
        fillPivot.localPosition = new Vector3(-0.5f, 0f, -0.01f);
        ZodiacPrototypeBuilder.CreateDecor("Fill", fillPivot, square, new Vector2(0.5f, 0f), new Vector2(1f, 0.1f), EnemyBarColor, 41);
        var healthBar = barRoot.gameObject.AddComponent<HealthBar>();
        healthBar.health = root.GetComponent<Health>();
        healthBar.fill = fillPivot;
        healthBar.hideWhenFull = true;
        healthBar.visualRoot = bar.gameObject;
        foreach (var sr in bar.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
            sr.shadowCastingMode = ShadowCastingMode.Off;
        }
        back.transform.localPosition = Vector3.zero;
    }
}
