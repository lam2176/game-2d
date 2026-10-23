using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Thiết lập hình ảnh/âm thanh dùng chung khi dựng prototype:
///   • Chuyển URP sang Universal Renderer (3D: bóng đổ, đèn điểm, sương mù, SSAO) — đặt làm mặc định cho PC/Mobile RP asset.
///   • Vật liệu: sprite trong thế giới 3D (shader Zodiac/Sprite Lit 3D — nhận đèn, đổ bóng), vật liệu môi trường URP/Lit
///     (đất, đá, gỗ, ngói… lát bằng texture vẽ bằng code, tô màu theo từng màn).
///   • Tạo Assets/Zodiac/Resources/ZodiacResources.asset: vật liệu hiệu ứng + toàn bộ âm thanh.
///   • Volume profile hậu kỳ cho từng màn (bloom cho đèn lồng/lửa, vignette, chỉnh màu).
///   • Vật liệu hạt (đom đóm, bụi, lá) cho hiệu ứng môi trường lưu trong scene.
/// Âm thanh: Assets/Zodiac/Audio/SFX — Kenney.nl (CC0); Synth + Music — tổng hợp bằng code, không bản quyền.
/// </summary>
public static class ZodiacVisualSetup
{
    const string SettingsFolder = "Assets/Zodiac/Settings";
    const string UniversalRendererPath = "Assets/Settings/PC_Renderer.asset";
    const string ResourcesPath = "Assets/Zodiac/Resources/ZodiacResources.asset";
    const string AudioRoot = "Assets/Zodiac/Audio";
    const string UrpMaterials = "Packages/com.unity.render-pipelines.universal/Runtime/Materials";
    const string MaterialFolder = "Assets/Zodiac/Art/Materials";
    const string ParticleMaterialPath = MaterialFolder + "/ParticleGlow.mat";
    const string SpriteLit3DPath = MaterialFolder + "/SpriteLit3D.mat";

    // ───────────────────────────── Universal Renderer (3D) ─────────────────────────────

    /// <summary>
    /// Mọi URP asset trong Quality Settings dùng Universal Renderer làm mặc định (thay 2D Renderer của bản platformer),
    /// bật bóng đổ mềm của mặt trời.
    /// </summary>
    public static void EnsureUniversalRenderer()
    {
        var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(UniversalRendererPath);
        if (data == null)
        {
            Debug.LogError("Không tìm thấy " + UniversalRendererPath + " (renderer 3D của template URP).");
            return;
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
                if (list.GetArrayElementAtIndex(i).objectReferenceValue is UniversalRendererData) index = i;
            if (index < 0)
            {
                index = list.arraySize;
                list.arraySize++;
                list.GetArrayElementAtIndex(index).objectReferenceValue = data;
            }
            so.FindProperty("m_DefaultRendererIndex").intValue = index;
            so.FindProperty("m_MSAA").intValue = 4;   // khử răng cưa mép sprite cắt alpha
            so.FindProperty("m_MainLightShadowsSupported").boolValue = true;
            so.FindProperty("m_ShadowDistance").floatValue = 45f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urp);
        }
        AssetDatabase.SaveAssets();
    }

    /// <summary>Vật liệu cho mọi sprite đứng trong thế giới 3D (nhân vật, quái, đạo cụ dạng ảnh).</summary>
    public static Material SpriteLit3D
    {
        get
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(SpriteLit3DPath);
            if (mat != null)
            {
                mat.enableInstancing = false;   // màu SpriteRenderer đi qua màu đỉnh (xem ghi chú trong shader)
                return mat;
            }
            var shader = Shader.Find("Zodiac/Sprite Lit 3D");
            if (shader == null)
            {
                Debug.LogError("Thiếu shader Zodiac/Sprite Lit 3D (Assets/Zodiac/Shaders/SpriteLit3D.shader).");
                return LitMaterial;
            }
            ZodiacPrototypeBuilder.EnsureFolder(MaterialFolder);
            mat = new Material(shader) { enableInstancing = false };
            AssetDatabase.CreateAsset(mat, SpriteLit3DPath);
            return mat;
        }
    }

    /// <summary>
    /// Vật liệu môi trường URP/Lit (mặt đất, tường, mái…) — texture xám vẽ bằng code tô màu bằng color.
    /// Tạo lại mỗi lần dựng (tên riêng theo màn) để chỉnh bảng màu là thấy ngay.
    /// </summary>
    public static Material EnvMaterial(string name, Texture texture, Color color, float smoothness = 0.12f, Color? emission = null)
    {
        string path = $"{MaterialFolder}/Env_{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            ZodiacPrototypeBuilder.EnsureFolder(MaterialFolder);
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetTexture("_BaseMap", texture);
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", 0f);
        mat.enableInstancing = true;
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(mat);
        return mat;
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
        Fill(ref res.block, "SFX/impactBell_heavy_000");
        Fill(ref res.spike, "SFX/impactMetal_light_000");
        Fill(ref res.fireball, "Synth/fireball");
        Fill(ref res.bossRoar, "Synth/boss_roar");
        Fill(ref res.bossSlam, "Synth/skill_ox_gong");
        Fill(ref res.coin, "Synth/coin");
        Fill(ref res.lantern, "Synth/lantern");
        Fill(ref res.win, "Synth/win");
        Fill(ref res.lose, "Synth/lose");
        if (res.musicMenu == null) res.musicMenu = Clip("Music/music_menu");
        Fill(ref res.anvil, "Synth/anvil_*");
        Fill(ref res.forgeSuccess, "Synth/forge_success");
        Fill(ref res.forgeBreak, "Synth/forge_break");
        Fill(ref res.coinsHandle, "SFX/handleCoins");
        Fill(ref res.page, "Synth/page_*");
        Fill(ref res.uiClick, "Synth/ui_click");
        if (res.fireLoop == null) res.fireLoop = Clip("Synth/fire_loop");
        if (res.enemyArt == null || res.enemyArt.Length == 0 || res.enemyArt.Any(s => s == null))
            res.enemyArt = AssetDatabase.FindAssets("t:Sprite", new[] { ZodiacEnemyArt.Folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetDirectoryName(p).Replace('\\', '/') == ZodiacEnemyArt.Folder)   // bỏ thư mục Raw
                .Distinct()
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(s => s != null)
                .ToArray();
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

    /// <summary>
    /// Volume profile riêng cho từng màn: bloom (chỉ những gì sáng hơn trắng — đèn lồng, lửa — mới loá;
    /// ngưỡng thấp hơn 1 làm cả bầu trời sáng loá → hình bị mờ), vignette nhẹ, chỉnh màu. Tạo lại mỗi lần dựng.
    /// </summary>
    public static VolumeProfile EnsureVolumeProfile(string name, float bloom, float vignette, float saturation, float contrast, Color filter)
    {
        string path = $"{SettingsFolder}/Volume_{name}.asset";
        AssetDatabase.DeleteAsset(path);
        ZodiacPrototypeBuilder.EnsureFolder(SettingsFolder);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        var b = profile.Add<Bloom>(true);
        b.threshold.Override(1.05f);
        b.intensity.Override(bloom);
        b.scatter.Override(0.5f);
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
