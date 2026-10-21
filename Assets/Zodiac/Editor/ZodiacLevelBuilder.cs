using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static ZodiacLevelData;

/// <summary>
/// Dựng một scene beat 'em up 2.5D từ <see cref="ZodiacLevelData"/>: con đường 3D chạy dọc trục X (làn theo Z),
/// tường vô hình giữ người chơi trên mặt đường, cảnh hai bên theo chủ đề (làng quê / rừng trúc / hang động),
/// nền xa bằng ảnh lớn đặt sâu (thị sai thật nhờ camera phối cảnh), mặt trời có bóng đổ, sương mù theo khoảng cách,
/// đèn điểm (đèn lồng, lửa, pha lê) — nhân vật dạng sprite nhận đúng ánh sáng đó nên "chìm" vào bản đồ.
/// Màn chia thành các khu vừa một khung hình (<see cref="EncounterZone"/>): tường hai đầu, đèn lồng đầu khu, biển hướng dẫn,
/// đồng xu, bãi chông, quái theo đợt, lối ra mép phải (<see cref="RoomExit"/>); khu cuối có cổng đích / boss.
/// Thêm người chơi, camera, HUD và hậu kỳ.
/// Hình khối 3D (nhà, mái, tường đá…) là mesh hộp UV theo kích thước thật nên texture lát đều, không bị kéo giãn.
/// </summary>
public static class ZodiacLevelBuilder
{
    public class Context
    {
        public Sprite square, circle;
        public ZodiacRoster roster;
        public ZodiacResources resources;
        public Material particleMaterial;
        public ZodiacTextureFactory.Set textures;
        public PhysicsMaterial noFriction;
        public ZodiacEnemyFactory.Prefabs prefabs;
        public int groundLayer, playerLayer, enemyLayer;
    }

    const string SceneFolder = "Assets/Zodiac/Scenes";
    const float CameraFov = 35f;

    static readonly Color CoinColor = new Color(1f, 0.80f, 0.15f);
    static readonly Color WoodColor = new Color(0.45f, 0.28f, 0.15f);
    static readonly Color LanternColor = new Color(0.85f, 0.15f, 0.1f);
    static readonly Color GateRed = new Color(0.72f, 0.12f, 0.1f);
    static readonly Color RoofColor = new Color(0.32f, 0.27f, 0.32f);
    static readonly Color BarBackColor = new Color(0.15f, 0.1f, 0.1f, 0.85f);
    static readonly Color PlayerBarColor = new Color(0.85f, 0.15f, 0.15f);
    static readonly Color BossBarColor = new Color(0.55f, 0.85f, 1f);
    static readonly Color SlashColor = new Color(1f, 1f, 0.85f, 0.55f);

    static Context ctx;
    static LevelDef def;
    static System.Random rnd;
    static Transform scenery;
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static readonly Dictionary<(Vector3, float), Mesh> boxMeshes = new Dictionary<(Vector3, float), Mesh>();

    public static string ScenePath(LevelDef level) => $"{SceneFolder}/{level.sceneName}.unity";

    static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
    static float LaneCenter => (def.laneMin + def.laneMax) / 2f;

    public static void Build(LevelDef level, Context context)
    {
        ctx = context;
        def = level;
        rnd = new System.Random(StableHash(def.sceneName));
        materials.Clear();
        boxMeshes.Clear();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = ZodiacPrototypeBuilder.CreateCamera(Vector3.zero, def.sky);
        cam.orthographic = false;
        cam.fieldOfView = CameraFov;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 260f;

        scenery = new GameObject("Scenery").transform;
        BuildGround();
        BuildBounds();
        switch (def.backdrop)
        {
            case Backdrop.Village: BuildVillage(); break;
            case Backdrop.Bamboo: BuildBambooForest(); break;
            default: BuildCave(); break;
        }
        BuildFarBackdrop();
        BuildSky(cam);

        var player = CreatePlayer(def.hub ? new Vector3(3f, 0.05f, LaneCenter) : RoomEntry(0));
        var follow = cam.gameObject.AddComponent<ZodiacCameraFollow>();
        follow.target = player.transform;
        follow.laneCenterZ = LaneCenter;
        follow.minX = 0f;
        follow.maxX = def.Length;

        var manager = new GameObject("GameManager").AddComponent<ZodiacGameManager>();
        manager.gameObject.AddComponent<InventoryWindow>();   // hành trang (I / Tab) + thanh dùng nhanh
        manager.gameObject.AddComponent<PauseMenu>();         // Esc: tạm dừng, cài đặt
        manager.levelIndex = System.Array.IndexOf(Levels, def);
        manager.villageScene = Village.sceneName;
        manager.isHub = def.hub;
        manager.player = player;
        manager.nextSceneName = def.nextScene;
        manager.levelTitle = def.title;
        manager.laneMinZ = def.laneMin;
        manager.laneMaxZ = def.laneMax;
        manager.music = ZodiacVisualSetup.Clip(MusicFor(def.backdrop));
        manager.stoneFootsteps = def.backdrop == Backdrop.Cave;
        var canvas = BuildHud(manager, player);
        if (def.hub) BuildHub(manager);
        else BuildRooms(manager, canvas);

        BuildLighting(cam, player);
        BuildAmbientParticles(cam);
        MarkStatic(scenery);

        ZodiacPrototypeBuilder.EnsureFolder(SceneFolder);
        EditorSceneManager.SaveScene(scene, ScenePath(def));
    }

    static int StableHash(string s)
    {
        unchecked
        {
            int h = (int)2166136261;
            foreach (char c in s) h = (h ^ c) * 16777619;
            return h;
        }
    }

    // ───────────────────────────── Hình khối ─────────────────────────────

    static Material Mat(string key, Texture texture, Color color, float smoothness = 0.12f, Color? emission = null)
    {
        string name = $"{def.sceneName}_{key}";
        if (materials.TryGetValue(name, out var m)) return m;
        m = ZodiacVisualSetup.EnvMaterial(name, texture, color, smoothness, emission);
        materials[name] = m;
        return m;
    }

    static Material Plain(string key, Color color, Color? emission = null) =>
        Mat(key, null, color, 0.1f, emission);

    /// <summary>Mesh hộp có UV theo kích thước thật (tile unit lặp lại một lần texture) — dùng chung cho cùng kích thước.</summary>
    static Mesh BoxMesh(Vector3 size, float tile)
    {
        var key = (size, tile);
        if (boxMeshes.TryGetValue(key, out var mesh)) return mesh;
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        Vector3 h = size * 0.5f;
        void Face(Vector3 normal, Vector3 up)
        {
            Vector3 right = Vector3.Cross(normal, up);
            float hr = Mathf.Abs(Vector3.Dot(h, right)), hu = Mathf.Abs(Vector3.Dot(h, up));
            Vector3 c = Vector3.Scale(normal, h);
            int s = verts.Count;
            verts.Add(c - right * hr - up * hu);
            verts.Add(c + right * hr - up * hu);
            verts.Add(c + right * hr + up * hu);
            verts.Add(c - right * hr + up * hu);
            for (int i = 0; i < 4; i++) normals.Add(normal);
            uvs.Add(new Vector2(-hr, -hu) / tile);
            uvs.Add(new Vector2(hr, -hu) / tile);
            uvs.Add(new Vector2(hr, hu) / tile);
            uvs.Add(new Vector2(-hr, hu) / tile);
            tris.AddRange(new[] { s, s + 3, s + 2, s, s + 2, s + 1 });
        }
        Face(Vector3.back, Vector3.up);
        Face(Vector3.forward, Vector3.up);
        Face(Vector3.left, Vector3.up);
        Face(Vector3.right, Vector3.up);
        Face(Vector3.up, Vector3.forward);
        Face(Vector3.down, Vector3.back);
        mesh = new Mesh { name = $"Box {size.x:0.##}x{size.y:0.##}x{size.z:0.##}" };
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        boxMeshes[key] = mesh;
        return mesh;
    }

    static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material mat, float tile = 2f,
                          bool collider = false, Vector3? euler = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(center, Quaternion.Euler(euler ?? Vector3.zero));
        go.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size, tile);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        if (collider)
        {
            go.layer = ctx.groundLayer;
            go.AddComponent<BoxCollider>().size = size;
        }
        return go;
    }

    /// <summary>Khối nguyên mẫu Unity (trụ, cầu) — bỏ collider, chỉ để trang trí.</summary>
    static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material mat,
                                Vector3? euler = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(euler ?? Vector3.zero));
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    /// <summary>Ảnh phẳng lớn đặt trong thế giới (núi, đồi xa) — nhận đèn và sương mù, không đổ bóng.</summary>
    static SpriteRenderer Card(string name, Transform parent, Sprite sprite, Vector3 position, Vector2 size, Color color, bool tiled = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (tiled)
        {
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
        }
        else
        {
            Vector2 s = sprite.bounds.size;
            go.transform.localScale = new Vector3(size.x / s.x, size.y / s.y, 1f);
        }
        sr.color = color;
        sr.sharedMaterial = ZodiacVisualSetup.SpriteLit3D;
        sr.shadowCastingMode = ShadowCastingMode.Off;
        sr.receiveShadows = false;
        return sr;
    }

    static void MarkStatic(Transform root)
    {
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic);
    }

    // ───────────────────────────── Mặt đất, giới hạn ─────────────────────────────

    static void BuildGround()
    {
        var t = ctx.textures;
        float span = def.Length + 80f;
        // Nền rộng ra hai bên con đường (đồng cỏ / nền rừng / nền hang) — mặt trên ở y = 0, có collider
        Material field = def.backdrop switch
        {
            Backdrop.Village => Mat("Field", t.turf.texture, def.grass),
            Backdrop.Bamboo => Mat("ForestFloor", t.turf.texture, Color.Lerp(def.grass, def.soil, 0.45f)),
            _ => Mat("CaveFloor", t.soil.texture, def.soil),
        };
        Box("Ground", scenery, new Vector3(def.Length / 2f, -0.5f, 14f), new Vector3(span, 1f, 50f), field, 3f, collider: true);
        // Mặt đường đi được (đường đất / lối mòn / nền đá) nhô lên 1cm để không nhấp nháy với nền
        Material road = def.backdrop switch
        {
            Backdrop.Village => Mat("Road", t.soil.texture, def.soil),
            Backdrop.Bamboo => Mat("Trail", t.soil.texture, def.soil),
            _ => Mat("CaveRoad", t.cobble.texture, def.stone),
        };
        float width = def.laneMax - def.laneMin + 1.4f;
        Box("Road", scenery, new Vector3(def.Length / 2f, -0.49f, LaneCenter), new Vector3(def.Length + 30f, 1f, width), road, 2.5f);
        // Mép đường: dải đá viền hai bên cho thấy rõ giới hạn đi được
        var curb = Mat("Curb", t.cobble.texture, Color.Lerp(def.stone, Color.white, 0.1f));
        foreach (float z in new[] { def.laneMin - 0.75f, def.laneMax + 0.75f })
            Box("Curb", scenery, new Vector3(def.Length / 2f, 0.02f, z), new Vector3(def.Length + 30f, 0.12f, 0.3f), curb, 1f);
    }

    /// <summary>Tường vô hình: hai mép đường (trước/sau) và hai đầu màn.</summary>
    static void BuildBounds()
    {
        var root = new GameObject("Bounds").transform;
        void Wall(string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name) { layer = ctx.groundLayer };
            go.transform.SetParent(root, false);
            go.transform.position = center;
            go.AddComponent<BoxCollider>().size = size;
        }
        float mid = def.Length / 2f, depth = def.laneMax - def.laneMin;
        Wall("Back", new Vector3(mid, 3f, def.laneMax + 0.75f), new Vector3(def.Length + 20f, 8f, 1f));
        Wall("Front", new Vector3(mid, 3f, def.laneMin - 0.75f), new Vector3(def.Length + 20f, 8f, 1f));
        Wall("Start", new Vector3(-0.5f, 3f, LaneCenter), new Vector3(1f, 8f, depth + 4f));
        Wall("End", new Vector3(def.Length + 0.5f, 3f, LaneCenter), new Vector3(1f, 8f, depth + 4f));
    }

    // ───────────────────────────── Cảnh theo chủ đề ─────────────────────────────

    static void BuildVillage()
    {
        var t = ctx.textures;
        var wall = Mat("HouseWall", t.wood.texture, new Color(0.78f, 0.62f, 0.45f));
        var plaster = Mat("Plaster", t.brick.texture, new Color(0.93f, 0.88f, 0.78f));
        var roof = Mat("Roof", t.roof.texture, new Color(0.62f, 0.33f, 0.24f));
        var dark = Plain("Doorway", new Color(0.16f, 0.1f, 0.07f));
        var trunk = Plain("Trunk", new Color(0.4f, 0.28f, 0.18f));
        var leaves = Mat("Leaves", t.turf.texture, Color.Lerp(def.grass, new Color(0.15f, 0.4f, 0.15f), 0.4f));
        var bamboo = Mat("Bamboo", t.wood.texture, new Color(0.55f, 0.7f, 0.3f));
        var hay = Mat("Hay", t.wood.texture, new Color(0.9f, 0.75f, 0.4f));
        var fence = Plain("Fence", new Color(0.55f, 0.4f, 0.25f));
        var rice = Mat("Rice", t.turf.texture, new Color(0.78f, 0.82f, 0.42f));

        // Dãy nhà phía sau con đường, xen luỹ tre và cây
        float x = -6f;
        while (x < def.Length + 10f)
        {
            float w = R(6f, 8.5f);
            float roll = (float)rnd.NextDouble();
            if (roll < 0.6f) House(new Vector3(x + w / 2f, 0f, def.laneMax + R(4.5f, 6f)), w, roll < 0.3f ? wall : plaster, roof, dark);
            else if (roll < 0.85f) BambooClump(new Vector3(x + w / 2f, 0f, def.laneMax + R(3.5f, 6f)), bamboo, leaves, 5);
            else Tree(new Vector3(x + w / 2f, 0f, def.laneMax + R(3.5f, 5f)), trunk, leaves, R(3.5f, 5f));
            x += w + R(1f, 3f);
        }
        // Hàng rào tre thấp sát mép trong con đường
        for (float fx = 0f; fx < def.Length; fx += 1.6f)
        {
            if (rnd.NextDouble() < 0.25f) { fx += 3f; continue; }
            Box("FencePost", scenery, new Vector3(fx, 0.45f, def.laneMax + 1.4f), new Vector3(0.12f, 0.9f, 0.12f), fence, 1f);
            Box("FenceRail", scenery, new Vector3(fx + 0.8f, 0.6f, def.laneMax + 1.4f), new Vector3(1.6f, 0.07f, 0.07f), fence, 1f);
        }
        // Tiền cảnh: bụi cỏ, đống rơm thấp (không che nhân vật)
        for (float fx = R(2f, 8f); fx < def.Length; fx += R(7f, 14f))
        {
            float z = def.laneMin - R(1.6f, 3.2f);
            if (rnd.NextDouble() < 0.5f)
                Primitive(PrimitiveType.Sphere, "Bush", scenery, new Vector3(fx, 0.2f, z), new Vector3(R(1.2f, 2f), R(0.6f, 0.9f), R(0.9f, 1.3f)), leaves);
            else
                Primitive(PrimitiveType.Cylinder, "Haystack", scenery, new Vector3(fx, 0.45f, z), new Vector3(1.1f, 0.45f, 1.1f), hay);
        }
        // Đồng lúa xa phía sau, bờ ruộng, vài cây lẻ
        Box("RiceField", scenery, new Vector3(def.Length / 2f, 0.02f, def.laneMax + 20f), new Vector3(def.Length + 80f, 0.04f, 22f), rice, 3f);
        for (float z = def.laneMax + 11f; z < def.laneMax + 31f; z += 4f)
            Box("Dike", scenery, new Vector3(def.Length / 2f, 0.06f, z), new Vector3(def.Length + 80f, 0.12f, 0.35f), Mat("Dike", t.soil.texture, def.soil), 2f);
        for (float fx = -10f; fx < def.Length + 20f; fx += R(12f, 22f))
            Tree(new Vector3(fx, 0f, def.laneMax + R(18f, 32f)), trunk, leaves, R(3f, 4.5f));
    }

    /// <summary>Nhà vách gỗ / tường vôi mái ngói hai mái, cửa tối, hiên thấp.</summary>
    static void House(Vector3 basePos, float width, Material wall, Material roof, Material dark)
    {
        var house = new GameObject("House").transform;
        house.SetParent(scenery, false);
        float depth = R(4f, 5f), height = R(2.6f, 3.2f);
        Box("Walls", house, basePos + new Vector3(0f, height / 2f, 0f), new Vector3(width, height, depth), wall, 1.5f);
        // Mái hai mái nghiêng về trước/sau, chìa ra ngoài tường
        float slope = 32f, half = depth / 2f + 0.6f;
        float slab = half / Mathf.Cos(slope * Mathf.Deg2Rad);
        float rise = Mathf.Tan(slope * Mathf.Deg2Rad) * half;
        Vector3 ridge = basePos + new Vector3(0f, height + rise, 0f);
        Box("RoofFront", house, ridge + new Vector3(0f, -rise / 2f, -half / 2f), new Vector3(width + 1.2f, 0.18f, slab), roof, 1.2f,
            euler: new Vector3(-slope, 0f, 0f));
        Box("RoofBack", house, ridge + new Vector3(0f, -rise / 2f, half / 2f), new Vector3(width + 1.2f, 0.18f, slab), roof, 1.2f,
            euler: new Vector3(slope, 0f, 0f));
        Box("Ridge", house, ridge + Vector3.up * 0.05f, new Vector3(width + 1.4f, 0.22f, 0.3f), roof, 1f);
        // Cửa ra vào và cửa sổ mặt trước (mặt nhìn ra đường)
        Box("Door", house, basePos + new Vector3(R(-width / 4f, width / 4f), 0.95f, -depth / 2f - 0.02f), new Vector3(1.1f, 1.9f, 0.05f), dark, 1f);
        Box("Window", house, basePos + new Vector3(width * 0.32f, 1.7f, -depth / 2f - 0.02f), new Vector3(0.9f, 0.6f, 0.05f), dark, 1f);
    }

    static void Tree(Vector3 basePos, Material trunk, Material leaves, float height)
    {
        var tree = new GameObject("Tree").transform;
        tree.SetParent(scenery, false);
        Primitive(PrimitiveType.Cylinder, "Trunk", tree, basePos + Vector3.up * height * 0.35f, new Vector3(0.35f, height * 0.35f, 0.35f), trunk);
        for (int i = 0; i < 3; i++)
        {
            Vector3 offset = new Vector3(R(-0.8f, 0.8f), height * 0.75f + R(-0.3f, 0.5f), R(-0.6f, 0.6f));
            float s = R(1.8f, 2.6f);
            Primitive(PrimitiveType.Sphere, "Canopy", tree, basePos + offset, new Vector3(s, s * 0.85f, s), leaves);
        }
    }

    static void BambooClump(Vector3 basePos, Material stalk, Material leaves, int count)
    {
        var clump = new GameObject("BambooClump").transform;
        clump.SetParent(scenery, false);
        for (int i = 0; i < count; i++)
        {
            float h = R(5f, 8f), r = R(0.12f, 0.18f);
            Vector3 p = basePos + new Vector3(R(-1.2f, 1.2f), h / 2f, R(-0.8f, 0.8f));
            Primitive(PrimitiveType.Cylinder, "Stalk", clump, p, new Vector3(r, h / 2f, r), stalk, new Vector3(R(-4f, 4f), 0f, R(-4f, 4f)));
        }
        Primitive(PrimitiveType.Sphere, "Leaves", clump, basePos + Vector3.up * 6.5f, new Vector3(3.5f, 2f, 2.5f), leaves);
    }

    static void BuildBambooForest()
    {
        var t = ctx.textures;
        Color[] tints =
        {
            new Color(0.52f, 0.7f, 0.32f), new Color(0.42f, 0.62f, 0.3f), new Color(0.6f, 0.72f, 0.38f), new Color(0.35f, 0.52f, 0.28f),
        };
        var stalkMats = new Material[tints.Length];
        for (int i = 0; i < tints.Length; i++) stalkMats[i] = Mat("Bamboo" + i, t.wood.texture, tints[i]);
        var node = Plain("BambooNode", new Color(0.3f, 0.4f, 0.2f));
        var leaves = Mat("BambooLeaves", t.turf.texture, new Color(0.3f, 0.5f, 0.28f));
        var rock = Mat("Rock", t.brick.texture, def.stone);

        // Rừng tre dày phía sau, thưa dần ra xa (sương mù làm nhạt dần → chiều sâu thật)
        for (float z = def.laneMax + 1.8f; z < def.laneMax + 40f; z += R(1.2f, 2.6f))
        {
            float density = Mathf.Lerp(0.9f, 0.35f, (z - def.laneMax) / 40f);
            for (float x = R(-12f, -8f); x < def.Length + 12f; x += R(1.4f, 3.5f) / density)
                BambooStalk(new Vector3(x, 0f, z + R(-0.5f, 0.5f)), stalkMats[rnd.Next(stalkMats.Length)], node, R(9f, 15f));
        }
        // Vài cây tre tiền cảnh sát camera (thưa, mảnh) — tạo lớp che phía trước như rừng thật
        for (float x = R(4f, 10f); x < def.Length; x += R(14f, 22f))
            BambooStalk(new Vector3(x, 0f, def.laneMin - R(2.2f, 3.5f)), stalkMats[rnd.Next(stalkMats.Length)], node, R(9f, 12f));
        // Tán lá trên cao, đá rêu ven đường
        for (float x = -10f; x < def.Length + 10f; x += R(6f, 10f))
            Primitive(PrimitiveType.Sphere, "Canopy", scenery, new Vector3(x, R(9f, 12f), def.laneMax + R(3f, 10f)), new Vector3(R(5f, 8f), 2f, 4f), leaves);
        for (float x = R(3f, 8f); x < def.Length; x += R(8f, 15f))
            Primitive(PrimitiveType.Sphere, "Rock", scenery, new Vector3(x, 0.15f, def.laneMax + R(1.2f, 2.2f)), new Vector3(R(0.8f, 1.6f), R(0.5f, 0.8f), R(0.7f, 1.1f)), rock);
    }

    static void BambooStalk(Vector3 basePos, Material stalk, Material node, float height)
    {
        float r = R(0.14f, 0.24f);
        var go = Primitive(PrimitiveType.Cylinder, "Stalk", scenery, basePos + Vector3.up * height / 2f, new Vector3(r, height / 2f, r), stalk,
                           new Vector3(R(-2f, 2f), 0f, R(-3f, 3f)));
        for (float y = R(0.8f, 1.6f); y < height; y += R(1.4f, 1.9f))
            Primitive(PrimitiveType.Cylinder, "Node", go.transform.parent, basePos + Vector3.up * y, new Vector3(r * 1.15f, 0.03f, r * 1.15f), node);
    }

    static void BuildCave()
    {
        var t = ctx.textures;
        var rock = Mat("CaveWall", t.brick.texture, Color.Lerp(def.far, def.stone, 0.4f));
        var rockDark = Mat("CaveWallDark", t.soil.texture, Color.Lerp(def.far, Color.black, 0.2f));
        var crystal = Plain("Crystal", new Color(0.7f, 0.55f, 1f), new Color(0.55f, 0.35f, 1f) * 2f);
        var bowl = Plain("Brazier", new Color(0.3f, 0.25f, 0.25f));
        var ember = Plain("Ember", new Color(1f, 0.55f, 0.2f), new Color(1f, 0.45f, 0.1f) * 3f);

        // Vách hang phía sau: các khối đá cao thấp, nghiêng nhẹ
        for (float x = -10f; x < def.Length + 12f; x += R(2.5f, 4.5f))
        {
            float h = R(6f, 11f), w = R(3f, 5f);
            Box("CaveRock", scenery, new Vector3(x, h / 2f - 0.5f, def.laneMax + R(2.5f, 4f)), new Vector3(w, h, R(2.5f, 4f)),
                rnd.NextDouble() < 0.5 ? rock : rockDark, 2f, euler: new Vector3(R(-6f, 6f), R(-15f, 15f), R(-5f, 5f)));
        }
        // Vách xa hơn mờ trong bóng tối
        Box("CaveBack", scenery, new Vector3(def.Length / 2f, 8f, def.laneMax + 14f), new Vector3(def.Length + 60f, 18f, 2f), rockDark, 4f);
        // Măng đá phía trước và sau
        for (float x = R(2f, 6f); x < def.Length; x += R(6f, 12f))
        {
            float s = R(0.5f, 0.9f);
            Box("Stalagmite", scenery, new Vector3(x, 0.4f, def.laneMin - R(1.6f, 3f)), new Vector3(s, s * 2.2f, s), rock, 1f, euler: new Vector3(0f, 45f, R(-8f, 8f)));
        }
        // Pha lê phát sáng trên vách, chậu lửa ven đường: nguồn sáng chính trong hang
        for (float x = R(3f, 7f); x < def.Length; x += R(8f, 12f))
        {
            var c = Box("Crystal", scenery, new Vector3(x, R(2f, 4.5f), def.laneMax + 1.9f), new Vector3(0.5f, 0.9f, 0.5f), crystal, 1f,
                        euler: new Vector3(0f, 45f, R(-25f, 25f)));
            ZodiacLights.AddPoint(c, new Color(0.6f, 0.45f, 1f), 7f, 2.5f);
        }
        for (float x = 6f; x < def.Length; x += R(11f, 15f))
        {
            var b = Primitive(PrimitiveType.Cylinder, "Brazier", scenery, new Vector3(x, 0.45f, def.laneMax + 1.3f), new Vector3(0.7f, 0.45f, 0.7f), bowl);
            var fire = Primitive(PrimitiveType.Sphere, "Fire", b.transform.parent, new Vector3(x, 1.05f, def.laneMax + 1.3f), new Vector3(0.55f, 0.45f, 0.55f), ember);
            ZodiacLights.AddPoint(fire, new Color(1f, 0.6f, 0.25f), 9f, 3.2f);
        }
    }

    /// <summary>Nền xa: ảnh dãy núi / đồi đặt rất sâu phía sau (camera phối cảnh tự tạo thị sai), sương mù làm nhạt dần.</summary>
    static void BuildFarBackdrop()
    {
        var t = ctx.textures;
        var root = new GameObject("FarBackdrop").transform;
        float span = def.Length + 140f;
        switch (def.backdrop)
        {
            case Backdrop.Village:
                Card("Mountains", root, t.ridge, new Vector3(def.Length / 2f, 9f, 95f), new Vector2(span * 1.6f, 22f), Color.Lerp(def.far, def.sky, 0.25f));
                Card("Hills", root, t.hills, new Vector3(def.Length / 2f, 3.5f, 55f), new Vector2(span * 1.2f, 8f), def.mid);
                break;
            case Backdrop.Bamboo:
                Card("Ridge", root, t.ridge, new Vector3(def.Length / 2f, 10f, 80f), new Vector2(span * 1.5f, 24f), def.far);
                for (int i = 0; i < 3; i++)
                    Card("Mist", root, t.fog, new Vector3(def.Length / 2f, 1.5f + i * 2.5f, 20f + i * 12f), new Vector2(span, 5f),
                         new Color(1f, 1f, 1f, 0.3f), tiled: false).sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
                break;
        }
    }

    /// <summary>Bầu trời chuyển màu gắn vào camera, nằm sau mọi thứ (không chịu sương).</summary>
    static void BuildSky(Camera cam)
    {
        Color top = def.backdrop switch
        {
            Backdrop.Village => Color.Lerp(def.sky, new Color(0.45f, 0.62f, 0.85f), 0.65f),
            Backdrop.Bamboo => Color.Lerp(def.sky, new Color(0.45f, 0.65f, 0.55f), 0.55f),
            _ => Color.Lerp(def.sky, Color.black, 0.6f),
        };
        var sprite = ZodiacTextureFactory.SkyGradient(def.sceneName, top, def.sky);
        var sky = new GameObject("Sky");
        sky.transform.SetParent(cam.transform, false);
        const float distance = 240f;
        sky.transform.localPosition = new Vector3(0f, 0f, distance);
        var sr = sky.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -100;
        sr.sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
        sr.shadowCastingMode = ShadowCastingMode.Off;
        // Phủ kín khung nhìn mọi tỉ lệ màn hình (tới 21:9), chừa dư khi rung
        float height = 2f * distance * Mathf.Tan(CameraFov * 0.5f * Mathf.Deg2Rad) * 1.3f;
        Vector2 size = sprite.bounds.size;
        sky.transform.localScale = new Vector3(height * 2.6f / size.x, height / size.y, 1f);
    }

    static string MusicFor(Backdrop b) => b switch
    {
        Backdrop.Village => "Music/music_village",
        Backdrop.Bamboo => "Music/music_bamboo",
        _ => "Music/music_cave",
    };

    // ───────────────────────────── Đèn lồng, xu, bẫy, biển, cổng ─────────────────────────────

    /// <summary>Cột đèn lồng ở mép trong con đường; vùng chạm phủ ngang cả mặt đường (đi ngang qua là thắp).</summary>
    static void CreateCheckpoint(Transform parent, float x, int index)
    {
        var go = new GameObject($"Checkpoint {index}");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, 0f, LaneCenter);
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(1.4f, 3f, def.laneMax - def.laneMin + 1f);
        box.center = new Vector3(0f, 1.5f, 0f);
        var cp = go.AddComponent<Checkpoint>();
        cp.index = index;

        var wood = Plain("LanternPole", WoodColor);
        float z = def.laneMax + 1.05f;
        Box("Pole", go.transform, new Vector3(x, 1.4f, z), new Vector3(0.14f, 2.8f, 0.14f), wood, 1f);
        Box("Arm", go.transform, new Vector3(x - 0.3f, 2.7f, z), new Vector3(0.6f, 0.08f, 0.08f), wood, 1f);
        var lamp = Primitive(PrimitiveType.Sphere, "Lantern", go.transform, new Vector3(x - 0.55f, 2.2f, z), new Vector3(0.5f, 0.65f, 0.5f),
                             Plain("Lantern", LanternColor, LanternColor * 0.4f));
        cp.lampLight = ZodiacLights.AddPoint(lamp, new Color(1f, 0.6f, 0.3f), 7f, 0.5f);
        cp.onIntensity = 3f;
        cp.offIntensity = 0.5f;
        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(lamp.transform, false);
        var glow = glowGo.AddComponent<SpriteRenderer>();
        glow.sprite = ctx.circle;
        glow.color = new Color(1f, 0.6f, 0.2f, 0.35f);
        glow.sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
        glowGo.AddComponent<Billboard>().full = true;
        cp.glow = glowGo;
        // Lồng đèn là khối 3D — Checkpoint đổi màu qua SpriteRenderer nên thêm một mặt sprite trước lồng
        var faceGo = new GameObject("LampFace");
        faceGo.transform.SetParent(lamp.transform, false);
        faceGo.transform.localPosition = new Vector3(0f, 0f, -0.52f);
        faceGo.transform.localScale = new Vector3(1.6f, 1.3f, 1f);
        var face = faceGo.AddComponent<SpriteRenderer>();
        face.sprite = ctx.circle;
        face.sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
        cp.lamp = face;
    }

    static void CreateCoinRow(Transform parent, float x)
    {
        float z = Mathf.Lerp(def.laneMin + 0.6f, def.laneMax - 0.6f, (float)rnd.NextDouble());
        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject("Coin");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x + i * 0.9f, 0.9f, z);
            go.AddComponent<SphereCollider>().radius = 0.45f;
            go.AddComponent<Collectible>();
            var tilt = new GameObject("Tilt");
            tilt.transform.SetParent(go.transform, false);
            tilt.AddComponent<Billboard>();
            var face = new GameObject("Face");
            face.transform.SetParent(tilt.transform, false);
            face.transform.localScale = Vector3.one * 0.6f;
            var sr = face.AddComponent<SpriteRenderer>();
            sr.sprite = ctx.circle;
            sr.color = CoinColor;
            sr.sharedMaterial = ZodiacVisualSetup.SpriteLit3D;
            sr.shadowCastingMode = ShadowCastingMode.On;
            // Lỗ vuông giữa đồng xu (tiền xu cổ)
            var hole = ZodiacPrototypeBuilder.CreateDecor("Hole", face.transform, ctx.square, Vector2.zero, new Vector2(0.3f, 0.3f),
                                                          new Color(0.7f, 0.5f, 0.1f), 6);
            hole.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            hole.GetComponent<SpriteRenderer>().sharedMaterial = ZodiacVisualSetup.SpriteLit3D;
        }
    }

    /// <summary>Bãi chông (làng, rừng) / than hồng (hang) chắn nửa mặt đường — đi vòng làn kia hoặc nhảy qua.</summary>
    static void CreateHazard(Transform parent, float x, bool innerHalf)
    {
        float zMin = innerHalf ? LaneCenter : def.laneMin, zMax = innerHalf ? def.laneMax : LaneCenter;
        var go = new GameObject("Hazard");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, 0f, (zMin + zMax) / 2f);
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(2.5f, 0.6f, zMax - zMin);
        box.center = new Vector3(0f, 0.3f, 0f);
        go.AddComponent<Hazard>();
        bool cave = def.backdrop == Backdrop.Cave;
        var plate = cave ? Plain("EmberBed", new Color(0.25f, 0.1f, 0.05f), new Color(0.8f, 0.25f, 0.05f))
                         : Plain("SpikeBed", Color.Lerp(def.soil, Color.black, 0.35f));
        var spike = cave ? Plain("Ember", new Color(1f, 0.55f, 0.2f), new Color(1f, 0.45f, 0.1f) * 2.5f)
                         : Plain("Spike", new Color(0.85f, 0.8f, 0.6f));
        Box("Bed", go.transform, go.transform.position + Vector3.up * 0.03f, new Vector3(2.5f, 0.06f, zMax - zMin), plate, 1f);
        for (float sx = -1.05f; sx <= 1.05f; sx += 0.5f)
        for (float sz = zMin + 0.3f; sz < zMax - 0.1f; sz += 0.55f)
            Box("Spike", go.transform, new Vector3(x + sx + R(-0.08f, 0.08f), 0.05f, sz), new Vector3(0.28f, 0.28f, 0.28f), spike, 1f,
                euler: new Vector3(45f, 0f, 45f));
        if (cave) ZodiacLights.AddPoint(go, new Color(1f, 0.45f, 0.15f), 4f, 2f);
    }

    /// <summary>Biển gỗ ở mép trong con đường: khung chữ (Canvas world-space quay về camera) hiện khi người chơi đi ngang.</summary>
    static void CreateSign(Transform parent, float x, string message)
    {
        var go = new GameObject("Sign");
        go.transform.SetParent(parent, false);
        float z = def.laneMax + 1.1f;
        go.transform.position = new Vector3(x, 0f, z);
        var wood = Plain("SignWood", WoodColor);
        Box("Post", go.transform, new Vector3(x, 0.6f, z), new Vector3(0.15f, 1.2f, 0.15f), wood, 1f);
        Box("Board", go.transform, new Vector3(x, 1.3f, z - 0.05f), new Vector3(1f, 0.65f, 0.08f), Plain("SignBoard", new Color(0.75f, 0.55f, 0.3f)), 1f);

        var canvasGo = new GameObject("Bubble", typeof(RectTransform));
        canvasGo.transform.SetParent(go.transform, false);
        canvasGo.AddComponent<Billboard>().full = true;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 30;
        var rt = (RectTransform)canvasGo.transform;
        rt.sizeDelta = new Vector2(1200f, 220f);
        rt.localScale = Vector3.one * 0.0085f;
        rt.position = new Vector3(x, 3.4f, z);
        var back = ZodiacPrototypeBuilder.CreateImage(canvasGo.transform, "Back", new Color(0.1f, 0.07f, 0.05f, 0.85f));
        ZodiacPrototypeBuilder.Stretch(back.rectTransform);
        var text = ZodiacPrototypeBuilder.CreateText(canvasGo.transform, "Text", message, 40, Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1160f, 210f), TextAnchor.MiddleCenter);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;

        var sign = go.AddComponent<HintSign>();
        sign.message = message;
        sign.bubble = canvasGo;
        sign.text = text;
    }

    /// <summary>Cổng làng (ngoài trời) / cổng đền (hang) bắc ngang con đường — đi qua là thắng.</summary>
    static PlayerTrigger CreateGoal(Transform parent, float x)
    {
        var go = new GameObject("Goal");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, 0f, LaneCenter);
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(1.5f, 4f, def.laneMax - def.laneMin + 1f);
        box.center = new Vector3(0f, 2f, 0f);
        var trigger = go.AddComponent<PlayerTrigger>();

        var red = Plain("GateRed", GateRed);
        var roof = Mat("GateRoof", ctx.textures.roof.texture, RoofColor);
        var gold = Plain("GatePlaque", new Color(0.95f, 0.8f, 0.3f), new Color(0.3f, 0.22f, 0.05f));
        float zFront = def.laneMin - 1.1f, zBack = def.laneMax + 1.1f, span = zBack - zFront;
        foreach (float z in new[] { zFront, zBack })
            Box("Pillar", go.transform, new Vector3(x, 2.2f, z), new Vector3(0.45f, 4.4f, 0.45f), red, 1f);
        Box("Beam", go.transform, new Vector3(x, 4.1f, LaneCenter), new Vector3(0.35f, 0.35f, span + 0.6f), red, 1f);
        Box("Roof", go.transform, new Vector3(x, 4.65f, LaneCenter), new Vector3(1.4f, 0.35f, span + 1.6f), roof, 1f);
        Box("Plaque", go.transform, new Vector3(x - 0.2f, 3.6f, LaneCenter), new Vector3(0.1f, 0.6f, 1.6f), gold, 1f);
        return trigger;
    }

    // ───────────────────────────── Làng Trung Tâm ─────────────────────────────

    /// <summary>Bốn điểm dọc đường làng: Bàn thờ tổ · Tiệm thuốc · Lò rèn · Cổng làng (đứng gần bấm F).</summary>
    static void BuildHub(ZodiacGameManager manager)
    {
        var root = new GameObject("Village").transform;
        var window = manager.gameObject.AddComponent<VillageWindow>();
        window.levelScenes = System.Array.ConvertAll(Levels, l => l.sceneName);
        window.levelTitles = System.Array.ConvertAll(Levels, l => l.title);
        float back = def.laneMax + 0.6f;
        var wood = Plain("HubWood", WoodColor);
        var red = Plain("HubRed", GateRed);
        var dark = Plain("HubIron", new Color(0.22f, 0.22f, 0.25f));
        var glow = Plain("HubFire", new Color(1f, 0.55f, 0.2f), new Color(1f, 0.45f, 0.1f) * 3f);
        var candle = Plain("HubCandle", new Color(1f, 0.9f, 0.6f), new Color(1f, 0.8f, 0.4f) * 2f);
        var jar = Mat("HubJar", ctx.textures.brick.texture, new Color(0.55f, 0.35f, 0.25f));

        // Bàn thờ tổ: bàn sơn đỏ, bát hương, hai cây nến
        var altar = Station(root, VillageStation.Kind.Altar, 6f, "Bàn thờ tổ", "F: Tu luyện");
        Box("AltarTable", altar, new Vector3(6f, 0.55f, back), new Vector3(2.2f, 1.1f, 1f), red, 1f);
        Primitive(PrimitiveType.Cylinder, "Incense", altar, new Vector3(6f, 1.25f, back), new Vector3(0.45f, 0.15f, 0.45f), jar);
        foreach (float dx in new[] { -0.8f, 0.8f })
        {
            var c = Primitive(PrimitiveType.Cylinder, "Candle", altar, new Vector3(6f + dx, 1.35f, back), new Vector3(0.12f, 0.25f, 0.12f), candle);
            ZodiacLights.AddPoint(c, new Color(1f, 0.75f, 0.4f), 4f, 1.6f);
        }

        // Tiệm thuốc: quầy gỗ + chum thuốc
        var shop = Station(root, VillageStation.Kind.Shop, 14f, "Tiệm thuốc bắc", "F: Mua bán · Luyện đan");
        Box("Counter", shop, new Vector3(14f, 0.55f, back), new Vector3(3f, 1.1f, 1f), wood, 1f);
        for (int i = 0; i < 4; i++)
            Primitive(PrimitiveType.Cylinder, "Jar", shop, new Vector3(13f + i * 0.65f, 1.35f, back), new Vector3(0.4f, 0.25f, 0.4f), jar);

        // Lò rèn: đe sắt + lò lửa
        var forge = Station(root, VillageStation.Kind.Forge, 22f, "Lò rèn", "F: Đập đồ · Phân giải");
        Box("AnvilBase", forge, new Vector3(21.3f, 0.35f, back - 0.2f), new Vector3(0.6f, 0.7f, 0.5f), dark, 1f);
        Box("AnvilTop", forge, new Vector3(21.3f, 0.8f, back - 0.2f), new Vector3(1.2f, 0.25f, 0.5f), dark, 1f);
        Box("Furnace", forge, new Vector3(23f, 1f, back + 0.2f), new Vector3(1.6f, 2f, 1.2f), Mat("HubBrick", ctx.textures.brick.texture, new Color(0.55f, 0.4f, 0.35f)), 1f);
        var fire = Box("FurnaceFire", forge, new Vector3(23f, 0.7f, back - 0.42f), new Vector3(0.8f, 0.6f, 0.05f), glow, 1f);
        ZodiacLights.AddPoint(fire, new Color(1f, 0.55f, 0.2f), 6f, 3f);

        // Cổng làng: lên đường vào các màn
        var gate = Station(root, VillageStation.Kind.Gate, 31f, "Cổng làng", "F: Lên đường");
        foreach (float z in new[] { def.laneMin - 1.1f, def.laneMax + 1.1f })
            Box("Pillar", gate, new Vector3(31f, 2.2f, z), new Vector3(0.45f, 4.4f, 0.45f), red, 1f);
        Box("Beam", gate, new Vector3(31f, 4.1f, LaneCenter), new Vector3(0.35f, 0.35f, def.laneMax - def.laneMin + 2.8f), red, 1f);
        Box("Roof", gate, new Vector3(31f, 4.65f, LaneCenter), new Vector3(1.4f, 0.35f, def.laneMax - def.laneMin + 3.8f),
            Mat("GateRoof", ctx.textures.roof.texture, RoofColor), 1f);
    }

    /// <summary>Một điểm trong làng: tên luôn hiện trên cao, chữ gợi ý "F: …" hiện khi đứng gần.</summary>
    static Transform Station(Transform parent, VillageStation.Kind kind, float x, string title, string hint)
    {
        var go = new GameObject("Station " + title);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, 0f, LaneCenter);
        var station = go.AddComponent<VillageStation>();
        station.kind = kind;
        var name = ZodiacPrototypeBuilder.CreateText(CreateWorldCanvas(go.transform, new Vector3(x, 3.4f, def.laneMax + 0.6f)), "Name", title, 52,
                                                     new Color(1f, 0.9f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 80f), TextAnchor.MiddleCenter);
        name.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 80f);
        var promptCanvas = CreateWorldCanvas(go.transform, new Vector3(x, 2.6f, def.laneMax + 0.6f));
        promptCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 60f);
        ZodiacPrototypeBuilder.CreateText(promptCanvas, "Prompt", hint, 40, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero,
                                          new Vector2(600f, 60f), TextAnchor.MiddleCenter);
        station.prompt = promptCanvas.gameObject;
        return go.transform;
    }

    // ───────────────────────────── Khu, boss ─────────────────────────────

    static Vector3 RoomEntry(int i) => new Vector3(def.RoomX(i) + 1.6f, 0.05f, LaneCenter);

    static void BuildRooms(ZodiacGameManager manager, Transform canvas)
    {
        var root = new GameObject("Rooms").transform;
        var coins = new GameObject("Collectibles").transform;
        var zones = new EncounterZone[def.stages.Length];
        for (int i = 0; i < def.stages.Length; i++)
        {
            var stage = def.stages[i];
            float xMin = def.RoomX(i), xMax = xMin + RoomWidth;
            bool last = i == def.stages.Length - 1;
            var go = new GameObject($"Room {i + 1} — {stage.title}");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3((xMin + xMax) / 2f, 0f, LaneCenter);
            var zone = go.AddComponent<EncounterZone>();
            zone.title = stage.title;
            zone.xMin = xMin;
            zone.xMax = xMax;
            zone.entry = RoomEntry(i);
            RoomWall(go.transform, xMin - 0.5f);
            RoomWall(go.transform, xMax + 0.5f);
            CreateCheckpoint(go.transform, xMin + 0.6f, i + 1);
            if (!string.IsNullOrEmpty(stage.sign)) CreateSign(go.transform, xMin + 3.2f, stage.sign);
            for (int c = 0; c < stage.coinRows; c++) CreateCoinRow(coins, Mathf.Lerp(xMin + 4f, xMax - 4f, (c + 0.5f) / stage.coinRows));
            if (stage.hazard) CreateHazard(go.transform, (xMin + xMax) / 2f + 1f, i % 2 == 0);
            zone.waves = new EncounterZone.Wave[stage.waves.Length];
            for (int w = 0; w < stage.waves.Length; w++) zone.waves[w] = ParseWave(xMin, xMax, w, stage.waves[w]);
            if (stage.boss) BuildBoss(zone, xMin, xMax, canvas);
            // Cổng đích sát mép phải (như lối ra các khu khác): đánh xong cứ đi sang phải là tới
            if (last) manager.goal = CreateGoal(go.transform, xMax - 0.8f);
            else zone.exit = CreateExit(go.transform, xMax - 0.7f);
            zones[i] = zone;
        }
        manager.rooms = zones;
    }

    /// <summary>Tường hai đầu khu — chặn cả người chơi lẫn quái (không ai bị đánh văng ra khỏi khung hình).</summary>
    static void RoomWall(Transform parent, float x)
    {
        var go = new GameObject("RoomWall") { layer = ctx.groundLayer };
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, 3f, LaneCenter);
        go.AddComponent<BoxCollider>().size = new Vector3(1f, 8f, def.laneMax - def.laneMin + 2f);
    }

    /// <summary>Lối ra mép phải: cột sáng vàng + mũi tên, bật khi dọn xong khu.</summary>
    static GameObject CreateExit(Transform parent, float x)
    {
        var go = new GameObject("RoomExit");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, 0f, LaneCenter);
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, 3f, def.laneMax - def.laneMin + 1f);
        box.center = new Vector3(0f, 1.5f, 0f);
        var exit = go.AddComponent<RoomExit>();

        var beacon = new GameObject("Beacon").transform;
        beacon.SetParent(go.transform, false);
        var gold = new Color(1f, 0.85f, 0.35f, 0.35f);
        var glow = Plain("ExitGlow", new Color(1f, 0.85f, 0.35f), new Color(1f, 0.75f, 0.25f) * 1.5f);
        // Cột sáng mảnh ở mép trong + vệt sáng chạy ngang mặt đường (không chắn tầm nhìn)
        var column = Box("LightColumn", beacon, new Vector3(x, 1.6f, def.laneMax + 0.4f), new Vector3(0.25f, 3.2f, 0.25f), glow, 1f);
        var strip = Box("LightStrip", beacon, new Vector3(x, 0.02f, LaneCenter), new Vector3(0.35f, 0.03f, def.laneMax - def.laneMin), glow, 1f);
        foreach (var r in new[] { column, strip }) r.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        var arrow = ZodiacPrototypeBuilder.CreateText(CreateWorldCanvas(beacon, new Vector3(x - 0.6f, 2.4f, def.laneMin - 0.2f)), "Arrow", "→", 160,
                                                      new Color(1f, 0.9f, 0.4f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 200f), TextAnchor.MiddleCenter);
        arrow.fontStyle = FontStyle.Bold;
        ZodiacLights.AddPoint(beacon.gameObject, gold, 6f, 2f);
        exit.beacon = beacon;
        return go;
    }

    static Transform CreateWorldCanvas(Transform parent, Vector3 position)
    {
        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        canvasGo.transform.SetParent(parent, false);
        canvasGo.AddComponent<Billboard>().full = true;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 30;
        var rt = (RectTransform)canvasGo.transform;
        rt.sizeDelta = new Vector2(200f, 200f);
        rt.localScale = Vector3.one * 0.01f;
        rt.position = position;
        return canvasGo.transform;
    }

    /// <summary>
    /// Đợt quái từ chuỗi mã. Quái hiện ngay trong khu: đợt đầu dồn về nửa phải (người chơi vào từ mép trái),
    /// các đợt sau xen kẽ hai bên; rải đều các làn. Thạch Sư đứng sẵn ở làn trong cùng.
    /// </summary>
    static EncounterZone.Wave ParseWave(float xMin, float xMax, int waveIndex, string codes)
    {
        var spawns = new List<EncounterZone.Spawn>();
        var tokens = codes.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        int walkers = 0, turrets = 0;
        foreach (var token in tokens)
        {
            char c = token[0];
            var prefab = c switch
            {
                'W' => ctx.prefabs.walker,
                'Q' => ctx.prefabs.shield,
                'S' => ctx.prefabs.roller,
                'F' => ctx.prefabs.flyer,
                'H' => ctx.prefabs.hopper,
                'T' => ctx.prefabs.turret,
                _ => null,
            };
            if (prefab == null)
            {
                Debug.LogWarning($"{def.sceneName}: mã quái lạ '{c}' trong đợt \"{codes}\"");
                continue;
            }
            Vector3 pos;
            float delay;
            if (c == 'T')
            {
                float tx = Mathf.Lerp(xMin, xMax, turrets % 2 == 0 ? 0.75f : 0.45f);
                pos = new Vector3(tx, 0.05f, def.laneMax - 0.5f);
                delay = 0f;
                turrets++;
            }
            else
            {
                bool right = waveIndex == 0 || walkers % 2 == 0;
                int slot = waveIndex == 0 ? walkers : walkers / 2;
                float x = right ? xMax - 1.2f - slot * 1.6f : xMin + 1.2f + slot * 1.6f;
                x = Mathf.Clamp(x, xMin + 5f, xMax - 1.2f);
                if (!right) x = Mathf.Clamp(xMin + 1.2f + slot * 1.6f, xMin + 1.2f, xMax - 5f);
                float lane = tokens.Length <= 1 ? 0.5f : (walkers * 0.37f + 0.15f) % 1f;
                float z = Mathf.Lerp(def.laneMin + 0.5f, def.laneMax - 0.5f, lane);
                pos = new Vector3(x, c == 'F' ? 2.6f : 0.05f, z);
                delay = walkers * 0.4f;
                walkers++;
            }
            spawns.Add(new EncounterZone.Spawn { prefab = prefab, position = pos, delay = delay });
        }
        return new EncounterZone.Wave { spawns = spawns.ToArray() };
    }

    static void BuildBoss(EncounterZone zone, float xMin, float xMax, Transform canvas)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(ctx.prefabs.boss);
        go.transform.position = new Vector3(Mathf.Lerp(xMin, xMax, 0.72f), 0.05f, LaneCenter);
        var boss = go.GetComponent<BossFoxSpirit>();
        zone.boss = boss;
        zone.bossMusic = ZodiacVisualSetup.Clip("Music/music_boss");

        // Thanh máu boss trên HUD
        var bar = ZodiacPrototypeBuilder.CreateImage(canvas, "BossBar", BarBackColor);
        bar.rectTransform.pivot = new Vector2(0.5f, 1f);
        ZodiacPrototypeBuilder.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 34f));
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
        zone.bossBar = bar.gameObject;
        zone.bossNameText = name;
        bar.transform.SetSiblingIndex(1);
    }

    // ───────────────────────────── Ánh sáng, hậu kỳ, hạt môi trường ─────────────────────────────

    static void BuildLighting(Camera cam, ZodiacPlayerController player)
    {
        var lights = new GameObject("Lighting");
        Color ambient, fog;
        float fogStart, fogEnd;
        // Mặt trời chiếu từ phía trước-trên (phía camera) → bóng đổ ra sau nhân vật, nhìn rõ trên mặt đường
        switch (def.backdrop)
        {
            case Backdrop.Village:
                ZodiacLights.AddSun(lights, new Color(1f, 0.9f, 0.76f), 1.35f, new Vector3(48f, -32f, 0f), 0.8f);
                ambient = new Color(0.58f, 0.58f, 0.62f);
                fog = Color.Lerp(def.sky, Color.white, 0.15f);
                fogStart = 28f; fogEnd = 120f;
                break;
            case Backdrop.Bamboo:
                ZodiacLights.AddSun(lights, new Color(0.85f, 1f, 0.88f), 0.95f, new Vector3(58f, -18f, 0f), 0.75f);
                ambient = new Color(0.46f, 0.56f, 0.5f);
                fog = Color.Lerp(def.sky, Color.white, 0.2f);
                fogStart = 10f; fogEnd = 62f;
                break;
            default:
                ZodiacLights.AddSun(lights, new Color(0.55f, 0.5f, 0.85f), 0.3f, new Vector3(70f, -10f, 0f), 0.5f);
                ambient = new Color(0.2f, 0.17f, 0.28f);
                fog = Color.Lerp(def.sky, Color.black, 0.2f);
                fogStart = 8f; fogEnd = 42f;
                break;
        }
        lights.AddComponent<ZodiacAmbient>().ambient = ambient;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fog;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
        cam.backgroundColor = fog;

        if (def.backdrop == Backdrop.Cave)   // đèn quanh người trong hang tối
        {
            var aura = new GameObject("Aura");
            aura.transform.SetParent(player.transform, false);
            aura.transform.localPosition = new Vector3(0f, 1.6f, -0.6f);
            ZodiacLights.AddPoint(aura, new Color(1f, 0.88f, 0.72f), 6f, 1.6f);
        }

        // Hậu kỳ
        var camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;
        camData.renderShadows = true;
        var volume = new GameObject("PostProcess").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = def.backdrop switch
        {
            Backdrop.Village => ZodiacVisualSetup.EnsureVolumeProfile("Village", 0.45f, 0.2f, 12f, 10f, Color.white),
            Backdrop.Bamboo => ZodiacVisualSetup.EnsureVolumeProfile("Bamboo", 0.5f, 0.24f, 4f, 8f, new Color(0.96f, 1f, 0.97f)),
            _ => ZodiacVisualSetup.EnsureVolumeProfile("Cave", 0.9f, 0.32f, 6f, 14f, new Color(0.95f, 0.93f, 1f)),
        };
    }

    /// <summary>Hạt môi trường bám theo camera: lá vàng bay (làng), đom đóm (rừng trúc), bụi lơ lửng (hang).</summary>
    static void BuildAmbientParticles(Camera cam)
    {
        var go = new GameObject("AmbientParticles");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0f, -1f, 12f);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 160;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(26f, 10f, 12f);
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
                emission.rateOverTime = 6f;
                main.startLifetime = 8f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.3f, 0.85f), new Color(0.95f, 0.55f, 0.3f, 0.85f));
                vel.x = new ParticleSystem.MinMaxCurve(-1.5f, -0.6f);
                vel.y = new ParticleSystem.MinMaxCurve(-0.8f, -0.3f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                noise.strength = 0.6f;
                noise.frequency = 0.3f;
                break;
            case Backdrop.Bamboo:    // đom đóm
                emission.rateOverTime = 9f;
                main.startLifetime = 6f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 1f, 0.4f, 1f), new Color(1f, 0.95f, 0.5f, 1f));
                vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.3f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                noise.strength = 1f;
                noise.frequency = 0.5f;
                break;
            default:                 // bụi hang
                emission.rateOverTime = 10f;
                main.startLifetime = 7f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 0.7f, 1f, 0.5f), new Color(1f, 1f, 1f, 0.4f));
                vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
                vel.y = new ParticleSystem.MinMaxCurve(-0.15f, 0.05f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
                noise.strength = 0.3f;
                noise.frequency = 0.4f;
                break;
        }
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = ctx.particleMaterial;
        renderer.sortingOrder = 25;
    }

    // ───────────────────────────── Người chơi ─────────────────────────────

    static ZodiacPlayerController CreatePlayer(Vector3 spawn)
    {
        var fallback = ctx.roster.Get(ZodiacSelection.Index);
        var go = new GameObject("Player") { layer = ctx.playerLayer };
        go.transform.position = spawn;

        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;   // ZodiacPlayerController tự tính trọng lực
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var body = go.AddComponent<CapsuleCollider>();
        body.direction = 1;
        body.radius = Mathf.Max(0.2f, fallback.colliderSize.x * 0.5f);
        body.height = Mathf.Max(body.radius * 2f, fallback.colliderSize.y);
        body.center = new Vector3(0f, body.height / 2f, 0f);
        body.sharedMaterial = ctx.noFriction;

        var health = go.AddComponent<Health>();
        health.maxHealth = fallback.maxHealth;
        health.invulnerableTime = 0.8f;

        // Player (vật lý) → Facing (lật trái/phải, chứa hitbox) → Tilt (ngả theo camera) → Visual (ảnh + CharacterAnimator)
        var facing = new GameObject("Facing").transform;
        facing.SetParent(go.transform, false);
        var tilt = new GameObject("Tilt");
        tilt.transform.SetParent(facing, false);
        tilt.AddComponent<Billboard>();
        var visual = new GameObject("Visual");
        visual.transform.SetParent(tilt.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = fallback.sprite;
        sr.sortingOrder = 10;
        sr.sharedMaterial = ZodiacVisualSetup.SpriteLit3D;
        sr.shadowCastingMode = ShadowCastingMode.On;
        sr.receiveShadows = true;

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
        // Bề dày Z = độ lệch làn tối đa vẫn chém trúng.
        var hitboxGo = new GameObject("AttackHitbox");
        hitboxGo.transform.SetParent(facing, false);
        hitboxGo.transform.localPosition = new Vector3(0.85f, 0.95f, 0f);
        var hitbox = hitboxGo.AddComponent<BoxCollider>();
        hitbox.isTrigger = true;
        hitbox.size = new Vector3(1.2f, 1.3f, 1.0f);
        hitbox.enabled = false;
        var slash = new GameObject("SlashEffect");
        slash.transform.SetParent(facing, false);
        slash.transform.localPosition = hitboxGo.transform.localPosition;
        slash.transform.localScale = new Vector3(hitbox.size.x, hitbox.size.y, 1f);
        var slashSr = slash.AddComponent<SpriteRenderer>();
        slashSr.sprite = ctx.circle;   // PlayerMeleeAttack đổi sang hình lưỡi liềm khi chạy
        slashSr.color = SlashColor;
        slashSr.sortingOrder = 11;
        slashSr.sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
        slashSr.shadowCastingMode = ShadowCastingMode.Off;
        slash.AddComponent<SlashEffect>();
        slash.SetActive(false);

        var attack = go.AddComponent<PlayerMeleeAttack>();
        attack.hitbox = hitbox;
        attack.slashEffect = slash;
        attack.targetLayers = 1 << ctx.enemyLayer;

        go.AddComponent<PlayerEnergy>();
        go.AddComponent<CharacterStats>();
        go.AddComponent<PlayerEquipment>();   // đồ đang mặc cộng chỉ số, phím 1/2 dùng nhanh
        var skill = go.AddComponent<PlayerSkill>();
        skill.targetLayers = 1 << ctx.enemyLayer;
        skill.groundLayer = 1 << ctx.groundLayer;
        go.AddComponent<PlayerDodge>();
        visual.AddComponent<CharacterAnimator>();   // sau attack/skill để đăng ký sự kiện

        var shadowGo = new GameObject("BlobShadow");
        shadowGo.transform.SetParent(go.transform, false);
        var shadowSr = shadowGo.AddComponent<SpriteRenderer>();
        shadowSr.sharedMaterial = ZodiacVisualSetup.UnlitMaterial;
        shadowSr.sortingOrder = -5;
        var blob = shadowGo.AddComponent<BlobShadow>();
        blob.target = go.transform;
        blob.size = 1.1f;
        blob.groundLayers = 1 << ctx.groundLayer;
        return pc;
    }

    // ───────────────────────────── HUD ─────────────────────────────

    static Transform BuildHud(ZodiacGameManager manager, ZodiacPlayerController player)
    {
        var canvas = ZodiacPrototypeBuilder.CreateCanvas();

        // Góc trái trên: chân dung tròn + tên + máu + Khí + nội tại
        var portraitRing = UiCircle(canvas, "PortraitRing", Color.white, new Vector2(0f, 1f), new Vector2(84f, -84f), 132f);
        var portraitMask = UiCircle(portraitRing.transform, "Mask", new Color(0.12f, 0.08f, 0.06f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, 120f);
        portraitMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        var portrait = ZodiacPrototypeBuilder.CreateImage(portraitMask.transform, "Portrait", Color.white);
        ZodiacPrototypeBuilder.Place(portrait.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(150f, 150f));
        portrait.raycastTarget = false;

        var characterText = ZodiacPrototypeBuilder.CreateText(canvas, "CharacterText", "", 32, Color.white,
            new Vector2(0f, 1f), new Vector2(165f, -18f), new Vector2(700f, 44f), TextAnchor.UpperLeft);
        var passive = ZodiacPrototypeBuilder.CreateText(canvas, "PassiveText", "", 22, Color.white,
            new Vector2(0f, 1f), new Vector2(165f, -128f), new Vector2(700f, 32f), TextAnchor.UpperLeft);

        var hud = canvas.gameObject.AddComponent<SkillHud>();
        hud.skill = player.GetComponent<PlayerSkill>();
        hud.attack = player.GetComponent<PlayerMeleeAttack>();
        hud.dodge = player.GetComponent<PlayerDodge>();
        hud.energy = player.GetComponent<PlayerEnergy>();
        hud.portrait = portrait;
        hud.portraitRing = portraitRing;
        hud.characterText = characterText;
        hud.passiveText = passive;

        // Thanh máu
        var back = ZodiacPrototypeBuilder.CreateImage(canvas, "PlayerHealthBar", BarBackColor);
        back.rectTransform.pivot = new Vector2(0f, 1f);
        ZodiacPrototypeBuilder.Place(back.rectTransform, new Vector2(0f, 1f), new Vector2(165f, -62f), new Vector2(360f, 34f));
        back.raycastTarget = false;
        var fill = ZodiacPrototypeBuilder.CreateImage(back.transform, "Fill", PlayerBarColor);
        ZodiacPrototypeBuilder.Stretch(fill.rectTransform);
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        fill.rectTransform.offsetMin = new Vector2(3f, 3f);
        fill.rectTransform.offsetMax = new Vector2(-3f, -3f);
        fill.raycastTarget = false;
        var label = ZodiacPrototypeBuilder.CreateText(back.transform, "Label", "Máu", 24, Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 34f), TextAnchor.MiddleCenter);
        var bar = back.gameObject.AddComponent<HealthBar>();
        bar.health = player.GetComponent<Health>();
        bar.fill = fill.rectTransform;
        bar.label = label;

        // Ô Khí dưới thanh máu (mỗi ô = một đoạn bằng nhau của thanh Khí)
        var pips = new Image[6];
        for (int i = 0; i < pips.Length; i++)
        {
            var pip = ZodiacPrototypeBuilder.CreateImage(canvas, $"EnergyPip {i + 1}", new Color(0.45f, 0.85f, 1f));
            pip.rectTransform.pivot = new Vector2(0f, 1f);
            ZodiacPrototypeBuilder.Place(pip.rectTransform, new Vector2(0f, 1f), new Vector2(165f + i * 32f, -104f), new Vector2(26f, 14f));
            pip.raycastTarget = false;
            pips[i] = pip;
        }
        hud.energyPips = pips;

        // Thanh chiêu giữa đáy màn hình: Q Đánh · W Lăn · E Chiêu · R Tuyệt kỹ
        hud.attackSlot = SkillSlot(canvas, "Slot Q", new Vector2(-310f, 105f), 96f, false, false);
        hud.dodgeSlot = SkillSlot(canvas, "Slot W", new Vector2(-175f, 105f), 96f, false, false);
        hud.skillSlot = SkillSlot(canvas, "Slot E", new Vector2(-20f, 112f), 112f, false, true);
        hud.ultimateSlot = SkillSlot(canvas, "Slot R", new Vector2(175f, 120f), 128f, true, true);

        manager.collectText = ZodiacPrototypeBuilder.CreateText(canvas, "CollectText", "Đồng xu: 0/0", 30, Color.white,
            new Vector2(0f, 1f), new Vector2(570f, -72f), new Vector2(400f, 40f), TextAnchor.UpperLeft);

        ZodiacPrototypeBuilder.CreateText(canvas, "HintText", "← ↑ → ↓ đi · Shift chạy · Space nhảy · Q đánh (nhảy + Q: đá bay) · F5 chơi lại", 24, Color.white,
            new Vector2(1f, 1f), new Vector2(-24f, -22f), new Vector2(1100f, 40f), TextAnchor.UpperRight);

        var go = ZodiacPrototypeBuilder.CreateText(canvas, "GoIndicator", "ĐI TIẾP →", 64, new Color(1f, 0.85f, 0.3f),
            new Vector2(1f, 0.5f), new Vector2(-60f, 120f), new Vector2(500f, 90f), TextAnchor.MiddleRight);
        go.fontStyle = FontStyle.Bold;
        go.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
        go.gameObject.AddComponent<GoIndicator>();

        var banner = ZodiacPrototypeBuilder.CreateText(canvas, "LevelBanner", $"{def.title}\n<size=34>{def.subtitle}</size>", 64,
            Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1400f, 200f), TextAnchor.MiddleCenter);
        banner.gameObject.AddComponent<TimedFade>().delay = 2.5f;
        var roomBanner = ZodiacPrototypeBuilder.CreateText(canvas, "RoomBanner", "", 48, new Color(1f, 0.9f, 0.6f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1200f, 140f), TextAnchor.MiddleCenter);
        roomBanner.gameObject.AddComponent<TimedFade>().delay = 1.6f;
        manager.roomBanner = roomBanner;

        BuildResultPanels(canvas, manager);
        ZodiacPrototypeBuilder.CreateEventSystem(null);
        return canvas;
    }

    /// <summary>Hình tròn UI (dùng hình tròn placeholder 256px).</summary>
    static Image UiCircle(Transform parent, string name, Color color, Vector2 anchor, Vector2 position, float size)
    {
        var img = ZodiacPrototypeBuilder.CreateImage(parent, name, color);
        img.sprite = ctx.circle;
        img.raycastTarget = false;
        ZodiacPrototypeBuilder.Place(img.rectTransform, anchor, position, new Vector2(size, size));
        return img;
    }

    /// <summary>
    /// Một ô chiêu tròn: viền (đổi màu khi sẵn sàng) → nền tối → biểu tượng (cắt tròn) → lớp tối quét tròn theo hồi chiêu,
    /// nhãn phím ở góc dưới trái, số Khí ở góc trên phải, tên chiêu bên dưới. Ô tuyệt kỹ viền vàng dày hơn.
    /// </summary>
    static SkillHud.Slot SkillSlot(Transform canvas, string name, Vector2 position, float size, bool ultimate, bool portraitIcon)
    {
        var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvas, false);
        ZodiacPrototypeBuilder.Place(root, new Vector2(0.5f, 0f), position, new Vector2(size, size));

        float border = ultimate ? 9f : 6f;
        var ring = UiCircle(root, "Ring", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        var bg = UiCircle(root, "Back", new Color(0.08f, 0.06f, 0.1f, 0.92f), new Vector2(0.5f, 0.5f), Vector2.zero, size - border * 2f);
        bg.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        var icon = ZodiacPrototypeBuilder.CreateImage(bg.transform, "Icon", Color.white);
        icon.raycastTarget = false;
        // Biểu tượng là chân dung nhân vật thì phóng to, lấy phần đầu + thân trên (ô tròn cắt bớt phần dưới)
        if (portraitIcon)
            ZodiacPrototypeBuilder.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -size * 0.42f), new Vector2(size * 1.7f, size * 1.7f));
        else
            ZodiacPrototypeBuilder.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.72f, size * 0.72f));
        var cd = UiCircle(bg.transform, "Cooldown", new Color(0f, 0f, 0f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, size - border * 2f);
        cd.type = Image.Type.Filled;
        cd.fillMethod = Image.FillMethod.Radial360;
        cd.fillOrigin = (int)Image.Origin360.Top;
        cd.fillClockwise = false;
        cd.fillAmount = 0f;

        var keyBadge = UiCircle(root, "KeyBadge", new Color(0.12f, 0.1f, 0.16f, 0.95f), new Vector2(0f, 0f), new Vector2(size * 0.12f, size * 0.12f), size * 0.38f);
        var key = ZodiacPrototypeBuilder.CreateText(keyBadge.transform, "Key", "", Mathf.RoundToInt(size * 0.22f), Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.38f, size * 0.38f), TextAnchor.MiddleCenter);
        key.fontStyle = FontStyle.Bold;

        var costBadge = UiCircle(root, "CostBadge", new Color(0.2f, 0.55f, 0.85f, 0.95f), new Vector2(1f, 1f), new Vector2(-size * 0.1f, -size * 0.1f), size * 0.32f);
        var cost = ZodiacPrototypeBuilder.CreateText(costBadge.transform, "Cost", "", Mathf.RoundToInt(size * 0.18f), Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.32f, size * 0.32f), TextAnchor.MiddleCenter);

        var label = ZodiacPrototypeBuilder.CreateText(root, "Label", "", ultimate ? 20 : 18, ultimate ? new Color(1f, 0.85f, 0.4f) : Color.white,
            new Vector2(0.5f, 0f), new Vector2(0f, -28f), new Vector2(size * 1.6f, 26f), TextAnchor.UpperCenter);
        label.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);

        return new SkillHud.Slot { root = root, icon = icon, cooldown = cd, ring = ring, key = key, label = label, cost = cost };
    }

    static void BuildResultPanels(Transform canvas, ZodiacGameManager manager)
    {
        manager.winPanel = ZodiacPrototypeBuilder.CreateResultPanel(canvas, "WinPanel", "CHIẾN THẮNG!", out Text winDetail,
            ("Về làng", manager.GoToVillage), (string.IsNullOrEmpty(def.nextScene) ? "Về làng" : "Màn tiếp theo", manager.NextLevel));
        manager.winDetailText = winDetail;
        manager.nextButtonLabel = manager.winPanel.transform.Find("Box/Button 2/Label").GetComponent<Text>();

        manager.losePanel = ZodiacPrototypeBuilder.CreateResultPanel(canvas, "LosePanel", "THUA RỒI!", out Text loseDetail,
            ("Hồi sinh tại đèn lồng", manager.ContinueFromCheckpoint), ("Về làng", manager.GoToVillage));
        loseDetail.text = "Bạn đã bị hạ gục";
        manager.loseDetailText = loseDetail;
    }
}
