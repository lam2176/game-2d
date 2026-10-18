using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Menu "Tools/Zodiac/Dựng prototype": tạo asset (placeholder, nhân vật Khỉ, animation, vật liệu vật lý) nếu chưa có,
/// dựng 2 scene Zodiac_Start và Zodiac_Level01, rồi đưa chúng lên đầu Build Settings.
/// Asset đã có thì giữ nguyên (không mất chỉnh sửa tay); scene đã có thì hỏi trước khi ghi đè.
/// Không đụng tới ảnh gốc trong Assets/Characters.
///
/// Animation Idle/Walk/Run/Jump chỉ biến đổi transform (nhún, nghiêng, co giãn) trên ảnh tĩnh duy nhất —
/// không có frame vẽ thêm. Khi có sprite frame thật, thay clip trong Animator Controller là xong.
/// </summary>
public static class ZodiacPrototypeBuilder
{
    const string Root = "Assets/Zodiac";
    const string SceneFolder = Root + "/Scenes";
    const string StartScenePath = SceneFolder + "/Zodiac_Start.unity";
    internal const string LevelScenePath = SceneFolder + "/Zodiac_Level01.unity";
    const string PlaceholderFolder = Root + "/Art/Placeholder";
    internal const string SquarePath = PlaceholderFolder + "/Square.png";
    internal const string CirclePath = PlaceholderFolder + "/Circle.png";
    const string MonkeySpritePath = Root + "/Art/Characters/Monkey_Than.png";
    const string CharacterFolder = Root + "/Characters";
    const string MonkeyDataPath = CharacterFolder + "/Monkey_Than.asset";
    const string AnimationFolder = Root + "/Animations";
    const string ControllerPath = AnimationFolder + "/ZodiacCharacter.controller";
    const string PhysicsFolder = Root + "/Physics";
    internal const string NoFrictionPath = PhysicsFolder + "/NoFriction.physicsMaterial2D";

    static readonly Color SkyColor = new Color(0.98f, 0.90f, 0.75f);
    static readonly Color SoilColor = new Color(0.55f, 0.36f, 0.22f);
    static readonly Color GrassColor = new Color(0.36f, 0.68f, 0.30f);
    static readonly Color PlatformColor = new Color(0.80f, 0.55f, 0.30f);
    static readonly Color WallColor = new Color(0.40f, 0.27f, 0.18f);
    static readonly Color CoinColor = new Color(1f, 0.80f, 0.15f);
    static readonly Color FlagColor = new Color(0.85f, 0.12f, 0.12f);
    static readonly Color PoleColor = new Color(0.30f, 0.20f, 0.12f);
    static readonly Color TitleColor = new Color(0.70f, 0.08f, 0.08f);
    static readonly Color ButtonColor = new Color(0.80f, 0.15f, 0.12f);

    const float GroundDepth = 4f;
    const float PlatformThickness = 0.5f;
    const float GrassThickness = 0.2f;

    // Mặt đất: mặt trên ở y = 0. Khoảng trống giữa các đoạn là vực.
    // Vực 1: x 12 → 15 (rộng 3). Vực 2: x 30 → 36 (rộng 6, có bậc đá ở giữa).
    static readonly (float left, float right)[] GroundSegments =
    {
        (-12f, 12f),
        (15f, 30f),
        (36f, 58f),
    };

    // Nền tảng lơ lửng: tâm x, mặt trên y, chiều rộng.
    static readonly (float x, float top, float width)[] Platforms =
    {
        (21f, 2.5f, 4f),
        (25.5f, 4.5f, 3f),
        (33f, 1.5f, 2f),   // bậc đá giữa vực 2
        (42f, 3f, 4f),
        (47.5f, 5.5f, 3f),
    };

    static readonly Vector2[] CoinPositions =
    {
        new Vector2(-4f, 1f), new Vector2(0f, 1f), new Vector2(4f, 1f),
        new Vector2(13.5f, 2.5f),                    // trên vực 1
        new Vector2(21f, 3.5f), new Vector2(25.5f, 5.5f),
        new Vector2(33f, 2.5f),                      // trên bậc đá
        new Vector2(39f, 1f), new Vector2(42f, 4f), new Vector2(47.5f, 6.5f),
        new Vector2(52f, 1f),
    };

    static readonly Vector2 SpawnPoint = new Vector2(-8f, 0.05f);
    const float GoalX = 55f;
    static readonly Rect CameraBounds = new Rect(-13f, -4f, 72f, 20f);

    [MenuItem("Tools/Zodiac/Dựng prototype")]
    public static void BuildInteractive() => Build(interactive: true);

    /// <summary>Gọi từ dòng lệnh: Unity -batchmode -executeMethod ZodiacPrototypeBuilder.BuildBatch -quit</summary>
    public static void BuildBatch() => Build(interactive: false);

    static void Build(bool interactive)
    {
        if (interactive)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var existing = new[] { StartScenePath, LevelScenePath }.Where(File.Exists).ToArray();
            if (existing.Length > 0 &&
                !EditorUtility.DisplayDialog("Scene đã tồn tại",
                    string.Join("\n", existing) + "\n\nGhi đè sẽ mất mọi chỉnh sửa tay trong các scene này.", "Ghi đè", "Huỷ"))
                return;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        int playerLayer = LayerMask.NameToLayer("Player");
        if (groundLayer < 0 || playerLayer < 0)
        {
            Debug.LogError("Thiếu layer \"Ground\" hoặc \"Player\" trong Project Settings > Tags and Layers.");
            return;
        }

        var monkeySprite = AssetDatabase.LoadAssetAtPath<Sprite>(MonkeySpritePath);
        if (monkeySprite == null)
        {
            Debug.LogError("Không tìm thấy sprite " + MonkeySpritePath);
            return;
        }

        Sprite square = EnsurePlaceholderSprite(SquarePath, 4, circle: false);
        Sprite circle = EnsurePlaceholderSprite(CirclePath, 64, circle: true);
        ZodiacCharacter monkey = EnsureMonkeyData(monkeySprite);
        AnimatorController controller = EnsureAnimatorController();
        PhysicsMaterial2D noFriction = EnsureNoFriction();

        BuildLevelScene(square, circle, monkey, controller, noFriction, groundLayer, playerLayer);
        BuildStartScene(monkey);
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(StartScenePath);
        Debug.Log("Đã dựng prototype: " + StartScenePath + " và " + LevelScenePath + ". Bấm Play ở Zodiac_Start để chơi.");
    }

    // ───────────────────────────── Màn chơi ─────────────────────────────

    static void BuildLevelScene(Sprite square, Sprite circle, ZodiacCharacter monkey, AnimatorController controller,
                                PhysicsMaterial2D noFriction, int groundLayer, int playerLayer)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = CreateCamera(new Vector3(SpawnPoint.x, 3f, -10f));

        var level = new GameObject("Level").transform;
        for (int i = 0; i < GroundSegments.Length; i++)
        {
            var (left, right) = GroundSegments[i];
            CreateBlock($"Ground {i + 1}", level, square,
                new Vector2((left + right) / 2f, -GroundDepth / 2f), new Vector2(right - left, GroundDepth),
                SoilColor, groundLayer, grass: true);
        }
        for (int i = 0; i < Platforms.Length; i++)
        {
            var (x, top, width) = Platforms[i];
            CreateBlock($"Platform {i + 1}", level, square,
                new Vector2(x, top - PlatformThickness / 2f), new Vector2(width, PlatformThickness),
                PlatformColor, groundLayer, grass: true);
        }
        float leftEdge = GroundSegments[0].left, rightEdge = GroundSegments[GroundSegments.Length - 1].right;
        CreateBlock("Wall Left", level, square, new Vector2(leftEdge - 0.5f, 4f), new Vector2(1f, 16f), WallColor, groundLayer, grass: false);
        CreateBlock("Wall Right", level, square, new Vector2(rightEdge + 0.5f, 4f), new Vector2(1f, 16f), WallColor, groundLayer, grass: false);

        var coins = new GameObject("Collectibles").transform;
        for (int i = 0; i < CoinPositions.Length; i++) CreateCoin($"Coin {i + 1}", coins, circle, CoinPositions[i]);

        var goal = CreateGoal(square, new Vector2(GoalX, 0f));

        var killZone = new GameObject("KillZone");
        killZone.transform.position = new Vector2((leftEdge + rightEdge) / 2f, -8f);
        killZone.AddComponent<BoxCollider2D>().size = new Vector2(rightEdge - leftEdge + 10f, 2f);
        var killTrigger = killZone.AddComponent<PlayerTrigger>();

        var player = CreatePlayer(monkey, controller, noFriction, groundLayer, playerLayer);

        var follow = cam.gameObject.AddComponent<ZodiacCameraFollow>();
        follow.target = player.transform;
        follow.bounds = CameraBounds;

        var manager = new GameObject("GameManager").AddComponent<ZodiacGameManager>();
        manager.player = player;
        manager.goal = goal;
        manager.killZone = killTrigger;
        BuildLevelUI(manager, monkey);
        ZodiacCombatSetup.Apply();

        EnsureFolder(SceneFolder);
        EditorSceneManager.SaveScene(scene, LevelScenePath);
    }

    static ZodiacPlayerController CreatePlayer(ZodiacCharacter character, AnimatorController controller,
                                               PhysicsMaterial2D noFriction, int groundLayer, int playerLayer)
    {
        var go = new GameObject("Player") { layer = playerLayer };
        go.transform.position = SpawnPoint;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;   // ZodiacPlayerController.gravityScale ghi đè khi vào game
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var body = go.AddComponent<CapsuleCollider2D>();
        body.direction = CapsuleDirection2D.Vertical;
        body.size = character.colliderSize;
        body.offset = character.colliderOffset;
        body.sharedMaterial = noFriction;

        // Player (vật lý) → Facing (lật trái/phải) → Visual (ảnh + Animator chỉnh transform)
        var facing = new GameObject("Facing").transform;
        facing.SetParent(go.transform, false);
        var visual = new GameObject("Visual");
        visual.transform.SetParent(facing, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = character.sprite;
        sr.sortingOrder = 10;
        var animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        var pc = go.AddComponent<ZodiacPlayerController>();
        pc.character = character;
        pc.groundLayer = 1 << groundLayer;
        pc.facing = facing;
        pc.spriteRenderer = sr;
        pc.animator = animator;
        return pc;
    }

    static void CreateCoin(string name, Transform parent, Sprite circle, Vector2 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.6f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circle;
        sr.color = CoinColor;
        sr.sortingOrder = 5;
        go.AddComponent<CircleCollider2D>().radius = 0.5f;
        go.AddComponent<Collectible>();
    }

    static PlayerTrigger CreateGoal(Sprite square, Vector2 basePosition)
    {
        var go = new GameObject("Goal");
        go.transform.position = basePosition;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.5f, 4f);
        box.offset = new Vector2(0f, 2f);
        var trigger = go.AddComponent<PlayerTrigger>();

        CreateDecor("Pole", go.transform, square, new Vector2(0f, 2f), new Vector2(0.15f, 4f), PoleColor, 2);
        CreateDecor("Flag", go.transform, square, new Vector2(0.65f, 3.55f), new Vector2(1.2f, 0.8f), FlagColor, 3);
        return trigger;
    }

    static Camera CreateCamera(Vector3 position)
    {
        var cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = SkyColor;
        cam.transform.position = position;
        cam.gameObject.AddComponent<AudioListener>();
        return cam;
    }

    static void CreateBlock(string name, Transform parent, Sprite sprite, Vector2 center, Vector2 size,
                            Color color, int layer, bool grass)
    {
        var go = new GameObject(name) { layer = layer };
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.color = color;
        go.AddComponent<BoxCollider2D>().size = size;

        if (grass)
            CreateDecor("Grass", go.transform, sprite, new Vector2(0f, size.y / 2f - GrassThickness / 2f),
                        new Vector2(size.x, GrassThickness), GrassColor, 1).layer = layer;
    }

    internal static GameObject CreateDecor(string name, Transform parent, Sprite sprite, Vector2 localPosition, Vector2 size,
                                  Color color, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    // ───────────────────────────── UI ─────────────────────────────

    static void BuildLevelUI(ZodiacGameManager manager, ZodiacCharacter character)
    {
        var canvas = CreateCanvas();

        manager.collectText = CreateText(canvas, "CollectText", "Đồng xu: 0/0", 40, Color.white,
            new Vector2(0f, 1f), new Vector2(30f, -25f), new Vector2(600f, 60f), TextAnchor.UpperLeft);
        CreateText(canvas, "CharacterText", $"Tuổi {character.zodiacName} ({character.displayName})", 30, Color.white,
            new Vector2(0f, 1f), new Vector2(30f, -85f), new Vector2(600f, 50f), TextAnchor.UpperLeft);
        CreateText(canvas, "HintText", "A/D: đi   Shift: chạy   Space: nhảy   R: chơi lại", 26, Color.white,
            new Vector2(1f, 1f), new Vector2(-30f, -25f), new Vector2(900f, 50f), TextAnchor.UpperRight);

        manager.winPanel = CreateResultPanel(canvas, "WinPanel", "CHIẾN THẮNG!", out Text winDetail, manager);
        manager.winDetailText = winDetail;
        manager.losePanel = CreateResultPanel(canvas, "LosePanel", "THUA RỒI!", out Text loseDetail, manager);
        loseDetail.text = "Bạn đã rơi xuống vực";
        CreateEventSystem(null);
    }

    static GameObject CreateResultPanel(Transform canvas, string name, string title, out Text detail, ZodiacGameManager manager)
    {
        var overlay = CreateImage(canvas, name, new Color(0f, 0f, 0f, 0.6f));
        Stretch(overlay.rectTransform);

        var box = CreateImage(overlay.transform, "Box", new Color(1f, 0.96f, 0.86f));
        Place(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 460f));

        CreateText(box.transform, "Title", title, 72, TitleColor, new Vector2(0.5f, 1f), new Vector2(0f, -50f),
                   new Vector2(700f, 100f), TextAnchor.MiddleCenter);
        detail = CreateText(box.transform, "Detail", "", 36, new Color(0.25f, 0.15f, 0.1f), new Vector2(0.5f, 1f),
                            new Vector2(0f, -170f), new Vector2(700f, 60f), TextAnchor.MiddleCenter);

        var restart = CreateButton(box.transform, "RestartButton", "Chơi lại", new Vector2(-170f, 80f));
        UnityEventTools.AddPersistentListener(restart.onClick, new UnityAction(manager.Restart));
        var menu = CreateButton(box.transform, "MenuButton", "Màn hình chính", new Vector2(170f, 80f));
        UnityEventTools.AddPersistentListener(menu.onClick, new UnityAction(manager.BackToMenu));
        foreach (var b in new[] { restart, menu })
        {
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        }
        return overlay.gameObject;
    }

    static void BuildStartScene(ZodiacCharacter character)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(new Vector3(0f, 0f, -10f));

        var menu = new GameObject("StartMenu").AddComponent<ZodiacStartMenu>();
        var canvas = CreateCanvas();

        CreateText(canvas, "Title", "12 CON GIÁP", 120, TitleColor, new Vector2(0.5f, 1f), new Vector2(0f, -110f),
                   new Vector2(1400f, 160f), TextAnchor.MiddleCenter);
        CreateText(canvas, "Subtitle", $"Prototype — Tuổi {character.zodiacName} ({character.displayName})", 40,
                   new Color(0.35f, 0.2f, 0.1f), new Vector2(0.5f, 1f), new Vector2(0f, -220f),
                   new Vector2(1400f, 60f), TextAnchor.MiddleCenter);

        var portrait = new GameObject("Portrait", typeof(RectTransform)).AddComponent<Image>();
        portrait.transform.SetParent(canvas, false);
        portrait.sprite = character.sprite;
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        Place(portrait.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(500f, 500f));

        var play = CreateButton(canvas, "PlayButton", "Bắt đầu", new Vector2(-170f, 150f));
        UnityEventTools.AddPersistentListener(play.onClick, new UnityAction(menu.Play));
        var quit = CreateButton(canvas, "QuitButton", "Thoát", new Vector2(170f, 150f));
        UnityEventTools.AddPersistentListener(quit.onClick, new UnityAction(menu.Quit));
        foreach (var b in new[] { play, quit })
        {
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        }

        CreateText(canvas, "Controls", "A/D hoặc ←/→: đi   ·   Shift: chạy   ·   Space: nhảy", 28,
                   new Color(0.35f, 0.2f, 0.1f), new Vector2(0.5f, 0f), new Vector2(0f, 50f),
                   new Vector2(1400f, 50f), TextAnchor.MiddleCenter);

        CreateEventSystem(play.gameObject);
        EnsureFolder(SceneFolder);
        EditorSceneManager.SaveScene(scene, StartScenePath);
    }

    static Transform CreateCanvas()
    {
        var go = new GameObject("Canvas", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go.transform;
    }

    static void CreateEventSystem(GameObject firstSelected)
    {
        var go = new GameObject("EventSystem");
        var es = go.AddComponent<EventSystem>();
        es.firstSelectedGameObject = firstSelected;
        go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    internal static Text CreateText(Transform parent, string name, string content, int size, Color color,
                           Vector2 anchor, Vector2 position, Vector2 boxSize, TextAnchor alignment)
    {
        var text = new GameObject(name, typeof(RectTransform)).AddComponent<Text>();
        text.transform.SetParent(parent, false);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        if (color == Color.white) text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        var rt = text.rectTransform;
        rt.pivot = new Vector2(anchor.x, anchor.y);
        Place(rt, anchor, position, boxSize);
        return text;
    }

    internal static Image CreateImage(Transform parent, string name, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        return image;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 position)
    {
        var image = CreateImage(parent, name, ButtonColor);
        Place(image.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(300f, 90f));
        var button = image.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
        colors.selectedColor = new Color(1f, 0.85f, 0.6f);
        button.colors = colors;
        var text = CreateText(image.transform, "Label", label, 38, Color.white, new Vector2(0.5f, 0.5f),
                              Vector2.zero, new Vector2(300f, 90f), TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        return button;
    }

    internal static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    internal static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    // ───────────────────────────── Asset ─────────────────────────────

    static ZodiacCharacter EnsureMonkeyData(Sprite sprite)
    {
        var data = AssetDatabase.LoadAssetAtPath<ZodiacCharacter>(MonkeyDataPath);
        if (data != null) return data;
        EnsureFolder(CharacterFolder);
        data = ScriptableObject.CreateInstance<ZodiacCharacter>();
        data.displayName = "Khỉ";
        data.zodiacName = "Thân";
        data.sprite = sprite;
        AssetDatabase.CreateAsset(data, MonkeyDataPath);
        return data;
    }

    static PhysicsMaterial2D EnsureNoFriction()
    {
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);
        if (mat != null) return mat;
        EnsureFolder(PhysicsFolder);
        mat = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(mat, NoFrictionPath);
        return mat;
    }

    /// <summary>
    /// Animator Controller dùng chung: 4 state Idle/Walk/Run/Jump, tham số Speed, Running, Grounded, VerticalSpeed.
    /// Clip chỉ chỉnh transform của object Visual (pivot ở chân nên co giãn không làm nhân vật lơ lửng).
    /// </summary>
    static AnimatorController EnsureAnimatorController()
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (existing != null) return existing;
        EnsureFolder(AnimationFolder);

        var idle = CreateClip("Idle", 1.2f, loop: true,
            ("m_LocalPosition.y", new[] { 0f, 0.6f, 1.2f }, new[] { 0f, 0.03f, 0f }),
            ("m_LocalScale.x", new[] { 0f, 0.6f, 1.2f }, new[] { 1f, 0.99f, 1f }),
            ("m_LocalScale.y", new[] { 0f, 0.6f, 1.2f }, new[] { 1f, 1.025f, 1f }));
        var walk = CreateClip("Walk", 0.5f, loop: true,
            ("m_LocalPosition.y", new[] { 0f, 0.125f, 0.25f, 0.375f, 0.5f }, new[] { 0f, 0.07f, 0f, 0.07f, 0f }),
            ("localEulerAnglesRaw.z", new[] { 0f, 0.25f, 0.5f }, new[] { 2f, -2f, 2f }));
        var run = CreateClip("Run", 0.36f, loop: true,
            ("m_LocalPosition.y", new[] { 0f, 0.09f, 0.18f, 0.27f, 0.36f }, new[] { 0f, 0.12f, 0f, 0.12f, 0f }),
            ("m_LocalScale.y", new[] { 0f, 0.09f, 0.18f, 0.27f, 0.36f }, new[] { 0.97f, 1.04f, 0.97f, 1.04f, 0.97f }),
            ("localEulerAnglesRaw.z", new[] { 0f, 0.18f, 0.36f }, new[] { -6f, -10f, -6f }));
        var jump = CreateClip("Jump", 0.25f, loop: false,
            ("m_LocalScale.x", new[] { 0f, 0.12f }, new[] { 1f, 0.92f }),
            ("m_LocalScale.y", new[] { 0f, 0.12f }, new[] { 1f, 1.08f }),
            ("localEulerAnglesRaw.z", new[] { 0f, 0.12f }, new[] { 0f, -6f }));

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Running", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);

        var sm = controller.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle", new Vector3(300f, 0f));
        var sWalk = sm.AddState("Walk", new Vector3(550f, 0f));
        var sRun = sm.AddState("Run", new Vector3(800f, 0f));
        var sJump = sm.AddState("Jump", new Vector3(550f, 150f));
        sIdle.motion = idle;
        sWalk.motion = walk;
        sRun.motion = run;
        sJump.motion = jump;
        sm.defaultState = sIdle;

        Transition(sIdle, sWalk, AnimatorConditionMode.Greater, "Speed", 0.1f);
        Transition(sWalk, sIdle, AnimatorConditionMode.Less, "Speed", 0.1f);
        Transition(sWalk, sRun, AnimatorConditionMode.If, "Running");
        Transition(sRun, sWalk, AnimatorConditionMode.IfNot, "Running");
        Transition(sRun, sIdle, AnimatorConditionMode.Less, "Speed", 0.1f);
        Transition(sJump, sIdle, AnimatorConditionMode.If, "Grounded", duration: 0.05f);

        var toJump = sm.AddAnyStateTransition(sJump);
        toJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
        toJump.hasExitTime = false;
        toJump.duration = 0.05f;
        toJump.canTransitionToSelf = false;

        AssetDatabase.SaveAssets();
        return controller;
    }

    static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, string param,
                           float threshold = 0f, float duration = 0.1f)
    {
        var t = from.AddTransition(to);
        t.AddCondition(mode, threshold, param);
        t.hasExitTime = false;
        t.duration = duration;
    }

    /// <summary>Tạo clip transform. Thuộc tính không liệt kê được giữ ở tư thế gốc để các state không "dính" tư thế của nhau.</summary>
    static AnimationClip CreateClip(string name, float length, bool loop, params (string property, float[] times, float[] values)[] curves)
    {
        var defaults = new Dictionary<string, float>
        {
            { "m_LocalPosition.x", 0f }, { "m_LocalPosition.y", 0f }, { "m_LocalPosition.z", 0f },
            { "m_LocalScale.x", 1f }, { "m_LocalScale.y", 1f }, { "m_LocalScale.z", 1f },
            { "localEulerAnglesRaw.x", 0f }, { "localEulerAnglesRaw.y", 0f }, { "localEulerAnglesRaw.z", 0f },
        };

        var clip = new AnimationClip { name = name, frameRate = 60f };
        foreach (var (property, defaultValue) in defaults)
        {
            AnimationCurve curve;
            var custom = curves.FirstOrDefault(c => c.property == property);
            if (custom.property != null)
            {
                curve = new AnimationCurve();
                for (int i = 0; i < custom.times.Length; i++) curve.AddKey(custom.times[i], custom.values[i]);
            }
            else
            {
                curve = AnimationCurve.Constant(0f, length, defaultValue);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), property), curve);
        }

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.stopTime = length;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, $"{AnimationFolder}/{name}.anim");
        return clip;
    }

    /// <summary>Ảnh placeholder trắng (ô vuông hoặc hình tròn) 1×1 unit, tô màu bằng SpriteRenderer.color.</summary>
    static Sprite EnsurePlaceholderSprite(string path, int pixels, bool circle)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;

        EnsureFolder(PlaceholderFolder);
        var tex = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false);
        var pixelData = new Color32[pixels * pixels];
        float r = pixels / 2f;
        for (int y = 0; y < pixels; y++)
        for (int x = 0; x < pixels; x++)
        {
            float alpha = 1f;
            if (circle)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                alpha = Mathf.Clamp01(r - d);   // viền mịn 1 pixel
            }
            pixelData[y * pixels + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
        }
        tex.SetPixels32(pixelData);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spritePixelsPerUnit = pixels;
        settings.spriteMeshType = SpriteMeshType.FullRect; // cần cho DrawMode Tiled
        settings.filterMode = circle ? FilterMode.Bilinear : FilterMode.Point;
        settings.mipmapEnabled = false;
        settings.wrapMode = TextureWrapMode.Clamp;
        settings.npotScale = TextureImporterNPOTScale.None;
        settings.alphaIsTransparency = true;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(StartScenePath, true),
            new EditorBuildSettingsScene(LevelScenePath, true),
        };
        scenes.AddRange(EditorBuildSettings.scenes.Where(s => s.path != StartScenePath && s.path != LevelScenePath));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    internal static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
