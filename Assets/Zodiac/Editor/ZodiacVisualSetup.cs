using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Thiết lập hình ảnh/âm thanh dùng chung khi dựng prototype:
///   • Chuyển URP sang 2D Renderer (để có Light2D) — thêm vào PC/Mobile RP asset và đặt làm mặc định.
///   • Tạo Assets/Zodiac/Resources/ZodiacResources.asset: vật liệu Sprite-Lit/Unlit + toàn bộ âm thanh.
///   • Volume profile hậu kỳ cho từng màn (bloom cho đèn lồng/lửa, vignette, chỉnh màu).
///   • Vật liệu hạt (đom đóm, bụi, lá) cho hiệu ứng môi trường lưu trong scene.
/// Âm thanh: Assets/Zodiac/Audio/SFX — Kenney.nl (CC0); Synth + Music — tổng hợp bằng code, không bản quyền.
/// </summary>
public static class ZodiacVisualSetup
{
    const string SettingsFolder = "Assets/Zodiac/Settings";
    const string Renderer2DPath = SettingsFolder + "/Zodiac2DRenderer.asset";
    const string ResourcesPath = "Assets/Zodiac/Resources/ZodiacResources.asset";
    const string AudioRoot = "Assets/Zodiac/Audio";
    const string UrpMaterials = "Packages/com.unity.render-pipelines.universal/Runtime/Materials";
    const string ParticleMaterialPath = "Assets/Zodiac/Art/Materials/ParticleGlow.mat";

    // ───────────────────────────── 2D Renderer ─────────────────────────────

    /// <summary>Đảm bảo mọi URP asset trong Quality Settings dùng 2D Renderer làm mặc định.</summary>
    public static void Ensure2DRenderer()
    {
        var data = AssetDatabase.LoadAssetAtPath<Renderer2DData>(Renderer2DPath);
        if (data == null)
        {
            ZodiacPrototypeBuilder.EnsureFolder(SettingsFolder);
            // Dùng đúng hàm của menu Create > Rendering > URP 2D Renderer (internal) để có đủ tài nguyên mặc định.
            var menus = Type.GetType("UnityEditor.Rendering.Universal.Renderer2DMenus, Unity.RenderPipelines.Universal.Editor");
            var create = menus?.GetMethod("CreateRendererAsset", BindingFlags.Static | BindingFlags.NonPublic);
            if (create != null)
            {
                var typeParam = create.GetParameters()[1].ParameterType;
                data = create.Invoke(null, new object[] { Renderer2DPath, Enum.Parse(typeParam, "_2DRenderer"), false, "Renderer" }) as Renderer2DData;
            }
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(data, Renderer2DPath);
            }
            AssetDatabase.SaveAssets();
        }

        var assets = new[] { GraphicsSettings.defaultRenderPipeline }
            .Concat(Enumerable.Range(0, QualitySettings.count).Select(QualitySettings.GetRenderPipelineAssetAt))
            .OfType<UniversalRenderPipelineAsset>().Distinct();
        foreach (var urp in assets)
        {
            var so = new SerializedObject(urp);
            var list = so.FindProperty("m_RendererDataList");
            int index = -1;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == data) index = i;
            if (index < 0)
            {
                index = list.arraySize;
                list.arraySize++;
                list.GetArrayElementAtIndex(index).objectReferenceValue = data;
            }
            so.FindProperty("m_DefaultRendererIndex").intValue = index;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urp);
        }
        AssetDatabase.SaveAssets();
    }

    public static Material LitMaterial => AssetDatabase.LoadAssetAtPath<Material>(UrpMaterials + "/Sprite-Lit-Default.mat");
    public static Material UnlitMaterial => AssetDatabase.LoadAssetAtPath<Material>(UrpMaterials + "/Sprite-Unlit-Default.mat");

    // ───────────────────────────── Tài nguyên lúc chạy ─────────────────────────────

    public static ZodiacResources EnsureResources()
    {
        var res = AssetDatabase.LoadAssetAtPath<ZodiacResources>(ResourcesPath);
        if (res == null)
        {
            ZodiacPrototypeBuilder.EnsureFolder(Path.GetDirectoryName(ResourcesPath).Replace('\\', '/'));
            res = ScriptableObject.CreateInstance<ZodiacResources>();
            AssetDatabase.CreateAsset(res, ResourcesPath);
        }
        ConfigureAudioImport();

        res.spriteLit ??= LitMaterial;
        res.spriteUnlit ??= UnlitMaterial;
        Fill(ref res.footstepGrass, "SFX/footstep_grass_00*");
        Fill(ref res.footstepStone, "SFX/footstep_concrete_00*");
        Fill(ref res.footstepWood, "SFX/footstep_wood_00*");
        Fill(ref res.jump, "SFX/cloth*");
        Fill(ref res.land, "SFX/impactSoft_medium_00*");
        Fill(ref res.swing, "Synth/swing");
        Fill(ref res.swingHeavy, "Synth/swing_heavy");
        Fill(ref res.hit, "SFX/impactPunch_medium_00*");
        Fill(ref res.hitHeavy, "SFX/impactPunch_heavy_00*");
        Fill(ref res.hurt, "SFX/impactSoft_heavy_00*");
        Fill(ref res.stomp, "SFX/impactPlank_medium_000");
        Fill(ref res.enemyDeath, "SFX/impactMining_00*");
        Fill(ref res.spike, "SFX/impactMetal_light_000");
        Fill(ref res.fireball, "Synth/fireball");
        Fill(ref res.bossRoar, "Synth/boss_roar");
        Fill(ref res.bossSlam, "Synth/skill_ox_gong");
        Fill(ref res.coin, "Synth/coin");
        Fill(ref res.lantern, "Synth/lantern");
        Fill(ref res.win, "Synth/win");
        Fill(ref res.lose, "Synth/lose");
        if (res.musicMenu == null) res.musicMenu = Clip("Music/music_menu");
        EditorUtility.SetDirty(res);
        AssetDatabase.SaveAssets();
        return res;
    }

    /// <summary>Lấy clip theo mẫu tên (dấu * ở cuối = mọi file bắt đầu bằng phần trước). Chỉ điền khi đang trống.</summary>
    static void Fill(ref AudioClip[] field, string pattern)
    {
        if (field != null && field.Length > 0 && field.All(c => c != null)) return;
        string folder = $"{AudioRoot}/{Path.GetDirectoryName(pattern).Replace('\\', '/')}";
        string name = Path.GetFileName(pattern);
        bool prefix = name.EndsWith("*");
        name = name.TrimEnd('*');
        field = AssetDatabase.FindAssets("t:AudioClip", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => prefix ? Path.GetFileNameWithoutExtension(p).StartsWith(name) : Path.GetFileNameWithoutExtension(p) == name)
            .OrderBy(p => p)
            .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
            .ToArray();
        if (field.Length == 0) Debug.LogWarning("Không tìm thấy âm thanh " + pattern);
    }

    public static AudioClip Clip(string relative)
    {
        var guid = AssetDatabase.FindAssets(Path.GetFileName(relative) + " t:AudioClip",
                                            new[] { $"{AudioRoot}/{Path.GetDirectoryName(relative).Replace('\\', '/')}" })
            .FirstOrDefault(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == Path.GetFileName(relative));
        return guid == null ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
    }

    /// <summary>Nhạc nền: phát trực tiếp từ đĩa (Streaming) cho nhẹ bộ nhớ; hiệu ứng ngắn: giải nén sẵn.</summary>
    static void ConfigureAudioImport()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) continue;
            bool music = path.Contains("/Music/");
            var s = importer.defaultSampleSettings;
            var load = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            if (s.loadType == load && importer.forceToMono == !music) continue;
            s.loadType = load;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = music ? 0.6f : 0.8f;
            importer.defaultSampleSettings = s;
            importer.forceToMono = !music;
            importer.SaveAndReimport();
        }
    }

    // ───────────────────────────── Hậu kỳ ─────────────────────────────

    /// <summary>Volume profile riêng cho từng màn: bloom (đèn lồng, lửa sáng loá), vignette, chỉnh màu.</summary>
    public static VolumeProfile EnsureVolumeProfile(string name, float bloom, float vignette, float saturation, float contrast, Color filter)
    {
        string path = $"{SettingsFolder}/Volume_{name}.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile != null) return profile;
        ZodiacPrototypeBuilder.EnsureFolder(SettingsFolder);
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        var b = profile.Add<Bloom>(true);
        b.threshold.Override(0.85f);
        b.intensity.Override(bloom);
        b.scatter.Override(0.6f);
        var v = profile.Add<Vignette>(true);
        v.intensity.Override(vignette);
        v.smoothness.Override(0.45f);
        var c = profile.Add<ColorAdjustments>(true);
        c.saturation.Override(saturation);
        c.contrast.Override(contrast);
        c.colorFilter.Override(filter);
        foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    /// <summary>Vật liệu hạt tự phát sáng (đom đóm, bụi hang, lá) — lưu trong scene nên cần là asset.</summary>
    public static Material EnsureParticleMaterial(Texture2D circle)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
        if (mat != null) return mat;
        ZodiacPrototypeBuilder.EnsureFolder(Path.GetDirectoryName(ParticleMaterialPath).Replace('\\', '/'));
        mat = new Material(UnlitMaterial) { mainTexture = circle };
        AssetDatabase.CreateAsset(mat, ParticleMaterialPath);
        return mat;
    }
}
