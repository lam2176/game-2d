using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Menu "Tools/Zodiac/Dựng prototype": chuyển URP sang Universal Renderer (3D: bóng đổ, sương, đèn điểm), tạo asset (placeholder,
/// 12 con giáp + roster, âm thanh, vật liệu, hậu kỳ, vật liệu vật lý) nếu chưa có, tạo lại prefab quái, dựng màn hình chọn nhân vật
/// Zodiac_Start và 3 màn beat 'em up 2.5D Zodiac_Level01..03 từ <see cref="ZodiacLevelData"/>, rồi đưa chúng lên đầu Build Settings.
/// Asset nhân vật/âm thanh đã có thì giữ nguyên (không mất chỉnh sửa tay); scene đã có thì hỏi trước khi ghi đè.
/// Không đụng tới ảnh gốc trong Assets/Characters.
///
/// Nhân vật chỉ có một ảnh tĩnh → animation tính bằng code (<see cref="CharacterAnimator"/>): co giãn, nghiêng, nhịp bước.
/// </summary>
public static class ZodiacPrototypeBuilder
{
    const string Root = "Assets/Zodiac";
    const string SceneFolder = Root + "/Scenes";
    const string StartScenePath = SceneFolder + "/Zodiac_Start.unity";
    const string PlaceholderFolder = Root + "/Art/Placeholder";
    internal const string SquarePath = PlaceholderFolder + "/Square.png";
    internal const string CirclePath = PlaceholderFolder + "/Circle.png";
    const string PhysicsFolder = Root + "/Physics";
    internal const string NoFrictionPath = PhysicsFolder + "/NoFriction.physicMaterial";
    /// <summary>Vật liệu vật lý 2D của bản platformer — xoá khi dựng lại.</summary>
    const string LegacyNoFriction2DPath = PhysicsFolder + "/NoFriction.physicsMaterial2D";

    static readonly Color MenuSkyColor = new Color(0.98f, 0.90f, 0.75f);
    static readonly Color TitleColor = new Color(0.70f, 0.08f, 0.08f);
    static readonly Color ButtonColor = new Color(0.80f, 0.15f, 0.12f);
    static readonly Color PanelColor = new Color(1f, 0.96f, 0.86f, 0.92f);
    static readonly Color InkColor = new Color(0.25f, 0.15f, 0.1f);

    static IEnumerable<string> ScenePaths =>
        new[] { StartScenePath }.Concat(ZodiacLevelData.Levels.Select(ZodiacLevelBuilder.ScenePath));

    // Menu mờ đi trong Play mode — dựng/lưu scene lúc đó sẽ lỗi InvalidOperationException.
    [MenuItem("Tools/Zodiac/Dựng prototype", true)]
    static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem("Tools/Zodiac/Dựng prototype")]
    public static void BuildInteractive() => Build(interactive: true);

    /// <summary>Gọi từ dòng lệnh: Unity -batchmode -executeMethod ZodiacPrototypeBuilder.BuildBatch -quit</summary>
    public static void BuildBatch() => Build(interactive: false);

    static void Build(bool interactive)
    {
        if (interactive)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var existing = ScenePaths.Where(File.Exists).ToArray();
            if (existing.Length > 0 &&
                !EditorUtility.DisplayDialog("Scene đã tồn tại",
                    string.Join("\n", existing) + "\n\nGhi đè sẽ mất mọi chỉnh sửa tay trong các scene này.", "Ghi đè", "Huỷ"))
                return;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (groundLayer < 0 || playerLayer < 0 || enemyLayer < 0)
        {
            Debug.LogError("Thiếu layer \"Ground\", \"Player\" hoặc \"Enemy\" trong Project Settings > Tags and Layers.");
            return;
        }

        ZodiacVisualSetup.EnsureUniversalRenderer();
        var ctx = new ZodiacLevelBuilder.Context
        {
            square = EnsurePlaceholderSprite(SquarePath, 4, circle: false),
            circle = EnsurePlaceholderSprite(CirclePath, 256, circle: true),
            resources = ZodiacVisualSetup.EnsureResources(),
            noFriction = EnsureNoFriction(),
            roster = ZodiacCharacterDefs.EnsureRoster(),
            groundLayer = groundLayer,
            playerLayer = playerLayer,
            enemyLayer = enemyLayer,
        };
        if (ctx.roster.Count == 0 || ctx.roster.characters.Any(c => c == null || c.sprite == null))
        {
            Debug.LogError("Thiếu asset hoặc sprite nhân vật — xem lỗi phía trên.");
            return;
        }
        ctx.particleMaterial = ZodiacVisualSetup.EnsureParticleMaterial(ctx.circle.texture);
        ctx.textures = ZodiacTextureFactory.EnsureAll();
        ctx.prefabs = ZodiacEnemyFactory.EnsurePrefabs(ctx.square, ctx.circle, ctx.noFriction, groundLayer, playerLayer, enemyLayer);

        foreach (var level in ZodiacLevelData.Levels)
        {
            EditorUtility.DisplayProgressBar("Zodiac", "Dựng " + level.title, 0.5f);
            ZodiacLevelBuilder.Build(level, ctx);
        }
        EditorUtility.ClearProgressBar();
        BuildStartScene(ctx.roster);
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(StartScenePath);
        Debug.Log("Đã dựng prototype: " + string.Join(", ", ScenePaths) + ". Bấm Play ở Zodiac_Start để chơi.");
    }

    // ───────────────────────────── Màn hình chọn nhân vật ─────────────────────────────

    static void BuildStartScene(ZodiacRoster roster)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(Vector3.zero, MenuSkyColor);

        var menu = new GameObject("StartMenu").AddComponent<ZodiacStartMenu>();
        menu.roster = roster;
        menu.levelSceneName = ZodiacLevelData.Levels[0].sceneName;
        var canvas = CreateCanvas();

        CreateText(canvas, "Title", "12 CON GIÁP", 96, TitleColor, new Vector2(0.5f, 1f), new Vector2(0f, -30f),
                   new Vector2(1400f, 120f), TextAnchor.MiddleCenter);
        CreateText(canvas, "Subtitle", "Chọn con giáp của bạn — mỗi con một nội tại và một chiêu riêng", 32, InkColor,
                   new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1400f, 50f), TextAnchor.MiddleCenter);

        // Bố cục (toạ độ chuẩn 1920×1080, gốc giữa màn hình) — hai khối không chồng nhau:
        //   lưới 6 × 2 ô: x −900 → +60, y +230 → −182 | bảng thông tin: x +160 → +880, y +300 → −340 (dưới dòng tiêu đề)
        const float slotW = 150f, slotH = 200f, gap = 12f, gridLeft = -900f, gridTop = 230f;
        int count = roster.Count;
        menu.slots = new Button[count];
        menu.slotFrames = new Image[count];
        for (int i = 0; i < count; i++)
        {
            var c = roster.characters[i];
            int col = i % 6, row = i / 6;
            var frame = CreateImage(canvas, $"Slot {i + 1}", new Color(0.35f, 0.2f, 0.12f, 0.6f));
            Place(frame.rectTransform, new Vector2(0.5f, 0.5f),
                  new Vector2(gridLeft + slotW / 2f + col * (slotW + gap), gridTop - slotH / 2f - row * (slotH + gap)), new Vector2(slotW, slotH));
            var inner = CreateImage(frame.transform, "Inner", PanelColor);
            Stretch(inner.rectTransform);
            inner.rectTransform.offsetMin = new Vector2(5f, 5f);
            inner.rectTransform.offsetMax = new Vector2(-5f, -5f);
            var portrait = CreateImage(inner.transform, "Portrait", Color.white);
            portrait.sprite = c.sprite;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            Place(portrait.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(slotW - 16f, 140f));
            CreateText(inner.transform, "Label", $"{c.zodiacName} · {c.displayName}", 22, InkColor,
                       new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(slotW, 36f), TextAnchor.LowerCenter);

            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = inner;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.95f, 0.8f);
            colors.selectedColor = new Color(1f, 0.9f, 0.7f);
            button.colors = colors;
            UnityEventTools.AddIntPersistentListener(button.onClick, menu.Choose, i);
            menu.slots[i] = button;
            menu.slotFrames[i] = frame;
        }

        // Bảng thông tin bên phải
        var panel = CreateImage(canvas, "InfoPanel", PanelColor);
        Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(520f, -20f), new Vector2(720f, 640f));
        panel.raycastTarget = false;
        var big = CreateImage(panel.transform, "Portrait", Color.white);
        big.preserveAspect = true;
        big.raycastTarget = false;
        // Trong bảng: chân dung trái trên, tên/nhạc cụ/chỉ số bên phải chân dung, nội tại và chiêu ở dưới
        Place(big.rectTransform, new Vector2(0f, 1f), new Vector2(125f, -130f), new Vector2(220f, 220f));
        menu.portrait = big;
        menu.nameText = CreateText(panel.transform, "Name", "", 42, InkColor, new Vector2(0f, 1f), new Vector2(255f, -25f),
                                   new Vector2(440f, 56f), TextAnchor.UpperLeft);
        menu.instrumentText = CreateText(panel.transform, "Instrument", "", 26, InkColor, new Vector2(0f, 1f), new Vector2(255f, -88f),
                                         new Vector2(440f, 36f), TextAnchor.UpperLeft);
        menu.statsText = CreateText(panel.transform, "Stats", "", 24, InkColor, new Vector2(0f, 1f), new Vector2(255f, -132f),
                                    new Vector2(440f, 100f), TextAnchor.UpperLeft);
        menu.passiveText = CreateText(panel.transform, "Passive", "", 24, InkColor, new Vector2(0f, 1f), new Vector2(28f, -255f),
                                      new Vector2(664f, 110f), TextAnchor.UpperLeft);
        menu.skillText = CreateText(panel.transform, "Skill", "", 22, InkColor, new Vector2(0f, 1f), new Vector2(28f, -370f),
                                    new Vector2(664f, 260f), TextAnchor.UpperLeft);
        foreach (var t in new[] { menu.passiveText, menu.skillText }) t.horizontalOverflow = HorizontalWrapMode.Wrap;

        var play = CreateButton(canvas, "PlayButton", "Bắt đầu", new Vector2(-585f, 110f));
        UnityEventTools.AddPersistentListener(play.onClick, new UnityAction(menu.Play));
        var quit = CreateButton(canvas, "QuitButton", "Thoát", new Vector2(-255f, 110f));
        UnityEventTools.AddPersistentListener(quit.onClick, new UnityAction(menu.Quit));
        foreach (var b in new[] { play, quit })
        {
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        }

        CreateText(canvas, "Controls", "Mũi tên / chuột: chọn con giáp · Enter: xác nhận · Trong màn: ← ↑ → ↓ đi · Space nhảy · Q đánh · W lăn · E chiêu · R tuyệt kỹ",
                   24, InkColor, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1800f, 40f), TextAnchor.MiddleCenter);

        CreateEventSystem(menu.slots.Length > 0 ? menu.slots[0].gameObject : play.gameObject);
        EnsureFolder(SceneFolder);
        EditorSceneManager.SaveScene(scene, StartScenePath);
    }

    // ───────────────────────────── UI dùng chung ─────────────────────────────

    internal static Camera CreateCamera(Vector3 position, Color background)
    {
        var cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = 5.5f;   // nhân vật to hơn trên màn hình → rõ nét vẽ
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.transform.position = new Vector3(position.x, position.y, -10f);
        cam.gameObject.AddComponent<AudioListener>();
        return cam;
    }

    internal static Transform CreateCanvas()
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

    internal static void CreateEventSystem(GameObject firstSelected)
    {
        var go = new GameObject("EventSystem");
        var es = go.AddComponent<EventSystem>();
        es.firstSelectedGameObject = firstSelected;
        go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    /// <summary>Bảng kết quả (thắng/thua): tiêu đề, dòng chi tiết và các nút "Box/Button 1", "Box/Button 2"...</summary>
    internal static GameObject CreateResultPanel(Transform canvas, string name, string title, out Text detail,
                                                 params (string label, UnityAction action)[] buttons)
    {
        var overlay = CreateImage(canvas, name, new Color(0f, 0f, 0f, 0.6f));
        Stretch(overlay.rectTransform);

        var box = CreateImage(overlay.transform, "Box", new Color(1f, 0.96f, 0.86f));
        Place(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 460f));

        CreateText(box.transform, "Title", title, 72, TitleColor, new Vector2(0.5f, 1f), new Vector2(0f, -50f),
                   new Vector2(760f, 100f), TextAnchor.MiddleCenter);
        detail = CreateText(box.transform, "Detail", "", 36, InkColor, new Vector2(0.5f, 1f),
                            new Vector2(0f, -170f), new Vector2(760f, 60f), TextAnchor.MiddleCenter);

        for (int i = 0; i < buttons.Length; i++)
        {
            float x = (i - (buttons.Length - 1) / 2f) * 370f;
            var button = CreateButton(box.transform, $"Button {i + 1}", buttons[i].label, new Vector2(x, 80f), 350f);
            UnityEventTools.AddPersistentListener(button.onClick, buttons[i].action);
            var rt = (RectTransform)button.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        }
        return overlay.gameObject;
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
        text.supportRichText = true;
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

    static Button CreateButton(Transform parent, string name, string label, Vector2 position, float width = 300f)
    {
        var image = CreateImage(parent, name, ButtonColor);
        Place(image.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(width, 90f));
        var button = image.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
        colors.selectedColor = new Color(1f, 0.85f, 0.6f);
        button.colors = colors;
        var text = CreateText(image.transform, "Label", label, 36, Color.white, new Vector2(0.5f, 0.5f),
                              Vector2.zero, new Vector2(width, 90f), TextAnchor.MiddleCenter);
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

    // ───────────────────────────── Asset ─────────────────────────────

    static PhysicsMaterial EnsureNoFriction()
    {
        AssetDatabase.DeleteAsset(LegacyNoFriction2DPath);
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(NoFrictionPath);
        if (mat != null) return mat;
        EnsureFolder(PhysicsFolder);
        mat = new PhysicsMaterial("NoFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum,
        };
        AssetDatabase.CreateAsset(mat, NoFrictionPath);
        return mat;
    }

    /// <summary>Ảnh placeholder trắng (ô vuông hoặc hình tròn) 1×1 unit, tô màu bằng SpriteRenderer.color.</summary>
    static Sprite EnsurePlaceholderSprite(string path, int pixels, bool circle)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null && sprite.texture.width == pixels) return sprite;   // khác cỡ (bản cũ 64px) thì vẽ lại, giữ GUID

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
                alpha = Mathf.Clamp01((r - d) / (pixels / 128f));   // viền mịn ~1 pixel khi hiển thị
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
        var paths = ScenePaths.ToList();
        var scenes = paths.Select(p => new EditorBuildSettingsScene(p, true)).ToList();
        scenes.AddRange(EditorBuildSettings.scenes.Where(s => !paths.Contains(s.path)));
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
