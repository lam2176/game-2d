using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Texture vẽ bằng code (chưa có hoạ sĩ) thay cho khối màu phẳng: đất có sỏi và vân, gạch đá có mạch vữa và vát cạnh,
/// viền cỏ có ngọn, bục tre có đốt, dãy núi/đồi có đường sống lưng tự nhiên (nhiễu fBm), sương mù, bầu trời chuyển màu.
/// Ảnh vẽ tông sáng gần trắng → tô màu theo từng màn bằng SpriteRenderer.color (một bộ texture dùng cho cả 3 màn).
/// Lát gạch được (tileable): nhiễu tuần hoàn theo chu kỳ ảnh nên ghép ô không lộ đường nối.
/// Lưu PNG ở Assets/Zodiac/Art/Generated; đã có thì dùng lại.
/// </summary>
public static class ZodiacTextureFactory
{
    const string Folder = "Assets/Zodiac/Art/Generated";
    /// <summary>Hệ số lấy mẫu: hàm vẽ tính trên lưới gốc, ảnh lưu ra lớn gấp Scale lần (toạ độ thực, không phóng pixel).</summary>
    const int Scale = 2;

    /// <summary>Gọi hàm vẽ ở toạ độ thực (x/scale) — hàm nhiễu liên tục nên chi tiết mịn hơn chứ không vỡ hạt.</summary>
    static System.Func<int, int, Color> Up(System.Func<float, float, Color> f, int scale = Scale) =>
        (x, y) => f((x + 0.5f) / scale - 0.5f, (y + 0.5f) / scale - 0.5f);

    public class Set
    {
        public Sprite soil, brick, grass, plank, ridge, hills, fog;
    }

    public static Set EnsureAll()
    {
        ZodiacPrototypeBuilder.EnsureFolder(Folder);
        return new Set
        {
            // Vẽ ở lưới gốc (128px…) rồi lấy mẫu dày gấp Scale lần → texture nét khi hiển thị (~100–200 px/unit)
            soil = Ensure("Soil", 128 * Scale, 128 * Scale, 64 * Scale, Up(Soil), tile: true),
            brick = Ensure("Brick", 128 * Scale, 128 * Scale, 64 * Scale, Up(Brick), tile: true),
            grass = Ensure("GrassEdge", 128 * Scale, 48 * Scale, 64 * Scale, Up(GrassEdge), tile: true, tileVertical: false),
            plank = Ensure("BambooPlank", 128 * Scale, 24 * Scale, 64 * Scale, Up(Plank), tile: true, tileVertical: false),
            ridge = Ensure("MountainRidge", 1024, 512, 64, Up((x, y) => Ridge(x, y, 512, 256, 0.55f, 4, 11), 2), tile: true, tileVertical: false),
            hills = Ensure("Hills", 1024, 256, 64, Up((x, y) => Ridge(x, y, 512, 128, 0.35f, 2, 23), 2), tile: true, tileVertical: false),
            fog = Ensure("Fog", 8, 128, 16, (x, y) => new Color(1f, 1f, 1f, Mathf.SmoothStep(1f, 0f, y / 127f)), tile: false),
        };
    }

    /// <summary>Bầu trời chuyển màu theo chiều dọc (đỉnh → chân trời), riêng cho từng màn.</summary>
    public static Sprite SkyGradient(string name, Color top, Color horizon)
    {
        ZodiacPrototypeBuilder.EnsureFolder(Folder);
        return Ensure("Sky_" + name, 4, 256, 32, (x, y) =>
        {
            float t = y / 255f;
            return Color.Lerp(horizon, top, Mathf.SmoothStep(0f, 1f, t));
        }, tile: false, overwrite: true);
    }

    // ───────────────────────────── Từng texture ─────────────────────────────

    static Color Soil(float x, float y)
    {
        float n = Fbm(x, y, 128, 16, 4, 1);
        float strata = Mathf.Sin((y + n * 24f) * 0.2f) * 0.06f;               // vân đất nằm ngang, uốn lượn
        float v = 0.78f + (n - 0.5f) * 0.35f + strata;
        float speck = Hash(Mathf.FloorToInt(x) + Mathf.FloorToInt(y) * 131, 5);  // hạt đất li ti
        if (speck > 0.97f) v -= 0.12f;
        else if (speck < 0.02f) v += 0.08f;
        // Sỏi nhỏ, thưa: < PebbleSize là nằm trong viên sỏi (khoảng cách đã chuẩn hoá theo bán kính ô)
        const float PebbleSize = 0.45f;
        float pebble = Cells(x, y, 128, 9, 3) / PebbleSize;
        if (pebble < 1f)
        {
            float dome = Mathf.Sqrt(1f - pebble * pebble);                     // viên sỏi tròn: sáng ở giữa
            v = 0.86f + dome * 0.1f + (Fbm(x, y, 128, 8, 2, 17) - 0.5f) * 0.06f;
        }
        else if (pebble < 1.35f) v -= 0.12f * (1.35f - pebble) / 0.35f;        // bóng tối quanh sỏi
        return Gray(v);
    }

    static Color Brick(float x, float y)
    {
        const int rowH = 32, brickW = 64, mortar = 3;
        int row = Mathf.FloorToInt(y / rowH);
        float bx = ((x + (row % 2) * brickW / 2f) % 128f + 128f) % 128f;
        float lx = bx % brickW, ly = ((y % rowH) + rowH) % rowH;
        bool isMortar = lx < mortar || ly < mortar;
        float n = Fbm(x, y, 128, 16, 3, 7);
        if (isMortar) return Gray(0.45f + n * 0.1f);
        int brickId = row * 7 + Mathf.FloorToInt((x + (row % 2) * brickW / 2f) / brickW) % 2;
        float tone = 0.8f + (Hash(brickId, 3) - 0.5f) * 0.14f;
        float bevel = (ly > rowH - 6 ? 0.08f : 0f) + (lx < mortar + 4 ? 0.04f : 0f) - (ly < mortar + 4 ? 0.07f : 0f);
        float wear = (Fbm(x, y, 128, 4, 2, brickId) - 0.5f) * 0.08f;           // bề mặt đá sần
        float chip = Cells(x, y, 128, 8, brickId % 5 + 40) < 0.35f ? -0.1f : 0f; // vết sứt nhỏ
        return Gray(tone + bevel + (n - 0.5f) * 0.12f + wear + chip);
    }

    /// <summary>Viền cỏ: dải cỏ dày ở đáy, ngọn cỏ nhọn chĩa lên với độ cao ngẫu nhiên (alpha), sáng dần về ngọn.</summary>
    static Color GrassEdge(float x, float y)
    {
        const int band = 16;
        float baseTop = band + (Fbm(x, 0, 128, 16, 2, 5) - 0.5f) * 6f;
        float shade = 0.75f + 0.25f * (y / 47f) + (Fbm(x, y, 128, 8, 2, 9) - 0.5f) * 0.1f;
        if (y < baseTop) return Gray(Mathf.Lerp(0.7f, shade, y / baseTop));
        // ngọn cỏ: mỗi 5px một ngọn, cao thấp khác nhau, nghiêng nhẹ
        for (int k = -1; k <= 1; k++)
        {
            int blade = Mathf.FloorToInt(x / 5f) + k;
            float height = baseTop + 6f + Hash(blade, 1) * 22f;
            float lean = (Hash(blade, 2) - 0.5f) * 0.5f;
            float cx = ((blade * 5 + 2.5f) % 128 + 128) % 128 + lean * (y - baseTop);
            float dx = Mathf.Abs(Mathf.DeltaAngle(cx / 128f * 360f, x / 128f * 360f)) / 360f * 128f;
            float width = 2.2f * (1f - (y - baseTop) / (height - baseTop));
            if (y < height && dx < width) return Gray(Mathf.Min(1f, shade + 0.1f));
        }
        return Color.clear;
    }

    static Color Plank(float x, float y)
    {
        float t = y / 23f;
        float tube = 0.7f + 0.3f * Mathf.Sin(t * Mathf.PI);                    // ống tre tròn: sáng giữa, tối mép
        if (y > 15 && y < 18) tube += 0.12f;                                    // vệt bóng
        float lx = ((x % 64f) + 64f) % 64f;
        if (lx < 3 || lx > 61) tube -= 0.25f;                                   // đốt tre
        else if (lx < 5 || lx > 59) tube += 0.08f;
        tube += (Fbm(x, y, 128, 32, 2, 13) - 0.5f) * 0.06f;
        return Gray(tube);
    }

    /// <summary>Dãy núi/đồi: đường sống lưng bằng nhiễu fBm 1 chiều tuần hoàn; sáng hơn ở đỉnh (sương), có vân.</summary>
    static Color Ridge(float x, float y, int w, int h, float amplitude, int octaves, int seed)
    {
        float ridge = Fbm(x, 0, w, w / 4, octaves, seed);
        float top = h * (0.35f + amplitude * ridge);
        if (y > top) return Color.clear;
        float edge = Mathf.Clamp01((top - y) / 2f);                             // khử răng cưa mép
        float depth = 1f - y / top;
        float v = 0.78f + 0.22f * (1f - depth) + (Fbm(x, y, w, 32, 3, seed + 1) - 0.5f) * 0.1f;
        return new Color(v, v, v, edge);
    }

    // ───────────────────────────── Nhiễu tuần hoàn ─────────────────────────────

    static float Hash(int i, int seed)
    {
        unchecked
        {
            uint h = (uint)(i * 374761393 + seed * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777215f;
        }
    }

    static float Lattice(int ix, int iy, int period, int seed) =>
        Hash(((ix % period + period) % period) + ((iy % period + period) % period) * 4099, seed);

    /// <summary>Nhiễu giá trị tuần hoàn (chu kỳ = size px) để lát ô liền mạch.</summary>
    static float ValueNoise(float x, float y, int size, int cell, int seed)
    {
        int period = Mathf.Max(1, size / cell);
        float fx = x / cell, fy = y / cell;
        int ix = Mathf.FloorToInt(fx), iy = Mathf.FloorToInt(fy);
        float tx = fx - ix, ty = fy - iy;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        float a = Mathf.Lerp(Lattice(ix, iy, period, seed), Lattice(ix + 1, iy, period, seed), tx);
        float b = Mathf.Lerp(Lattice(ix, iy + 1, period, seed), Lattice(ix + 1, iy + 1, period, seed), tx);
        return Mathf.Lerp(a, b, ty);
    }

    static float Fbm(float x, float y, int size, int cell, int octaves, int seed)
    {
        float sum = 0f, amp = 0.5f, norm = 0f;
        for (int o = 0; o < octaves && cell >= 1; o++)
        {
            sum += ValueNoise(x, y, size, cell, seed + o * 31) * amp;
            norm += amp;
            amp *= 0.5f;
            cell /= 2;
        }
        return sum / norm;
    }

    /// <summary>Khoảng cách (chuẩn hoá) tới tâm ô Voronoi gần nhất — dùng vẽ sỏi.</summary>
    static float Cells(float x, float y, int size, int cells, int seed)
    {
        float cellSize = (float)size / cells, best = 9f;
        int cx = Mathf.FloorToInt(x / cellSize), cy = Mathf.FloorToInt(y / cellSize);
        for (int i = -1; i <= 1; i++)
        for (int j = -1; j <= 1; j++)
        {
            int gx = cx + i, gy = cy + j;
            int wx = (gx % cells + cells) % cells, wy = (gy % cells + cells) % cells;
            float px = (gx + Hash(wx + wy * 97, seed)) * cellSize;
            float py = (gy + Hash(wx + wy * 97, seed + 1)) * cellSize;
            float r = 0.25f + Hash(wx + wy * 97, seed + 2) * 0.35f;
            best = Mathf.Min(best, Vector2.Distance(new Vector2(x, y), new Vector2(px, py)) / (cellSize * r));
        }
        return best;
    }

    static Color Gray(float v)
    {
        v = Mathf.Clamp01(v);
        return new Color(v, v, v, 1f);
    }

    // ───────────────────────────── Lưu + import ─────────────────────────────

    /// <summary>
    /// tileVertical = false: chỉ lát theo chiều ngang (viền cỏ, dãy núi) — kẹp mép dọc, nếu không mép trên sẽ lấy màu
    /// của hàng pixel dưới cùng (lọc bilinear quấn vòng) thành đường kẻ mảnh.
    /// </summary>
    static Sprite Ensure(string name, int w, int h, float ppu, System.Func<int, int, Color> pixel, bool tile, bool overwrite = false,
                         bool tileVertical = true)
    {
        string path = $"{Folder}/{name}.png";
        if (!overwrite && File.Exists(path))
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null && existing.texture.width == w && existing.texture.height == h)
            {
                ApplyWrap(path, tile, tileVertical);
                return existing;
            }
        }
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            pixels[y * w + x] = pixel(x, y);
        tex.SetPixels(pixels);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spritePixelsPerUnit = ppu;
        settings.spriteMeshType = SpriteMeshType.FullRect;   // cần cho DrawMode Tiled
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.wrapMode = tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        settings.filterMode = FilterMode.Bilinear;
        settings.mipmapEnabled = tile;
        settings.alphaIsTransparency = true;
        settings.npotScale = TextureImporterNPOTScale.None;
        importer.SetTextureSettings(settings);
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
        ApplyWrap(path, tile, tileVertical);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void ApplyWrap(string path, bool tile, bool tileVertical)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var u = tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        var v = tile && tileVertical ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        if (importer.wrapModeU == u && importer.wrapModeV == v) return;
        importer.wrapModeU = u;
        importer.wrapModeV = v;
        importer.SaveAndReimport();
    }
}
