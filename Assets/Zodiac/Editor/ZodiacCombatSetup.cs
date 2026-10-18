using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu "Tools/Zodiac/Thêm chiến đấu vào Level01": bổ sung hệ thống chiến đấu vào scene màn chơi đang có,
/// không dựng lại scene. Thêm cho Player: Health, HitFlash, PlayerMeleeAttack + AttackHitbox/SlashEffect;
/// thêm thanh máu HUD; tạo prefab quái Enemy_Placeholder (nếu chưa có) và đặt 3 quái.
/// Phần nào đã có thì bỏ qua — chạy lại không tạo trùng và không ghi đè chỉnh sửa trong Inspector.
/// ZodiacPrototypeBuilder cũng gọi Apply() khi dựng màn mới.
/// </summary>
public static class ZodiacCombatSetup
{
    const string PrefabFolder = "Assets/Zodiac/Prefabs";
    const string EnemyPrefabPath = PrefabFolder + "/Enemy_Placeholder.prefab";

    static readonly Color EnemyColor = new Color(0.55f, 0.25f, 0.65f);
    static readonly Color BarBackColor = new Color(0.15f, 0.1f, 0.1f, 0.85f);
    static readonly Color PlayerBarColor = new Color(0.85f, 0.15f, 0.15f);
    static readonly Color EnemyBarColor = new Color(0.95f, 0.3f, 0.2f);
    static readonly Color SlashColor = new Color(1f, 1f, 0.85f, 0.55f);

    // Quái đặt trên mặt đất: x, quãng tuần tra mỗi phía.
    static readonly (float x, float patrol)[] EnemySpawns = { (6f, 3f), (18f, 2.5f), (44f, 3f) };

    // Menu mờ đi trong Play mode — sửa/lưu scene lúc đó sẽ lỗi InvalidOperationException.
    [MenuItem("Tools/Zodiac/Thêm chiến đấu vào Level01", true)]
    static bool CanApply() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem("Tools/Zodiac/Thêm chiến đấu vào Level01")]
    static void ApplyInteractive()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        ApplyToLevelScene();
    }

    /// <summary>Gọi từ dòng lệnh: Unity -batchmode -executeMethod ZodiacCombatSetup.ApplyBatch -quit</summary>
    public static void ApplyBatch() => ApplyToLevelScene();

    static void ApplyToLevelScene()
    {
        var scene = EditorSceneManager.OpenScene(ZodiacPrototypeBuilder.LevelScenePath);
        if (!Apply()) return;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Đã thêm hệ thống chiến đấu vào " + scene.path);
    }

    /// <summary>Bổ sung chiến đấu vào scene đang mở. Trả về false nếu thiếu thành phần bắt buộc.</summary>
    public static bool Apply()
    {
        var player = Object.FindAnyObjectByType<ZodiacPlayerController>();
        var manager = Object.FindAnyObjectByType<ZodiacGameManager>();
        int groundLayer = LayerMask.NameToLayer("Ground");
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        var square = AssetDatabase.LoadAssetAtPath<Sprite>(ZodiacPrototypeBuilder.SquarePath);
        var circle = AssetDatabase.LoadAssetAtPath<Sprite>(ZodiacPrototypeBuilder.CirclePath);
        var noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(ZodiacPrototypeBuilder.NoFrictionPath);

        if (player == null || manager == null || player.facing == null)
        {
            Debug.LogError("Scene thiếu Player (ZodiacPlayerController + Facing) hoặc GameManager.");
            return false;
        }
        if (groundLayer < 0 || playerLayer < 0 || enemyLayer < 0)
        {
            Debug.LogError("Thiếu layer Ground / Player / Enemy trong Project Settings > Tags and Layers.");
            return false;
        }
        if (square == null || circle == null || noFriction == null)
        {
            Debug.LogError("Thiếu asset placeholder hoặc NoFriction — chạy Tools/Zodiac/Dựng prototype một lần trước.");
            return false;
        }

        var health = SetupPlayer(player, circle, enemyLayer);
        SetupHud(manager, health);
        var prefab = EnsureEnemyPrefab(square, noFriction, groundLayer, playerLayer, enemyLayer);
        PlaceEnemies(prefab);
        return true;
    }

    static Health SetupPlayer(ZodiacPlayerController player, Sprite circle, int enemyLayer)
    {
        var go = player.gameObject;
        if (!go.TryGetComponent(out Health health))
        {
            health = go.AddComponent<Health>();
            health.maxHealth = 5;
            health.invulnerableTime = 0.8f;
        }
        if (!go.TryGetComponent(out HitFlash flash))
        {
            flash = go.AddComponent<HitFlash>();
            flash.renderers = new[] { player.spriteRenderer };
            flash.flashColor = new Color(1f, 0.35f, 0.35f);
        }
        if (!go.TryGetComponent(out PlayerMeleeAttack attack))
        {
            attack = go.AddComponent<PlayerMeleeAttack>();

            // Hitbox là con của Facing → tự lật theo hướng nhìn. Để tắt: script chỉ dùng hình dạng để quét.
            var hitboxGo = new GameObject("AttackHitbox");
            hitboxGo.transform.SetParent(player.facing, false);
            hitboxGo.transform.localPosition = new Vector3(0.85f, 1f, 0f);
            var box = hitboxGo.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1f, 1.2f);
            box.enabled = false;

            var slash = new GameObject("SlashEffect");
            slash.transform.SetParent(hitboxGo.transform, false);
            slash.transform.localScale = new Vector3(box.size.x, box.size.y, 1f);
            var sr = slash.AddComponent<SpriteRenderer>();
            sr.sprite = circle;
            sr.color = SlashColor;
            sr.sortingOrder = 11;
            slash.SetActive(false);

            attack.hitbox = box;
            attack.slashEffect = slash;
            attack.targetLayers = 1 << enemyLayer;
        }
        EditorUtility.SetDirty(go);
        return health;
    }

    static void SetupHud(ZodiacGameManager manager, Health playerHealth)
    {
        if (manager.loseDetailText == null && manager.losePanel != null)
        {
            var detail = manager.losePanel.transform.Find("Box/Detail");
            if (detail != null) manager.loseDetailText = detail.GetComponent<Text>();
        }
        EditorUtility.SetDirty(manager);

        Transform canvas = manager.collectText.transform.parent;
        if (canvas.Find("PlayerHealthBar") != null) return;

        var back = ZodiacPrototypeBuilder.CreateImage(canvas, "PlayerHealthBar", BarBackColor);
        back.rectTransform.pivot = new Vector2(0f, 1f);
        ZodiacPrototypeBuilder.Place(back.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -140f), new Vector2(360f, 36f));
        back.raycastTarget = false;

        var fill = ZodiacPrototypeBuilder.CreateImage(back.transform, "Fill", PlayerBarColor);
        ZodiacPrototypeBuilder.Stretch(fill.rectTransform);
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        fill.rectTransform.offsetMin = new Vector2(3f, 3f);
        fill.rectTransform.offsetMax = new Vector2(-3f, -3f);
        fill.raycastTarget = false;

        var label = ZodiacPrototypeBuilder.CreateText(back.transform, "Label", "Máu", 24, Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 36f), TextAnchor.MiddleCenter);

        var bar = back.gameObject.AddComponent<HealthBar>();
        bar.health = playerHealth;
        bar.fill = fill.rectTransform;
        bar.label = label;
        // Vẽ trước bảng thắng/thua để các bảng đó luôn che lên thanh máu
        back.transform.SetSiblingIndex(manager.winPanel.transform.GetSiblingIndex());
    }

    static GameObject EnsureEnemyPrefab(Sprite square, PhysicsMaterial2D noFriction, int groundLayer, int playerLayer, int enemyLayer)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        if (existing != null) return existing;
        ZodiacPrototypeBuilder.EnsureFolder(PrefabFolder);

        var root = new GameObject("Enemy_Placeholder") { layer = enemyLayer };
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var body = root.AddComponent<BoxCollider2D>();
        body.size = new Vector2(0.9f, 0.8f);
        body.offset = new Vector2(0f, 0.4f);
        body.sharedMaterial = noFriction;
        body.excludeLayers = 1 << playerLayer;   // không đẩy/đứng lên người chơi; sát thương chạm do script quét

        var health = root.AddComponent<Health>();
        health.maxHealth = 3;

        // Hình placeholder: khối tím + 2 mắt chỉ hướng đi
        var visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);
        ZodiacPrototypeBuilder.CreateDecor("Body", visual, square, new Vector2(0f, 0.4f), new Vector2(0.9f, 0.8f), EnemyColor, 8);
        ZodiacPrototypeBuilder.CreateDecor("EyeFront", visual, square, new Vector2(0.3f, 0.55f), new Vector2(0.14f, 0.18f), Color.white, 9);
        ZodiacPrototypeBuilder.CreateDecor("EyeBack", visual, square, new Vector2(0.08f, 0.55f), new Vector2(0.14f, 0.18f), Color.white, 9);

        // Thanh máu trên đầu: không nằm trong Visual nên không bị lật
        var barRoot = new GameObject("HealthBar").transform;
        barRoot.SetParent(root.transform, false);
        barRoot.localPosition = new Vector3(0f, 1.1f, 0f);
        var bar = new GameObject("Bar").transform;
        bar.SetParent(barRoot, false);
        ZodiacPrototypeBuilder.CreateDecor("Back", bar, square, Vector2.zero, new Vector2(1f, 0.14f), BarBackColor, 20);
        var fillPivot = new GameObject("FillPivot").transform;
        fillPivot.SetParent(bar, false);
        fillPivot.localPosition = new Vector3(-0.5f, 0f, 0f);
        ZodiacPrototypeBuilder.CreateDecor("Fill", fillPivot, square, new Vector2(0.5f, 0f), new Vector2(1f, 0.1f), EnemyBarColor, 21);
        var healthBar = barRoot.gameObject.AddComponent<HealthBar>();
        healthBar.health = health;
        healthBar.fill = fillPivot;
        healthBar.hideWhenFull = true;
        healthBar.visualRoot = bar.gameObject;

        var flash = root.AddComponent<HitFlash>();
        flash.renderers = visual.GetComponentsInChildren<SpriteRenderer>();
        flash.flashColor = Color.white;

        var patrol = root.AddComponent<EnemyPatrol>();
        patrol.groundLayer = 1 << groundLayer;
        patrol.playerLayer = 1 << playerLayer;
        patrol.visual = visual;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static void PlaceEnemies(GameObject prefab)
    {
        if (GameObject.Find("Enemies") != null) return;
        var root = new GameObject("Enemies").transform;
        for (int i = 0; i < EnemySpawns.Length; i++)
        {
            var (x, patrol) = EnemySpawns[i];
            var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            enemy.name = $"Enemy {i + 1}";
            enemy.transform.position = new Vector3(x, 0.02f, 0f);
            enemy.GetComponent<EnemyPatrol>().patrolDistance = patrol;
        }
    }
}
