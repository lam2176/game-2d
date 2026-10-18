using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu "Tools > Tạo scene Demo 2D": sinh sprite pixel art (PixelArtLibrary), tạo layer, prefab chưởng,
/// mặt đất, nhân vật, quái, camera rồi lưu thành Assets/Scenes/Demo2D.unity. Chạy lại sẽ ghi đè scene demo.
/// </summary>
public static class DemoSceneBuilder
{
    const string PrefabFolder = "Assets/Prefabs";
    const string KiBlastPrefabPath = PrefabFolder + "/KiBlast.prefab";
    const string ScenePath = "Assets/Scenes/Demo2D.unity";

    [MenuItem("Tools/Tạo scene Demo 2D")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        int ground = EnsureLayer("Ground");
        int player = EnsureLayer("Player");
        int enemy = EnsureLayer("Enemy");

        var hero = PixelArtLibrary.BuildHero();
        Sprite slime = PixelArtLibrary.BuildSlime();
        Sprite groundTile = PixelArtLibrary.BuildGroundTile();
        KiBlast kiBlastPrefab = BuildKiBlastPrefab(PixelArtLibrary.BuildOrb());

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.45f, 0.75f, 0.95f);
        cam.transform.position = new Vector3(0, 0, -10);

        // Mặt đất và vài khối đất lơ lửng
        CreateTiledBlock("Ground", groundTile, new Vector2(0, -3.5f), new Vector2(60, 2), ground);
        CreateTiledBlock("Platform 1", groundTile, new Vector2(-6, 0), new Vector2(4, 1), ground);
        CreateTiledBlock("Platform 2", groundTile, new Vector2(7, 1.5f), new Vector2(4, 1), ground);

        // Nhân vật: object gốc lo vật lý/logic, object con "Visual" lo hình ảnh
        var playerGo = new GameObject("Player") { layer = player };
        playerGo.transform.position = new Vector2(-2, -1.5f);
        var prb = playerGo.AddComponent<Rigidbody2D>();
        prb.gravityScale = 3f;
        prb.constraints = RigidbodyConstraints2D.FreezeRotation;
        var pcol = playerGo.AddComponent<BoxCollider2D>();
        pcol.size = new Vector2(0.6f, 1.45f);
        pcol.offset = new Vector2(0, -0.02f);
        playerGo.AddComponent<CharacterStats>();
        var pc = playerGo.AddComponent<PlayerController2D>();
        playerGo.AddComponent<PlayerHUD>();

        var heroAnim = AddVisual(playerGo, hero.idle[0], 10);
        heroAnim.idle = hero.idle;
        heroAnim.run = hero.run;
        heroAnim.jump = hero.jump;
        heroAnim.fly = hero.fly;
        heroAnim.attack = hero.attack;

        var groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(playerGo.transform, false);
        groundCheck.localPosition = new Vector3(0, -0.75f, 0);
        var firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(playerGo.transform, false);
        firePoint.localPosition = new Vector3(0.6f, 0.05f, 0);

        pc.jumpForce = 14f;
        pc.groundCheck = groundCheck;
        pc.groundLayer = 1 << ground;
        pc.enemyLayer = 1 << enemy;
        pc.firePoint = firePoint;
        pc.kiBlastPrefab = kiBlastPrefab;

        cam.gameObject.AddComponent<CameraFollow2D>().target = playerGo.transform;

        // Quái
        CreateSlime("Slime 1", slime, new Vector2(4, -2f), enemy, player);
        CreateSlime("Slime 2", slime, new Vector2(10, -2f), enemy, player);
        CreateSlime("Slime 3", slime, new Vector2(-10, -2f), enemy, player);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuild(ScenePath);
        Debug.Log("Đã tạo " + ScenePath + ". Bấm Play để chơi: WASD di chuyển, Space nhảy/bay, chuột trái đánh, E bắn chưởng.");
    }

    static CharacterSpriteAnimator AddVisual(GameObject parent, Sprite sprite, int sortingOrder)
    {
        var visual = new GameObject("Visual");
        visual.transform.SetParent(parent.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        var anim = visual.AddComponent<CharacterSpriteAnimator>();
        anim.idle = new[] { sprite };
        return anim;
    }

    static void CreateTiledBlock(string name, Sprite tile, Vector2 pos, Vector2 size, int layer)
    {
        var go = new GameObject(name) { layer = layer };
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = tile;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        go.AddComponent<BoxCollider2D>().size = size;
    }

    static void CreateSlime(string name, Sprite sprite, Vector2 pos, int enemyLayer, int playerLayer)
    {
        var go = new GameObject(name) { layer = enemyLayer };
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 0.75f);
        col.offset = new Vector2(0, -0.06f);
        var stats = go.AddComponent<CharacterStats>();
        stats.maxHp = 60;
        stats.damage = 5;
        go.AddComponent<EnemyAI>().playerLayer = 1 << playerLayer;

        var anim = AddVisual(go, sprite, 5);
        anim.squash = true;
        anim.bobAmount = 0f;
    }

    static KiBlast BuildKiBlastPrefab(Sprite orb)
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets", "Prefabs");

        var go = new GameObject("KiBlast");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = orb;
        sr.sortingOrder = 20;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<CircleCollider2D>().isTrigger = true;
        go.AddComponent<KiBlast>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, KiBlastPrefabPath);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<KiBlast>();
    }

    static int EnsureLayer(string name)
    {
        int idx = LayerMask.NameToLayer(name);
        if (idx >= 0) return idx;

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var sp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = name;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }
        throw new System.InvalidOperationException("Hết slot layer trống để tạo layer " + name);
    }

    static void AddSceneToBuild(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path)) return;
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
