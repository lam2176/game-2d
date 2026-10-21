using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ảnh quái: tách nền trắng của ảnh gốc (vẽ bằng AI / tự vẽ, đặt ở Assets/Zodiac/Art/Enemies/Raw) thành sprite nền trong suốt
/// ở Assets/Zodiac/Art/Enemies. Nền được loang từ mép ảnh vào nên phần trắng nằm trong thân (ngực, đốm sáng) vẫn giữ;
/// viền giáp nền được làm mờ và khử quầng trắng. Ảnh cắt sát hình, pivot ở chân (quái bay / đồ vật: ở giữa).
/// Menu: Tools/Zodiac/Tách nền ảnh quái. <see cref="ZodiacEnemyFactory"/> dùng sprite nếu có, không thì ghép hình khối.
/// </summary>
public static class ZodiacEnemyArt
{
    public const string Folder = "Assets/Zodiac/Art/Enemies";
    const string RawFolder = Folder + "/Raw";

    /// <summary>Tên sprite pivot ở giữa (bay lơ lửng hoặc là đồ vật), còn lại pivot ở chân.</summary>
    static readonly HashSet<string> CenterPivot = new HashSet<string> { "MaTroi", "Giao", "Khien" };

    public static Sprite Get(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");

    [MenuItem("Tools/Zodiac/Tách nền ảnh quái")]
    public static void CleanAll()
    {
        if (!Directory.Exists(RawFolder))
        {
            Debug.LogWarning("Chưa có thư mục " + RawFolder);
            return;
        }
        foreach (var src in Directory.GetFiles(RawFolder, "*.png"))
        {
            string name = Path.GetFileNameWithoutExtension(src);
            Clean(src, $"{Folder}/{name}.png", CenterPivot.Contains(name));
        }
        AssetDatabase.Refresh();
        Debug.Log("Đã tách nền ảnh quái → " + Folder);
    }

    /// <summary>Gọi từ dòng lệnh: -executeMethod ZodiacEnemyArt.CleanBatch</summary>
    public static void CleanBatch()
    {
        CleanAll();
        EditorApplication.Exit(0);
    }

    static bool IsBackground(Color32 c)
    {
        int max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
        return min >= 228 && max - min <= 20;
    }

    static void Clean(string src, string dst, bool centerPivot)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(src));
        int w = tex.width, h = tex.height;
        var px = tex.GetPixels32();

        // Loang nền từ mép ảnh
        var bg = new bool[w * h];
        var queue = new Queue<int>();
        void Seed(int x, int y)
        {
            int i = y * w + x;
            if (bg[i] || !IsBackground(px[i])) return;
            bg[i] = true;
            queue.Enqueue(i);
        }
        for (int x = 0; x < w; x++) { Seed(x, 0); Seed(x, h - 1); }
        for (int y = 0; y < h; y++) { Seed(0, y); Seed(w - 1, y); }
        while (queue.Count > 0)
        {
            int i = queue.Dequeue(), x = i % w, y = i / w;
            if (x > 0) Seed(x - 1, y);
            if (x < w - 1) Seed(x + 1, y);
            if (y > 0) Seed(x, y - 1);
            if (y < h - 1) Seed(x, y + 1);
        }

        // Viền: pixel sáng sát nền → trong suốt một phần, khử phần trắng đã trộn vào màu
        var outPx = new Color32[w * h];
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            if (bg[i]) { outPx[i] = new Color32(0, 0, 0, 0); continue; }
            Color32 c = px[i];
            bool edge = (x > 0 && bg[i - 1]) || (x < w - 1 && bg[i + 1]) || (y > 0 && bg[i - w]) || (y < h - 1 && bg[i + w]);
            if (edge)
            {
                float white = Mathf.Min(c.r, Mathf.Min(c.g, c.b)) / 255f;
                float a = Mathf.Clamp01((1f - white) / 0.35f);
                if (a < 0.03f) { outPx[i] = new Color32(0, 0, 0, 0); continue; }
                byte Un(byte v) => (byte)Mathf.Clamp(Mathf.RoundToInt((v - 255f * (1f - a)) / a), 0, 255);
                c = new Color32(Un(c.r), Un(c.g), Un(c.b), (byte)(a * 255));
            }
            outPx[i] = c;
            if (c.a > 8)
            {
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
        }
        Object.DestroyImmediate(tex);
        if (maxX < 0) { Debug.LogWarning("Ảnh trống sau khi tách nền: " + src); return; }

        // Cắt sát hình (chừa 4px)
        const int pad = 4;
        minX = Mathf.Max(0, minX - pad); minY = Mathf.Max(0, minY - pad);
        maxX = Mathf.Min(w - 1, maxX + pad); maxY = Mathf.Min(h - 1, maxY + pad);
        int cw = maxX - minX + 1, ch = maxY - minY + 1;
        var crop = new Color32[cw * ch];
        for (int y = 0; y < ch; y++)
            System.Array.Copy(outPx, (y + minY) * w + minX, crop, y * cw, cw);
        var result = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
        result.SetPixels32(crop);
        result.Apply();
        ZodiacPrototypeBuilder.EnsureFolder(Folder);
        File.WriteAllBytes(dst, result.EncodeToPNG());
        Object.DestroyImmediate(result);

        AssetDatabase.ImportAsset(dst);
        var importer = (TextureImporter)AssetImporter.GetAtPath(dst);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = centerPivot ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, pad / (float)ch);
        settings.spritePixelsPerUnit = 512f;
        settings.spriteMeshType = SpriteMeshType.Tight;
        settings.alphaIsTransparency = true;
        settings.mipmapEnabled = true;
        settings.filterMode = FilterMode.Trilinear;
        importer.SetTextureSettings(settings);
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }
}
