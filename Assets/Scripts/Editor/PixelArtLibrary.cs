using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sinh sprite pixel art (nhân vật, quái, chưởng, đất) từ bản vẽ ký tự, lưu thành PNG trong Assets/Art/Generated.
/// Mỗi ký tự là 1 pixel, tra màu trong bảng màu; '.' là trong suốt. Bản vẽ từ trên xuống, nhân vật quay mặt sang phải.
/// Muốn sửa hình: sửa các chuỗi bên dưới rồi chạy lại menu "Tools > Tạo scene Demo 2D".
/// </summary>
public static class PixelArtLibrary
{
    public const int PixelsPerUnit = 16;
    const string Folder = "Assets/Art/Generated";

    // ---------- Nhân vật chính: võ sĩ băng đô đỏ, áo xanh ngọc ----------
    static readonly Dictionary<char, Color32> HeroPalette = new()
    {
        ['K'] = new Color32(30, 25, 40, 255),    // viền
        ['S'] = new Color32(255, 205, 160, 255), // da
        ['H'] = new Color32(40, 45, 80, 255),    // tóc
        ['B'] = new Color32(215, 40, 40, 255),   // băng đô
        ['J'] = new Color32(25, 155, 150, 255),  // áo
        ['W'] = new Color32(240, 240, 240, 255), // cổ áo
        ['Y'] = new Color32(240, 190, 50, 255),  // đai
        ['P'] = new Color32(75, 75, 95, 255),    // quần
        ['O'] = new Color32(115, 70, 40, 255),   // giày
    };

    static readonly string[] HeroHead =
    {
        "................",
        "....KKKKKK......",
        "...KHHHHHHK.....",
        "..KHHHHHHHHK....",
        "BBKBBBBBBBBK....",
        ".BKSSSSSSSSK....",
        "..KSSSKSSKSK....",
        "..KSSSKSSKSK....",
        "..KSSSSSSSSK....",
        "...KSSSSSSK.....",
        "....KKSSKK......",
    };

    static readonly string[] TorsoIdle =
    {
        "...KJJJJJJK.....",
        "..KJJJWWJJJK....",
        "..KJKJJJJKJK....",
        "..KSKJJJJKSK....",
        "..KKKJJJJKKK....",
        "...KKYYYYKK.....",
    };

    static readonly string[] TorsoAttack =
    {
        "...KJJJJJJK.....",
        "..KJJJWWJJJKKKK.",
        "..KJKJJJJJJSSSSK",
        "..KSKJJJJKKKKKK.",
        "..KKKJJJJK......",
        "...KKYYYYKK.....",
    };

    static readonly string[] LegsIdle =
    {
        "....KPPPPK......",
        "....KPPPPK......",
        "....KPKKPK......",
        "....KPKKPK......",
        "...KOOKKOOK.....",
        "...KOOKKOOK.....",
        "...KKKKKKKK.....",
    };

    static readonly string[] LegsRunA =
    {
        "....KPPPPK......",
        "...KPPKPPK......",
        "..KPPK.KPPK.....",
        ".KPPK...KPK.....",
        "KOOK....KOOK....",
        "KOOK....KOOK....",
        "KKK.....KKKK....",
    };

    static readonly string[] LegsRunB =
    {
        "....KPPPPK......",
        "....KPPPPK......",
        "....KPPPPK......",
        ".....KPPK.......",
        "....KOOOOK......",
        "....KOOOOK......",
        "....KKKKKK......",
    };

    static readonly string[] LegsJump =
    {
        "....KPPPPK......",
        "...KPPKKPPK.....",
        "...KPK..KPK.....",
        "..KOOK..KOOK....",
        "..KOOK..KOOK....",
        "..KKKK..KKKK....",
        "................",
    };

    static readonly string[] LegsFly =
    {
        "....KPPPPK......",
        "...KPPPPK.......",
        "..KPPPK.........",
        ".KPPK...........",
        "KOOK............",
        "KKK.............",
        "................",
    };

    // ---------- Quái: slime tím một mắt ----------
    static readonly Dictionary<char, Color32> SlimePalette = new()
    {
        ['K'] = new Color32(40, 20, 50, 255),
        ['M'] = new Color32(140, 75, 190, 255),
        ['L'] = new Color32(195, 140, 230, 255),
        ['W'] = new Color32(250, 250, 250, 255),
        ['D'] = new Color32(25, 10, 35, 255),
    };

    static readonly string[] Slime =
    {
        "................",
        "................",
        "................",
        "......KKKK......",
        "....KKMMMMKK....",
        "...KMMLLMMMMK...",
        "..KMMLLMMMMMMK..",
        "..KMMMWWWWMMMK..",
        ".KMMMWWDDWMMMMK.",
        ".KMMMWWDDWMMMMK.",
        ".KMMMMWWWWMMMMK.",
        ".KMMMMMMMMMMMMK.",
        "KMMMMMMMMMMMMMMK",
        "KMMMMMMMMMMMMMMK",
        ".KKKKKKKKKKKKKK.",
        "................",
    };

    // ---------- Chưởng ----------
    static readonly Dictionary<char, Color32> OrbPalette = new()
    {
        ['C'] = new Color32(90, 200, 255, 170),
        ['B'] = new Color32(150, 230, 255, 255),
        ['W'] = new Color32(240, 255, 255, 255),
    };

    static readonly string[] Orb =
    {
        "..CCCC..",
        ".CBBBBC.",
        "CBWWWWBC",
        "CBWWWWBC",
        "CBWWWWBC",
        "CBWWWWBC",
        ".CBBBBC.",
        "..CCCC..",
    };

    // ---------- Ô đất có cỏ ----------
    static readonly Dictionary<char, Color32> GroundPalette = new()
    {
        ['G'] = new Color32(90, 180, 75, 255),
        ['g'] = new Color32(60, 140, 55, 255),
        ['R'] = new Color32(130, 85, 50, 255),
        ['r'] = new Color32(100, 62, 38, 255),
    };

    static readonly string[] GroundTile =
    {
        "GGGGGGGGGGGGGGGG",
        "GgGGGGgGGGGGgGGG",
        "gGgGgGGgGgGGGgGg",
        "RgRRgRRRgRRgRRgR",
        "RRRRRRRRRRRRRRRR",
        "RRrRRRRRRRRrRRRR",
        "RRRRRRRrRRRRRRRR",
        "RRRRRRRRRRRRRrRR",
        "RrRRRRRRRRRRRRRR",
        "RRRRRRrRRRRRRRRR",
        "RRRRRRRRRRRRrRRR",
        "RRRRrRRRRRRRRRRR",
        "RRRRRRRRRRrRRRRR",
        "RRrRRRRRRRRRRRRR",
        "RRRRRRRRrRRRRRRR",
        "RRRRRRRRRRRRRRrR",
    };

    public class HeroSprites
    {
        public Sprite[] idle, run, jump, fly, attack;
    }

    public static HeroSprites BuildHero()
    {
        return new HeroSprites
        {
            idle = new[] { Save("Hero_Idle", Join(HeroHead, TorsoIdle, LegsIdle), HeroPalette) },
            run = new[]
            {
                Save("Hero_Run1", Join(HeroHead, TorsoIdle, LegsRunA), HeroPalette),
                Save("Hero_Run2", Join(HeroHead, TorsoIdle, LegsRunB), HeroPalette),
            },
            jump = new[] { Save("Hero_Jump", Join(HeroHead, TorsoIdle, LegsJump), HeroPalette) },
            fly = new[] { Save("Hero_Fly", Join(HeroHead, TorsoIdle, LegsFly), HeroPalette) },
            attack = new[] { Save("Hero_Attack", Join(HeroHead, TorsoAttack, LegsIdle), HeroPalette) },
        };
    }

    public static Sprite BuildSlime() => Save("Slime", Slime, SlimePalette);
    public static Sprite BuildOrb() => Save("KiOrb", Orb, OrbPalette);
    public static Sprite BuildGroundTile() => Save("GroundTile", GroundTile, GroundPalette);

    static string[] Join(params string[][] parts)
    {
        var all = new List<string>();
        foreach (var p in parts) all.AddRange(p);
        return all.ToArray();
    }

    static Sprite Save(string name, string[] rows, Dictionary<char, Color32> palette)
    {
        int h = rows.Length;
        int w = 0;
        foreach (var r in rows) w = Mathf.Max(w, r.Length);

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var clear = new Color32(0, 0, 0, 0);
        for (int y = 0; y < h; y++)
        {
            string row = rows[y];
            for (int x = 0; x < w; x++)
            {
                char c = x < row.Length ? row[x] : '.';
                // Texture có gốc ở dưới, bản vẽ có gốc ở trên nên lật trục y
                tex.SetPixel(x, h - 1 - y, palette.TryGetValue(c, out var col) ? col : clear);
            }
        }
        tex.Apply();

        if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Art", "Generated");

        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; // cần cho chế độ Tiled của mặt đất
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
