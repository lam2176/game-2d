using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static ZodiacLevelData;

/// <summary>
/// Dựng một scene màn chơi từ bản đồ ký tự trong <see cref="ZodiacLevelData"/>:
/// địa hình (gộp thành một CompositeCollider2D để nhân vật không vấp ở chỗ nối các khối), cỏ, bục tre một chiều,
/// gai, bục di động, đồng xu, đèn lồng, biển hướng dẫn, cổng đích, quái, đấu trường boss,
/// nền parallax theo chủ đề màn (làng quê / rừng trúc / hang động), người chơi, camera và HUD.
/// Ánh sáng 2D theo màn (trời chiều / rừng sương / hang tối + đèn lồng, lửa, ma trơi tự phát sáng),
/// hậu kỳ (bloom, vignette, chỉnh màu), hạt môi trường (lá rơi, đom đóm, bụi hang), nhạc nền.
/// </summary>
public static class ZodiacLevelBuilder
{
    public class Context
    {
        public Sprite square, circle;
        public ZodiacRoster roster;
        public ZodiacResources resources;
        public Material particleMaterial;
        public PhysicsMaterial2D noFriction;
        public ZodiacEnemyFactory.Prefabs prefabs;
        public int groundLayer, playerLayer, enemyLayer;
    }

    const string SceneFolder = "Assets/Zodiac/Scenes";
    /// <summary>Độ sâu kéo thêm xuống dưới đáy bản đồ cho các khối đất (unit).</summary>
    const float GroundDepthBelow = 8f;

    static readonly Color CoinColor = new Color(1f, 0.80f, 0.15f);
    static readonly Color WoodColor = new Color(0.45f, 0.28f, 0.15f);
    static readonly Color LanternColor = new Color(0.85f, 0.15f, 0.1f);
    static readonly Color GateRed = new Color(0.72f, 0.12f, 0.1f);
    static readonly Color RoofColor = new Color(0.25f, 0.2f, 0.25f);
    static readonly Color BarBackColor = new Color(0.15f, 0.1f, 0.1f, 0.85f);
    static readonly Color PlayerBarColor = new Color(0.85f, 0.15f, 0.15f);
    static readonly Color BossBarColor = new Color(0.55f, 0.85f, 1f);
    static readonly Color SlashColor = new Color(1f, 1f, 0.85f, 0.55f);

    static Context ctx;
    static LevelDef def;
    static char[,] grid;
    static int W, H;

    public static string ScenePath(LevelDef level) => $"{SceneFolder}/{level.sceneName}.unity";

    public static void Build(LevelDef level, Context context)
    {
        ctx = context;
        def = level;
        Parse();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = ZodiacPrototypeBuilder.CreateCamera(Vector3.zero, def.sky);

        BuildTerrain();
        BuildBackdrop();
        var coins = new GameObject("Collectibles").transform;
        var props = new GameObject("Props").transform;
        var enemies = new GameObject("Enemies").transform;
        Vector2 spawn = new Vector2(2.5f, 3f);
        PlayerTrigger goal = null;
        BossFoxSpirit boss = null;
        int checkpointIndex = 1, hintIndex = 0;
        var arenaGates = new List<int>();

        for (int x = 0; x < W; x++)
        for (int y = 0; y < H; y++)
        {
            Vector2 cell = new Vector2(x + 0.5f, y);
            switch (grid[x, y])
            {
                case 'o': CreateCoin(coins, new Vector2(x + 0.5f, y + 0.5f)); break;
                case 'P': spawn = cell + Vector2.up * 0.05f; break;
                case 'K': CreateCheckpoint(props, cell, checkpointIndex++); break;
                case 'G': goal = CreateGoal(props, cell); break;
                case 'i': CreateSign(props, cell, hintIndex < def.hints.Length ? def.hints[hintIndex++] : ""); break;
                case 'W': Spawn(ctx.prefabs.walker, enemies, cell + Vector2.up * 0.02f); break;
                case 'S': Spawn(ctx.prefabs.spiky, enemies, cell + Vector2.up * 0.02f); break;
                case 'H': Spawn(ctx.prefabs.hopper, enemies, cell + Vector2.up * 0.02f); break;
                case 'T': Spawn(ctx.prefabs.turret, enemies, cell); break;
                case 'F': Spawn(ctx.prefabs.flyer, enemies, new Vector2(x + 0.5f, y + 0.5f)); break;
                case 'X': boss = Spawn(ctx.prefabs.boss, enemies, cell + Vector2.up * 0.02f).GetComponent<BossFoxSpirit>(); break;
                case 'A': arenaGates.Add(x); break;
            }
        }
        BuildPlanks();
        BuildSpikes();
        BuildMovers();

        var killZone = new GameObject("KillZone");
        killZone.transform.position = new Vector2(W / 2f, -4f);
        killZone.AddComponent<BoxCollider2D>().size = new Vector2(W + 20f, 2f);
        var killTrigger = killZone.AddComponent<PlayerTrigger>();

        var player = CreatePlayer(spawn);
        var follow = cam.gameObject.AddComponent<ZodiacCameraFollow>();
        follow.target = player.transform;
        follow.bounds = new Rect(-1f, -3f, W + 2f, H + (def.backdrop == Backdrop.Cave ? 3f : 6f));
        cam.transform.position = new Vector3(spawn.x, spawn.y + 2f, -10f);

        var manager = new GameObject("GameManager").AddComponent<ZodiacGameManager>();
        manager.player = player;
        manager.goal = goal;
        manager.killZone = killTrigger;
        manager.nextSceneName = def.nextScene;
        manager.levelTitle = def.title;
        manager.fallLimitY = -6f;
        manager.boss = boss;
        manager.music = ZodiacVisualSetup.Clip(MusicFor(def.backdrop));
        manager.stoneFootsteps = def.backdrop == Backdrop.Cave;
        var canvas = BuildHud(manager, player);
        if (boss != null && arenaGates.Count > 0) BuildArena(boss, arenaGates[0], canvas);

        BuildLighting(cam, player);
        BuildAmbientParticles(cam);
        ApplySpriteMaterials();

        ZodiacPrototypeBuilder.EnsureFolder(SceneFolder);
        EditorSceneManager.SaveScene(scene, ScenePath(def));
    }

    // ───────────────────────────── Bản đồ ─────────────────────────────

    static void Parse()
    {
        H = def.rows.Length;
        W = 0;
        foreach (var r in def.rows) W = Mathf.Max(W, r.Length);
        grid = new char[W, H];
        for (int r = 0; r < H; r++)
        for (int x = 0; x < W; x++)
            grid[x, H - 1 - r] = x < def.rows[r].Length ? def.rows[r][x] : ' ';
    }

    static char At(int x, int y) => x < 0 || y < 0 || x >= W || y >= H ? ' ' : grid[x, y];
    static bool Solid(int x, int y) => At(x, y) == '#' || At(x, y) == 'B';

    /// <summary>Ghép các ô cùng loại thành hình chữ nhật lớn nhất có thể (quét theo hàng rồi kéo xuống).</summary>
    static List<RectInt> MergeRects(char ch)
    {
        var used = new bool[W, H];
        var rects = new List<RectInt>();
        for (int y = H - 1; y >= 0; y--)
        for (int x = 0; x < W; x++)
        {
            if (used[x, y] || grid[x, y] != ch) continue;
            int x1 = x;
            while (x1 + 1 < W && grid[x1 + 1, y] == ch && !used[x1 + 1, y]) x1++;
            int y0 = y;
            while (y0 - 1 >= 0 && RowMatches(ch, x, x1, y0 - 1, used)) y0--;
            for (int ix = x; ix <= x1; ix++)
            for (int iy = y0; iy <= y; iy++)
                used[ix, iy] = true;
            rects.Add(new RectInt(x, y0, x1 - x + 1, y - y0 + 1));
        }
        return rects;
    }

    static bool RowMatches(char ch, int x0, int x1, int y, bool[,] used)
    {
        for (int x = x0; x <= x1; x++)
            if (grid[x, y] != ch || used[x, y]) return false;
        return true;
    }

    // ───────────────────────────── Địa hình ─────────────────────────────

    static void BuildTerrain()
    {
        var level = new GameObject("Terrain") { layer = ctx.groundLayer };
        var rb = level.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        var composite = level.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.sharedMaterial = ctx.noFriction;

        foreach (char ch in new[] { '#', 'B' })
        {
            Color body = ch == '#' ? def.soil : def.stone;
            foreach (var r in MergeRects(ch))
            {
                // Khối chạm đáy bản đồ kéo sâu xuống để camera không nhìn thấy "đáy" mặt đất (vực vẫn thủng)
                float bottom = r.y == 0 ? r.y - GroundDepthBelow : r.y;
                float height = r.yMax - bottom;
                var go = Block($"{(ch == '#' ? "Ground" : "Stone")} {r.x},{r.y}", level.transform,
                               new Vector2(r.x + r.width / 2f, bottom + height / 2f), new Vector2(r.width, height), body, 0);
                if (ch == 'B')
                    for (int i = 1; i < height; i++)   // mạch vữa giữa các hàng đá
                        ZodiacPrototypeBuilder.CreateDecor("Mortar", go.transform, ctx.square, new Vector2(0f, -height / 2f + i),
                                                           new Vector2(r.width, 0.06f), Color.Lerp(def.stone, Color.black, 0.3f), 1);
            }
        }

        // Cỏ (đất) hoặc gờ sáng (đá) trên mọi mặt trống phía trên.
        for (int y = 0; y < H; y++)
        {
            int x = 0;
            while (x < W)
            {
                if (!Solid(x, y) || Solid(x, y + 1)) { x++; continue; }
                char ch = grid[x, y];
                int x1 = x;
                while (x1 + 1 < W && grid[x1 + 1, y] == ch && !Solid(x1 + 1, y + 1)) x1++;
                float w = x1 - x + 1;
                if (ch == '#')
                {
                    Decor(level.transform, "Grass", new Vector2(x + w / 2f, y + 0.88f), new Vector2(w, 0.24f), def.grass, 2);
                    Decor(level.transform, "GrassLip", new Vector2(x + w / 2f, y + 0.99f), new Vector2(w, 0.06f),
                          Color.Lerp(def.grass, Color.white, 0.3f), 2);
                }
                else
                {
                    Decor(level.transform, "StoneCap", new Vector2(x + w / 2f, y + 0.94f), new Vector2(w, 0.12f),
                          Color.Lerp(def.stone, Color.white, 0.25f), 2);
                }
                x = x1 + 1;
            }
        }

        // Tường biên trái/phải.
        float wallH = H + 12f;
        Block("Wall Left", level.transform, new Vector2(-0.5f, wallH / 2f - 4f), new Vector2(1f, wallH), Color.Lerp(def.soil, Color.black, 0.3f), 0);
        Block("Wall Right", level.transform, new Vector2(W + 0.5f, wallH / 2f - 4f), new Vector2(1f, wallH), Color.Lerp(def.soil, Color.black, 0.3f), 0);
    }

    static GameObject Block(string name, Transform parent, Vector2 center, Vector2 size, Color color, int order)
    {
        var go = Decor(parent, name, center, size, color, order);
        go.layer = ctx.groundLayer;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.compositeOperation = Collider2D.CompositeOperation.Merge;
        return go;
    }

    static GameObject Decor(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order)
    {
        var go = ZodiacPrototypeBuilder.CreateDecor(name, parent, ctx.square, Vector2.zero, size, color, order);
        go.transform.position = position;
        return go;
    }

    static GameObject Circle(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ctx.circle;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    static GameObject Diamond(Transform parent, string name, Vector2 position, float size, Color color, int order)
    {
        var go = Decor(parent, name, position, Vector2.one * size, color, order);
        go.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        return go;
    }

    /// <summary>Bục tre một chiều: PlatformEffector2D, nhảy xuyên từ dưới lên.</summary>
    static void BuildPlanks()
    {
        var root = new GameObject("Planks").transform;
        foreach (var r in MergeRects('='))
        {
            var go = new GameObject($"Plank {r.x},{r.y}") { layer = ctx.groundLayer };
            go.transform.SetParent(root, false);
            go.transform.position = new Vector2(r.x + r.width / 2f, r.y + r.height - 0.15f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(r.width, 0.3f);
            box.usedByEffector = true;
            box.sharedMaterial = ctx.noFriction;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
            PlankVisual(go.transform, r.width);
        }
    }

    static void PlankVisual(Transform t, float width)
    {
        ZodiacPrototypeBuilder.CreateDecor("Bamboo", t, ctx.square, Vector2.zero, new Vector2(width, 0.3f), def.plank, 3);
        ZodiacPrototypeBuilder.CreateDecor("Shade", t, ctx.square, new Vector2(0f, -0.11f), new Vector2(width, 0.08f),
                                           Color.Lerp(def.plank, Color.black, 0.35f), 3);
        for (float x = -width / 2f + 0.5f; x < width / 2f; x += 1f)   // đốt tre
            ZodiacPrototypeBuilder.CreateDecor("Node", t, ctx.square, new Vector2(x + 0.5f, 0f), new Vector2(0.06f, 0.3f),
                                               Color.Lerp(def.plank, Color.black, 0.45f), 4);
    }

    static void BuildSpikes()
    {
        var root = new GameObject("Spikes").transform;
        foreach (var r in MergeRects('^'))
        {
            var go = new GameObject($"Spikes {r.x},{r.y}");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector2(r.x + r.width / 2f, r.y + 0.25f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(r.width - 0.2f, 0.45f);
            go.AddComponent<Hazard>();
            for (float x = r.x + 0.25f; x < r.x + r.width; x += 0.5f)   // nửa dưới khuất sau mặt đất
                Diamond(go.transform, "Spike", new Vector2(x, r.y + 0.02f), 0.42f, new Color(0.85f, 0.85f, 0.9f), -1);
        }
    }

    /// <summary>Bục di động: 'M' (ngang, đi theo số '-' phía sau) hoặc 'V' (dọc, đi theo số '|' phía trên).</summary>
    static void BuildMovers()
    {
        var root = new GameObject("MovingPlatforms").transform;
        foreach (char ch in new[] { 'M', 'V' })
        foreach (var r in MergeRects(ch))
        {
            int travel = 0;
            if (ch == 'M') while (At(r.xMax + travel, r.y) == '-') travel++;
            else while (At(r.x, r.yMax + travel) == '|') travel++;

            var go = new GameObject($"Mover {r.x},{r.y}") { layer = ctx.groundLayer };
            go.transform.SetParent(root, false);
            go.transform.position = new Vector2(r.x + r.width / 2f, r.y + 0.8f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(r.width, 0.4f);
            box.usedByEffector = true;
            box.sharedMaterial = ctx.noFriction;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
            var mover = go.AddComponent<MovingPlatform>();
            mover.travel = ch == 'M' ? new Vector2(travel, 0f) : new Vector2(0f, travel);
            mover.duration = Mathf.Max(1.5f, travel / 2.5f);
            mover.phase = (r.x % 7) / 7f;
            ZodiacPrototypeBuilder.CreateDecor("Raft", go.transform, ctx.square, Vector2.zero, new Vector2(r.width, 0.4f),
                                               Color.Lerp(def.plank, WoodColor, 0.5f), 3);
            for (float x = -r.width / 2f + 0.25f; x < r.width / 2f; x += 0.5f)
                ZodiacPrototypeBuilder.CreateDecor("Rope", go.transform, ctx.square, new Vector2(x + 0.25f, 0f), new Vector2(0.05f, 0.4f),
                                                   Color.Lerp(def.plank, Color.black, 0.5f), 4);
        }
    }

    // ───────────────────────────── Vật phẩm, đèn lồng, biển, cổng ─────────────────────────────

    static void CreateCoin(Transform parent, Vector2 position)
    {
        var go = new GameObject("Coin");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.6f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ctx.circle;
        sr.color = CoinColor;
        sr.sortingOrder = 5;
        // Lỗ vuông giữa đồng xu (tiền xu cổ)
        var hole = ZodiacPrototypeBuilder.CreateDecor("Hole", go.transform, ctx.square, Vector2.zero, new Vector2(0.3f, 0.3f),
                                                      new Color(0.7f, 0.5f, 0.1f), 6);
        hole.transform.localScale = Vector3.one;
        go.AddComponent<CircleCollider2D>().radius = 0.5f;
        go.AddComponent<Collectible>();
    }

    static void CreateCheckpoint(Transform parent, Vector2 basePosition, int index)
    {
        var go = new GameObject($"Checkpoint {index}");
        go.transform.SetParent(parent, false);
        go.transform.position = basePosition;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.2f, 3f);
        box.offset = new Vector2(0f, 1.5f);
        var cp = go.AddComponent<Checkpoint>();
        cp.index = index;

        ZodiacPrototypeBuilder.CreateDecor("Pole", go.transform, ctx.square, new Vector2(0f, 1.25f), new Vector2(0.12f, 2.5f), WoodColor, 3);
        ZodiacPrototypeBuilder.CreateDecor("Arm", go.transform, ctx.square, new Vector2(0.3f, 2.45f), new Vector2(0.6f, 0.08f), WoodColor, 3);
        var glow = Circle(go.transform, "Glow", basePosition + new Vector2(0.55f, 1.85f), Vector2.one * 1.2f, new Color(1f, 0.6f, 0.2f, 0.35f), 3);
        var lamp = Circle(go.transform, "Lantern", basePosition + new Vector2(0.55f, 1.9f), new Vector2(0.55f, 0.7f), LanternColor, 4);
        ZodiacPrototypeBuilder.CreateDecor("Cap", lamp.transform.parent, ctx.square, new Vector2(0.55f, 2.28f), new Vector2(0.35f, 0.08f),
                                           new Color(0.9f, 0.75f, 0.2f), 5);
        ZodiacPrototypeBuilder.CreateDecor("Tassel", go.transform, ctx.square, new Vector2(0.55f, 1.42f), new Vector2(0.06f, 0.25f),
                                           new Color(0.9f, 0.75f, 0.2f), 5);
        cp.lamp = lamp.GetComponent<SpriteRenderer>();
        cp.glow = glow;
        Unlit(glow);
        Unlit(lamp);
        cp.lampLight = ZodiacLights.AddPoint(lamp, new Color(1f, 0.6f, 0.3f), 5f, 0.35f);
    }

    /// <summary>Cổng đích: cổng làng (màn ngoài trời) hoặc cổng đền (hang động) — hai cột đỏ, mái ngói cong.</summary>
    static PlayerTrigger CreateGoal(Transform parent, Vector2 basePosition)
    {
        var go = new GameObject("Goal");
        go.transform.SetParent(parent, false);
        go.transform.position = basePosition;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(2f, 4f);
        box.offset = new Vector2(0f, 2f);
        var trigger = go.AddComponent<PlayerTrigger>();

        var t = go.transform;
        ZodiacPrototypeBuilder.CreateDecor("PillarLeft", t, ctx.square, new Vector2(-1.3f, 1.9f), new Vector2(0.4f, 3.8f), GateRed, 2);
        ZodiacPrototypeBuilder.CreateDecor("PillarRight", t, ctx.square, new Vector2(1.3f, 1.9f), new Vector2(0.4f, 3.8f), GateRed, 2);
        ZodiacPrototypeBuilder.CreateDecor("Beam", t, ctx.square, new Vector2(0f, 3.5f), new Vector2(3.2f, 0.3f), GateRed, 2);
        ZodiacPrototypeBuilder.CreateDecor("Plaque", t, ctx.square, new Vector2(0f, 3.95f), new Vector2(1.3f, 0.55f), new Color(0.95f, 0.8f, 0.3f), 3);
        ZodiacPrototypeBuilder.CreateDecor("Roof", t, ctx.square, new Vector2(0f, 4.45f), new Vector2(4f, 0.4f), RoofColor, 3);
        var eaveL = ZodiacPrototypeBuilder.CreateDecor("EaveLeft", t, ctx.square, new Vector2(-2.05f, 4.6f), new Vector2(0.6f, 0.2f), RoofColor, 3);
        eaveL.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
        var eaveR = ZodiacPrototypeBuilder.CreateDecor("EaveRight", t, ctx.square, new Vector2(2.05f, 4.6f), new Vector2(0.6f, 0.2f), RoofColor, 3);
        eaveR.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
        return trigger;
    }

    /// <summary>Biển gỗ: chữ hiện trong khung (Canvas world-space) khi người chơi đứng gần.</summary>
    static void CreateSign(Transform parent, Vector2 basePosition, string message)
    {
        var go = new GameObject("Sign");
        go.transform.SetParent(parent, false);
        go.transform.position = basePosition;
        ZodiacPrototypeBuilder.CreateDecor("Post", go.transform, ctx.square, new Vector2(0f, 0.5f), new Vector2(0.15f, 1f), WoodColor, 3);
        ZodiacPrototypeBuilder.CreateDecor("Board", go.transform, ctx.square, new Vector2(0f, 1.1f), new Vector2(0.9f, 0.6f),
                                           new Color(0.75f, 0.55f, 0.3f), 4);
        ZodiacPrototypeBuilder.CreateDecor("Mark", go.transform, ctx.square, new Vector2(0f, 1.1f), new Vector2(0.1f, 0.35f), WoodColor, 5);

        var canvasGo = new GameObject("Bubble", typeof(RectTransform));
        canvasGo.transform.SetParent(go.transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 30;
        var rt = (RectTransform)canvasGo.transform;
        rt.sizeDelta = new Vector2(1000f, 210f);
        rt.localScale = Vector3.one * 0.01f;
        rt.localPosition = new Vector3(0f, 3.1f, 0f);
        var back = ZodiacPrototypeBuilder.CreateImage(canvasGo.transform, "Back", new Color(0.1f, 0.07f, 0.05f, 0.85f));
        ZodiacPrototypeBuilder.Stretch(back.rectTransform);
        var text = ZodiacPrototypeBuilder.CreateText(canvasGo.transform, "Text", message, 40, Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 200f), TextAnchor.MiddleCenter);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;

        var sign = go.AddComponent<HintSign>();
        sign.message = message;
        sign.bubble = canvasGo;
        sign.text = text;
    }

    static GameObject Spawn(GameObject prefab, Transform parent, Vector2 position)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = position;
        return go;
    }

    // ───────────────────────────── Boss ─────────────────────────────

    static void BuildArena(BossFoxSpirit boss, int gateX, Transform canvas)
    {
        int floor = 0;
        while (floor < H && Solid(gateX, floor)) floor++;
        int ceiling = floor;
        while (ceiling < H && !Solid(gateX, ceiling)) ceiling++;

        var gate = new GameObject("ArenaGate") { layer = ctx.groundLayer };
        gate.transform.position = new Vector2(gateX + 0.5f, (floor + ceiling) / 2f);
        gate.AddComponent<BoxCollider2D>().size = new Vector2(1f, ceiling - floor);
        ZodiacPrototypeBuilder.CreateDecor("Bars", gate.transform, ctx.square, Vector2.zero, new Vector2(1f, ceiling - floor),
                                           Color.Lerp(def.stone, Color.black, 0.4f), 6);
        for (float y = -(ceiling - floor) / 2f + 0.5f; y < (ceiling - floor) / 2f; y += 1.2f)
            ZodiacPrototypeBuilder.CreateDecor("Rune", gate.transform, ctx.square, new Vector2(0f, y), new Vector2(0.3f, 0.3f),
                                               new Color(0.6f, 0.9f, 1f), 7).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        foreach (Transform rune in gate.transform) if (rune.name == "Rune") Unlit(rune.gameObject);
        ZodiacLights.AddPoint(gate, new Color(0.5f, 0.85f, 1f), 4f, 0.8f);

        var arenaGo = new GameObject("BossArena");
        float left = gateX + 3f;
        arenaGo.transform.position = new Vector2((left + W) / 2f, (floor + ceiling) / 2f);
        arenaGo.AddComponent<BoxCollider2D>().size = new Vector2(W - left, ceiling - floor);
        var arena = arenaGo.AddComponent<BossArena>();
        arena.boss = boss;
        arena.gates = new[] { gate };
        arena.cameraBounds = new Rect(gateX - 0.5f, -3f, W - gateX + 1.5f, H + 3f);
        arena.bossMusic = ZodiacVisualSetup.Clip("Music/music_boss");

        // Thanh máu boss trên HUD
        var bar = ZodiacPrototypeBuilder.CreateImage(canvas, "BossBar", BarBackColor);
        bar.rectTransform.pivot = new Vector2(0.5f, 0f);
        ZodiacPrototypeBuilder.Place(bar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(900f, 34f));
        bar.raycastTarget = false;
        var fill = ZodiacPrototypeBuilder.CreateImage(bar.transform, "Fill", BossBarColor);
        ZodiacPrototypeBuilder.Stretch(fill.rectTransform);
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        fill.rectTransform.offsetMin = new Vector2(4f, 4f);
        fill.rectTransform.offsetMax = new Vector2(-4f, -4f);
        fill.raycastTarget = false;
        var name = ZodiacPrototypeBuilder.CreateText(bar.transform, "Name", boss.bossName, 34, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0f, 46f), new Vector2(900f, 50f), TextAnchor.LowerCenter);
        var healthBar = bar.gameObject.AddComponent<HealthBar>();
        healthBar.health = boss.GetComponent<Health>();
        healthBar.fill = fill.rectTransform;
        arena.bossBar = bar.gameObject;
        arena.bossNameText = name;
        bar.transform.SetSiblingIndex(1);
    }

    // ───────────────────────────── Ánh sáng, hậu kỳ, hạt môi trường ─────────────────────────────

    static string MusicFor(Backdrop b) => b switch
    {
        Backdrop.Village => "Music/music_village",
        Backdrop.Bamboo => "Music/music_bamboo",
        _ => "Music/music_cave",
    };

    static void BuildLighting(Camera cam, ZodiacPlayerController player)
    {
        var lights = new GameObject("Lighting");
        // Ánh sáng toàn cục: chiều vàng ấm (làng), xanh sương (rừng), tối tím (hang)
        switch (def.backdrop)
        {
            case Backdrop.Village: ZodiacLights.AddGlobal(lights, new Color(1f, 0.93f, 0.82f), 1f); break;
            case Backdrop.Bamboo: ZodiacLights.AddGlobal(lights, new Color(0.8f, 0.95f, 0.88f), 0.78f); break;
            default: ZodiacLights.AddGlobal(lights, new Color(0.55f, 0.5f, 0.85f), 0.32f); break;
        }

        // Hậu kỳ
        cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        var volume = new GameObject("PostProcess").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = def.backdrop switch
        {
            Backdrop.Village => ZodiacVisualSetup.EnsureVolumeProfile("Village", 0.5f, 0.22f, 8f, 6f, new Color(1f, 0.97f, 0.9f)),
            Backdrop.Bamboo => ZodiacVisualSetup.EnsureVolumeProfile("Bamboo", 0.7f, 0.3f, -8f, 4f, new Color(0.92f, 1f, 0.95f)),
            _ => ZodiacVisualSetup.EnsureVolumeProfile("Cave", 1.1f, 0.4f, -5f, 12f, new Color(0.92f, 0.9f, 1f)),
        };
    }

    /// <summary>Hạt môi trường bám theo camera: lá vàng bay (làng), đom đóm (rừng trúc), bụi lơ lửng (hang).</summary>
    static void BuildAmbientParticles(Camera cam)
    {
        var go = new GameObject("AmbientParticles");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 10f);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 120;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(26f, 14f, 1f);
        var emission = ps.emission;
        var noise = ps.noise;
        noise.enabled = true;
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = fade;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;

        switch (def.backdrop)
        {
            case Backdrop.Village:   // lá/hoa vàng bay theo gió
                emission.rateOverTime = 4f;
                main.startLifetime = 8f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.3f, 0.85f), new Color(0.95f, 0.55f, 0.3f, 0.85f));
                vel.x = new ParticleSystem.MinMaxCurve(-1.5f, -0.6f);
                vel.y = new ParticleSystem.MinMaxCurve(-0.8f, -0.3f);
                vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                noise.strength = 0.6f;
                noise.frequency = 0.3f;
                break;
            case Backdrop.Bamboo:    // đom đóm
                emission.rateOverTime = 6f;
                main.startLifetime = 6f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 1f, 0.4f, 1f), new Color(1f, 0.95f, 0.5f, 1f));
                vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.3f);
                vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                noise.strength = 1f;
                noise.frequency = 0.5f;
                break;
            default:                 // bụi hang
                emission.rateOverTime = 8f;
                main.startLifetime = 7f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 0.7f, 1f, 0.5f), new Color(1f, 1f, 1f, 0.4f));
                vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
                vel.y = new ParticleSystem.MinMaxCurve(-0.15f, 0.05f);
                vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                noise.strength = 0.3f;
                noise.frequency = 0.4f;
                break;
        }
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = ctx.particleMaterial;
        renderer.sortingOrder = 25;
    }

    static void Unlit(GameObject go)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
    }

    /// <summary>Mọi sprite trong scene (trừ thứ tự phát sáng và prefab quái — đã đặt sẵn) dùng vật liệu chịu ánh sáng 2D.</summary>
    static void ApplySpriteMaterials()
    {
        var lit = ZodiacVisualSetup.LitMaterial;
        var unlit = ZodiacVisualSetup.UnlitMaterial;
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
        {
            if (sr.sharedMaterial == unlit || PrefabUtility.IsPartOfPrefabInstance(sr)) continue;
            sr.sharedMaterial = lit;
        }
    }

    // ───────────────────────────── Nền parallax ─────────────────────────────

    static void BuildBackdrop()
    {
        var root = new GameObject("Backdrop").transform;
        var rnd = new System.Random(def.sceneName.GetHashCode());
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        Transform Layer(string name, float fx, float fy)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            var p = t.gameObject.AddComponent<ParallaxLayer>();
            p.factorX = fx;
            p.factorY = fy;
            return t;
        }
        float span = W + 40f;

        switch (def.backdrop)
        {
            case Backdrop.Village:
            {
                var sky = Layer("Sun", 0.95f, 0.9f);
                Unlit(Circle(sky, "Sun", new Vector2(14f, 11f), Vector2.one * 3.2f, new Color(1f, 0.75f, 0.45f), -60));
                Unlit(Circle(sky, "SunGlow", new Vector2(14f, 11f), Vector2.one * 5f, new Color(1f, 0.85f, 0.6f, 0.4f), -61));
                var clouds = Layer("Clouds", 0.85f, 0.8f);
                for (float x = -10f; x < span * 0.3f; x += R(9f, 15f))
                    for (int i = 0; i < 3; i++)
                        Circle(clouds, "Cloud", new Vector2(x + i * 1.2f, R(10f, 13f)), new Vector2(R(2.2f, 3.2f), R(1f, 1.4f)),
                               new Color(1f, 0.97f, 0.92f, 0.85f), -55);
                var mountains = Layer("Mountains", 0.75f, 0.6f);
                for (float x = -10f; x < span * 0.4f; x += R(6f, 10f))
                {
                    float s = R(8f, 13f);
                    Diamond(mountains, "Mountain", new Vector2(x, R(0f, 1.5f)), s, def.far, -50);
                }
                var hills = Layer("Hills", 0.5f, 0.35f);
                for (float x = -10f; x < span * 0.6f; x += R(5f, 8f))
                    Circle(hills, "Hill", new Vector2(x, R(0f, 1f)), new Vector2(R(9f, 14f), R(5f, 7f)), def.mid, -45);
                var fields = Layer("RiceFields", 0.3f, 0.2f);
                for (int i = 0; i < 3; i++)
                    Decor(fields, "Field", new Vector2(span * 0.4f, 1.6f - i * 0.6f), new Vector2(span, 0.35f),
                          Color.Lerp(new Color(0.75f, 0.8f, 0.35f), def.mid, i * 0.3f), -40);
                var bamboo = Layer("BambooGroves", 0.2f, 0.1f);
                for (float x = 0f; x < span * 0.85f; x += R(10f, 18f))
                    BambooClump(bamboo, x, 2.5f, R(5f, 8f), Color.Lerp(def.mid, Color.black, 0.25f), -35, rnd);
                break;
            }
            case Backdrop.Bamboo:
            {
                var mist = Layer("Mist", 0.9f, 0.85f);
                for (int i = 0; i < 4; i++)
                    Decor(mist, "MistBand", new Vector2(span * 0.3f, 6f + i * 4f), new Vector2(span, 1.5f), new Color(1f, 1f, 1f, 0.25f), -58);
                Color[] tints = { def.far, Color.Lerp(def.far, def.mid, 0.5f), def.mid };
                float[] factors = { 0.75f, 0.55f, 0.35f };
                for (int l = 0; l < 3; l++)
                {
                    var layer = Layer($"Bamboo {l + 1}", factors[l], factors[l] * 0.6f);
                    for (float x = -10f; x < span * (1f - factors[l]) + 30f; x += R(1.2f, 3f))
                        BambooStalk(layer, x, H + 8f, R(0.25f, 0.45f) * (1f + l * 0.3f), tints[l], -55 + l * 6);
                }
                break;
            }
            case Backdrop.Cave:
            {
                var back = Layer("CaveWall", 0.8f, 0.7f);
                for (float x = -10f; x < span * 0.3f; x += R(3f, 6f))
                    Decor(back, "Column", new Vector2(x, H / 2f), new Vector2(R(1.5f, 3f), H + 10f), def.far, -55);
                var crystals = Layer("Crystals", 0.55f, 0.45f);
                for (float x = -10f; x < span * 0.5f; x += R(4f, 8f))
                {
                    float y = R(3f, H - 4f);
                    var crystal = Diamond(crystals, "Crystal", new Vector2(x, y), R(0.5f, 0.9f), new Color(0.7f, 0.55f, 1f), -50);
                    Unlit(crystal);
                    ZodiacLights.AddPoint(crystal, new Color(0.6f, 0.45f, 1f), 3f, 0.35f);
                }
                var drips = Layer("Stalactites", 0.35f, 0.25f);
                for (float x = -10f; x < span * 0.7f; x += R(2f, 5f))
                    Diamond(drips, "Stalactite", new Vector2(x, H - 3f + R(-0.5f, 0.5f)), R(1f, 2f), def.mid, -45);
                for (float x = -10f; x < span * 0.7f; x += R(3f, 7f))
                    Diamond(drips, "Stalagmite", new Vector2(x, R(0.5f, 1.5f)), R(1.2f, 2.4f), def.mid, -45);
                break;
            }
        }
    }

    static void BambooStalk(Transform parent, float x, float height, float width, Color color, int order)
    {
        var stalk = Decor(parent, "Stalk", new Vector2(x, height / 2f - 4f), new Vector2(width, height), color, order);
        for (float y = 0.5f; y < height; y += 1.6f)
            ZodiacPrototypeBuilder.CreateDecor("Node", stalk.transform, ctx.square, new Vector2(0f, -height / 2f + y), new Vector2(width * 1.3f, 0.08f),
                                               Color.Lerp(color, Color.black, 0.25f), order + 1);
    }

    static void BambooClump(Transform parent, float x, float baseY, float height, Color color, int order, System.Random rnd)
    {
        for (int i = 0; i < 5; i++)
        {
            float h = height * (0.7f + (float)rnd.NextDouble() * 0.5f);
            var stalk = Decor(parent, "Bamboo", new Vector2(x + i * 0.55f, baseY + h / 2f - 1f), new Vector2(0.22f, h), color, order);
            stalk.transform.rotation = Quaternion.Euler(0f, 0f, (i - 2) * 4f);
            Circle(parent, "Leaves", new Vector2(x + i * 0.55f + (i - 2) * 0.3f, baseY + h - 1f), new Vector2(1.6f, 0.9f),
                   Color.Lerp(color, Color.white, 0.1f), order);
        }
    }

    // ───────────────────────────── Người chơi ─────────────────────────────

    static ZodiacPlayerController CreatePlayer(Vector2 spawn)
    {
        var fallback = ctx.roster.Get(ZodiacSelection.Index);
        var go = new GameObject("Player") { layer = ctx.playerLayer };
        go.transform.position = spawn;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;   // ZodiacPlayerController.gravityScale ghi đè khi vào game
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var body = go.AddComponent<CapsuleCollider2D>();
        body.direction = CapsuleDirection2D.Vertical;
        body.size = fallback.colliderSize;
        body.offset = fallback.colliderOffset;
        body.sharedMaterial = ctx.noFriction;

        var health = go.AddComponent<Health>();
        health.maxHealth = fallback.maxHealth;
        health.invulnerableTime = 0.8f;

        // Player (vật lý) → Facing (lật trái/phải) → Visual (ảnh + CharacterAnimator co giãn/nghiêng bằng code)
        var facing = new GameObject("Facing").transform;
        facing.SetParent(go.transform, false);
        var visual = new GameObject("Visual");
        visual.transform.SetParent(facing, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = fallback.sprite;
        sr.sortingOrder = 10;

        var flash = go.AddComponent<HitFlash>();
        flash.renderers = new[] { sr };
        flash.flashColor = new Color(1f, 0.35f, 0.35f);

        var pc = go.AddComponent<ZodiacPlayerController>();
        pc.roster = ctx.roster;
        pc.character = fallback;
        pc.groundLayer = 1 << ctx.groundLayer;
        pc.facing = facing;
        pc.spriteRenderer = sr;

        // Hitbox là con của Facing → tự lật theo hướng nhìn. Để tắt: script chỉ dùng hình dạng để quét.
        var hitboxGo = new GameObject("AttackHitbox");
        hitboxGo.transform.SetParent(facing, false);
        hitboxGo.transform.localPosition = new Vector3(0.85f, 0.9f, 0f);
        var hitbox = hitboxGo.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.size = new Vector2(1.1f, 1.2f);
        hitbox.enabled = false;
        var slash = new GameObject("SlashEffect");
        slash.transform.SetParent(hitboxGo.transform, false);
        slash.transform.localScale = new Vector3(hitbox.size.x, hitbox.size.y, 1f);
        var slashSr = slash.AddComponent<SpriteRenderer>();
        slashSr.sprite = ctx.circle;   // PlayerMeleeAttack đổi sang hình lưỡi liềm khi chạy
        slashSr.color = SlashColor;
        slashSr.sortingOrder = 11;
        slash.AddComponent<SlashEffect>();
        Unlit(slash);
        slash.SetActive(false);

        var attack = go.AddComponent<PlayerMeleeAttack>();
        attack.hitbox = hitbox;
        attack.slashEffect = slash;
        attack.targetLayers = 1 << ctx.enemyLayer;

        var skill = go.AddComponent<PlayerSkill>();
        skill.targetLayers = 1 << ctx.enemyLayer;
        skill.groundLayer = 1 << ctx.groundLayer;
        visual.AddComponent<CharacterAnimator>();   // sau attack/skill để đăng ký sự kiện
        if (def.backdrop == Backdrop.Cave)          // đèn quanh người trong hang tối
        {
            var aura = new GameObject("Aura");
            aura.transform.SetParent(go.transform, false);
            aura.transform.localPosition = Vector3.up;
            ZodiacLights.AddPoint(aura, new Color(1f, 0.9f, 0.75f), 6f, 0.75f, 0.8f);
        }
        return pc;
    }

    // ───────────────────────────── HUD ─────────────────────────────

    static Transform BuildHud(ZodiacGameManager manager, ZodiacPlayerController player)
    {
        var canvas = ZodiacPrototypeBuilder.CreateCanvas();

        // Góc trái trên: ô chiêu + chân dung, tên, máu, đồng xu
        var frame = ZodiacPrototypeBuilder.CreateImage(canvas, "SkillFrame", Color.white);
        frame.rectTransform.pivot = new Vector2(0f, 1f);
        ZodiacPrototypeBuilder.Place(frame.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(150f, 150f));
        frame.raycastTarget = false;
        var slotBack = ZodiacPrototypeBuilder.CreateImage(frame.transform, "Back", new Color(0.12f, 0.08f, 0.06f, 0.9f));
        ZodiacPrototypeBuilder.Stretch(slotBack.rectTransform);
        slotBack.rectTransform.offsetMin = new Vector2(6f, 6f);
        slotBack.rectTransform.offsetMax = new Vector2(-6f, -6f);
        var icon = ZodiacPrototypeBuilder.CreateImage(slotBack.transform, "Portrait", Color.white);
        ZodiacPrototypeBuilder.Stretch(icon.rectTransform);
        icon.rectTransform.offsetMin = new Vector2(6f, 6f);
        icon.rectTransform.offsetMax = new Vector2(-6f, -6f);
        icon.preserveAspect = true;
        var cooldown = ZodiacPrototypeBuilder.CreateImage(slotBack.transform, "Cooldown", new Color(0f, 0f, 0f, 0.65f));
        ZodiacPrototypeBuilder.Stretch(cooldown.rectTransform);
        cooldown.rectTransform.pivot = new Vector2(0.5f, 0f);
        var cdText = ZodiacPrototypeBuilder.CreateText(slotBack.transform, "CooldownText", "", 44, Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 60f), TextAnchor.MiddleCenter);

        var characterText = ZodiacPrototypeBuilder.CreateText(canvas, "CharacterText", "", 32, Color.white,
            new Vector2(0f, 1f), new Vector2(190f, -22f), new Vector2(700f, 44f), TextAnchor.UpperLeft);
        var skillName = ZodiacPrototypeBuilder.CreateText(canvas, "SkillName", "", 26, new Color(1f, 0.92f, 0.6f),
            new Vector2(0f, 1f), new Vector2(190f, -118f), new Vector2(700f, 36f), TextAnchor.UpperLeft);
        skillName.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        var passive = ZodiacPrototypeBuilder.CreateText(canvas, "PassiveText", "", 22, Color.white,
            new Vector2(0f, 1f), new Vector2(190f, -150f), new Vector2(700f, 32f), TextAnchor.UpperLeft);

        var hud = canvas.gameObject.AddComponent<SkillHud>();
        hud.skill = player.GetComponent<PlayerSkill>();
        hud.icon = icon;
        hud.cooldownFill = cooldown.rectTransform;
        hud.cooldownText = cdText;
        hud.nameText = skillName;
        hud.passiveText = passive;
        hud.characterText = characterText;
        hud.readyFrame = frame;

        // Thanh máu
        var back = ZodiacPrototypeBuilder.CreateImage(canvas, "PlayerHealthBar", BarBackColor);
        back.rectTransform.pivot = new Vector2(0f, 1f);
        ZodiacPrototypeBuilder.Place(back.rectTransform, new Vector2(0f, 1f), new Vector2(190f, -70f), new Vector2(360f, 36f));
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
        bar.health = player.GetComponent<Health>();
        bar.fill = fill.rectTransform;
        bar.label = label;

        manager.collectText = ZodiacPrototypeBuilder.CreateText(canvas, "CollectText", "Đồng xu: 0/0", 30, Color.white,
            new Vector2(0f, 1f), new Vector2(570f, -72f), new Vector2(400f, 40f), TextAnchor.UpperLeft);

        ZodiacPrototypeBuilder.CreateText(canvas, "HintText", "A/D đi · Shift chạy · Space nhảy · J đánh · K chiêu · R chơi lại", 24, Color.white,
            new Vector2(1f, 1f), new Vector2(-24f, -22f), new Vector2(1000f, 40f), TextAnchor.UpperRight);

        var banner = ZodiacPrototypeBuilder.CreateText(canvas, "LevelBanner", $"{def.title}\n<size=34>{def.subtitle}</size>", 64,
            Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(1400f, 200f), TextAnchor.MiddleCenter);
        banner.gameObject.AddComponent<TimedFade>().delay = 2.5f;

        BuildResultPanels(canvas, manager);
        ZodiacPrototypeBuilder.CreateEventSystem(null);
        return canvas;
    }

    static void BuildResultPanels(Transform canvas, ZodiacGameManager manager)
    {
        manager.winPanel = ZodiacPrototypeBuilder.CreateResultPanel(canvas, "WinPanel", "CHIẾN THẮNG!", out Text winDetail,
            ("Chơi lại", manager.Restart), (string.IsNullOrEmpty(def.nextScene) ? "Màn hình chính" : "Màn tiếp theo", manager.NextLevel));
        manager.winDetailText = winDetail;
        manager.nextButtonLabel = manager.winPanel.transform.Find("Box/Button 2/Label").GetComponent<Text>();

        manager.losePanel = ZodiacPrototypeBuilder.CreateResultPanel(canvas, "LosePanel", "THUA RỒI!", out Text loseDetail,
            ("Hồi sinh tại đèn lồng", manager.ContinueFromCheckpoint), ("Màn hình chính", manager.BackToMenu));
        loseDetail.text = "Bạn đã bị hạ gục";
        manager.loseDetailText = loseDetail;
    }
}
