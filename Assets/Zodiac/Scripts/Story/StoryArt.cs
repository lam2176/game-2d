using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hình minh hoạ cho tranh kể chuyện, vẽ bằng code lúc chạy (chưa có tranh vẽ tay): cảnh nền (làng, đồng, đêm, Hồ Tây,
/// bình minh) và hình chính (Trống Đồng Thiên Âm, Hồ Tinh, mười hai Âm Phù). Gam màu ấm kiểu tranh Đông Hồ.
/// Mỗi hình vẽ một lần rồi giữ lại.
/// </summary>
public static class StoryArt
{
    public enum Kind { None, Drum, Fox, Shards, Village, Field, Night, Lake, Dawn }

    static readonly Dictionary<Kind, Sprite> backgrounds = new Dictionary<Kind, Sprite>();
    static readonly Dictionary<Kind, Sprite> icons = new Dictionary<Kind, Sprite>();

    public static readonly Color Paper = new Color(0.95f, 0.89f, 0.74f);
    public static readonly Color Ink = new Color(0.18f, 0.1f, 0.06f);
    public static readonly Color Cinnabar = new Color(0.72f, 0.16f, 0.1f);

    /// <summary>Cảnh nền của khung tranh.</summary>
    public static Sprite Background(Kind kind)
    {
        if (backgrounds.TryGetValue(kind, out var s) && s != null) return s;
        const int w = 320, h = 160;
        var px = new Color[w * h];
        switch (kind)
        {
            case Kind.Village: Scene(px, w, h, new Color(1f, 0.72f, 0.4f), new Color(1f, 0.93f, 0.75f), new Color(0.45f, 0.55f, 0.25f), true, false); break;
            case Kind.Field: Scene(px, w, h, new Color(0.55f, 0.75f, 0.95f), new Color(0.9f, 0.95f, 1f), new Color(0.45f, 0.65f, 0.25f), false, true); break;
            case Kind.Night: Scene(px, w, h, new Color(0.05f, 0.07f, 0.2f), new Color(0.2f, 0.18f, 0.35f), new Color(0.12f, 0.12f, 0.15f), true, false); Moon(px, w, h); Stars(px, w, h); break;
            case Kind.Lake: Scene(px, w, h, new Color(0.06f, 0.12f, 0.22f), new Color(0.2f, 0.32f, 0.4f), new Color(0.1f, 0.25f, 0.32f), false, false); Water(px, w, h, new Color(0.2f, 0.45f, 0.55f)); Lamp(px, w, h); break;
            case Kind.Dawn: Scene(px, w, h, new Color(0.45f, 0.3f, 0.55f), new Color(1f, 0.7f, 0.35f), new Color(0.3f, 0.25f, 0.3f), false, false); Sun(px, w, h); Water(px, w, h, new Color(0.95f, 0.6f, 0.35f)); break;
            default: PaperFill(px, w, h); break;
        }
        s = Make(px, w, h);
        backgrounds[kind] = s;
        return s;
    }

    /// <summary>Hình chính đặt giữa khung (trống đồng, Hồ Tinh, Âm Phù) — null nếu cảnh không có.</summary>
    public static Sprite Icon(Kind kind)
    {
        if (kind != Kind.Drum && kind != Kind.Fox && kind != Kind.Shards) return null;
        if (icons.TryGetValue(kind, out var s) && s != null) return s;
        const int n = 256;
        var px = new Color[n * n];
        if (kind == Kind.Drum) Drum(px, n);
        else if (kind == Kind.Fox) FoxHead(px, n);
        else Shards(px, n);
        s = Make(px, n, n);
        icons[kind] = s;
        return s;
    }

    // ───────── Vẽ ─────────

    static Sprite Make(Color[] px, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    static float Noise(int x, int y) => Mathf.PerlinNoise(x * 0.07f, y * 0.07f);

    static void PaperFill(Color[] px, int w, int h)
    {
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = Paper * (0.93f + 0.07f * Noise(x * 3, y * 3));
    }

    /// <summary>Trời chuyển màu + đồi xa + mặt đất; roofs = dãy mái đình, furrows = luống cày.</summary>
    static void Scene(Color[] px, int w, int h, Color skyTop, Color skyBottom, Color ground, bool roofs, bool furrows)
    {
        int horizon = h * 2 / 5;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c;
                if (y > horizon) c = Color.Lerp(skyBottom, skyTop, (y - horizon) / (float)(h - horizon));
                else c = ground * (0.85f + 0.15f * Noise(x, y));
                // Đồi xa
                float hill = horizon + 10f + Mathf.Sin(x * 0.03f) * 8f + Mathf.Sin(x * 0.011f + 1f) * 10f;
                if (y > horizon && y < hill) c = Color.Lerp(c, ground * 0.6f + skyBottom * 0.2f, 0.6f);
                if (furrows && y < horizon && (x + y * 2) % 14 < 2) c *= 0.8f;
                px[y * w + x] = c;
            }
        if (!roofs) return;
        // Mái đình cong: hình thang tối màu, đầu đao vểnh
        Color roof = Color.Lerp(Ink, skyTop, 0.25f);
        for (int i = 0; i < 4; i++)
        {
            int cx = 40 + i * 80, top = horizon + 34 + (i % 2) * 8, half = 34;
            for (int y = horizon; y < top; y++)
            {
                float k = (y - horizon) / (float)(top - horizon);
                int span = Mathf.RoundToInt(Mathf.Lerp(half, half * 0.45f, k));
                for (int x = cx - span; x <= cx + span; x++)
                    if (x >= 0 && x < w) px[y * w + x] = roof;
            }
            for (int d = 0; d < 6; d++)
            {
                Set(px, w, h, cx - half - d, top - 14 + d, roof);
                Set(px, w, h, cx + half + d, top - 14 + d, roof);
            }
            Disc(px, w, h, cx, horizon - 6, 3, Cinnabar);   // đèn lồng
        }
    }

    static void Moon(Color[] px, int w, int h) => Disc(px, w, h, w - 60, h - 35, 16, new Color(1f, 0.95f, 0.75f));

    static void Stars(Color[] px, int w, int h)
    {
        var r = new System.Random(7);
        for (int i = 0; i < 60; i++) Set(px, w, h, r.Next(w), h * 2 / 5 + 20 + r.Next(h * 3 / 5 - 20), new Color(1f, 1f, 0.9f));
    }

    static void Sun(Color[] px, int w, int h) => Disc(px, w, h, w / 2, h * 2 / 5, 28, new Color(1f, 0.85f, 0.4f));

    static void Water(Color[] px, int w, int h, Color shine)
    {
        int horizon = h * 2 / 5;
        for (int y = 0; y < horizon; y++)
            for (int x = 0; x < w; x++)
            {
                var c = px[y * w + x];
                float ripple = Mathf.Sin(x * 0.15f + y * 0.9f) * 0.5f + 0.5f;
                px[y * w + x] = Color.Lerp(c * 0.7f, shine, ripple > 0.92f ? 0.6f : 0.1f);
            }
    }

    static void Lamp(Color[] px, int w, int h)
    {
        int cx = w / 2, cy = h / 5;
        for (int r = 30; r > 0; r--)
            Disc(px, w, h, cx, cy, r, Color.Lerp(px[cy * w + cx], new Color(1f, 0.8f, 0.4f), (30 - r) / 30f * 0.15f), true);
        Disc(px, w, h, cx, cy, 5, new Color(1f, 0.9f, 0.55f));
    }

    static void Drum(Color[] px, int n)
    {
        float c = (n - 1) / 2f, R = n * 0.46f;
        var bronze = new Color(0.72f, 0.5f, 0.22f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x - c, dy = y - c, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > R) { px[y * n + x] = Color.clear; continue; }
                float a = Mathf.Atan2(dy, dx);
                var col = bronze * (0.85f + 0.15f * Noise(x * 2, y * 2));
                // Vành khắc
                foreach (float ring in new[] { 0.95f, 0.78f, 0.6f, 0.42f })
                    if (Mathf.Abs(d - R * ring) < 2f) col = bronze * 0.55f;
                // Hoạ tiết răng cưa giữa các vành
                if (d > R * 0.78f && d < R * 0.95f && Mathf.Repeat(a * 24f / Mathf.PI, 2f) < 0.5f) col = bronze * 0.7f;
                // Mặt trời 12 tia ở giữa
                if (d < R * 0.12f) col = new Color(1f, 0.85f, 0.45f);
                else if (d < R * 0.4f && Mathf.Abs(Mathf.Sin(a * 6f)) > 0.93f - d / R * 0.2f) col = new Color(1f, 0.8f, 0.4f);
                // Chim Lạc (vệt cong) quanh vành giữa
                if (d > R * 0.6f && d < R * 0.75f && Mathf.Repeat(a * 4f / Mathf.PI, 2f) < 0.35f) col = bronze * 0.5f;
                px[y * n + x] = col;
            }
    }

    static void FoxHead(Color[] px, int n)
    {
        var fur = new Color(0.85f, 0.25f, 0.12f);
        var light = new Color(1f, 0.85f, 0.7f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x - n / 2f) / (n / 2f), v = (y - n / 2f) / (n / 2f);
                Color col = Color.clear;
                // Đầu (hình thoi bo tròn hướng xuống) + hai tai tam giác
                float head = Mathf.Abs(u) * 1.1f + Mathf.Max(0f, -v) * 1.3f + Mathf.Max(0f, v - 0.2f) * 1.6f;
                bool earL = v > 0.1f && v < 0.95f && u < -0.15f && u > -0.75f && (u + 0.45f) * (u + 0.45f) * 6f < (0.95f - v);
                bool earR = v > 0.1f && v < 0.95f && u > 0.15f && u < 0.75f && (u - 0.45f) * (u - 0.45f) * 6f < (0.95f - v);
                if (head < 0.9f || earL || earR) col = fur * (0.85f + 0.15f * Noise(x * 2, y * 2));
                if (head < 0.9f && v < -0.2f && Mathf.Abs(u) < 0.5f + v * 0.3f) col = light;   // mõm trắng
                if ((earL || earR) && Mathf.Abs(Mathf.Abs(u) - 0.45f) < 0.1f && v > 0.35f) col = new Color(0.4f, 0.08f, 0.06f);
                // Mắt xếch vàng
                if (Mathf.Abs(v - 0.05f + (Mathf.Abs(u) - 0.3f) * 0.5f) < 0.04f && Mathf.Abs(Mathf.Abs(u) - 0.3f) < 0.13f) col = new Color(1f, 0.85f, 0.2f);
                if (head < 0.9f && Mathf.Abs(u) < 0.06f && Mathf.Abs(v + 0.62f) < 0.05f) col = Ink;   // mũi
                px[y * n + x] = col;
            }
    }

    static void Shards(Color[] px, int n)
    {
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
        float c = n / 2f;
        for (int k = 0; k < 12; k++)
        {
            float a = k * Mathf.PI * 2f / 12f;
            int sx = Mathf.RoundToInt(c + Mathf.Cos(a) * n * 0.36f), sy = Mathf.RoundToInt(c + Mathf.Sin(a) * n * 0.36f);
            var glow = Color.HSVToRGB(k / 12f, 0.5f, 1f);
            for (int y = -22; y <= 22; y++)
                for (int x = -22; x <= 22; x++)
                {
                    float d = Mathf.Abs(x) * 1.6f + Mathf.Abs(y);
                    int px0 = sx + x, py0 = sy + y;
                    if (px0 < 0 || py0 < 0 || px0 >= n || py0 >= n) continue;
                    if (d < 16f) px[py0 * n + px0] = Color.Lerp(Color.white, glow, d / 16f);
                    else if (d < 24f) px[py0 * n + px0] = Color.Lerp(px[py0 * n + px0], new Color(glow.r, glow.g, glow.b, 0.5f), 1f - (d - 16f) / 8f);
                }
        }
    }

    static void Set(Color[] px, int w, int h, int x, int y, Color c)
    {
        if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = c;
    }

    static void Disc(Color[] px, int w, int h, int cx, int cy, int r, Color c, bool blend = false)
    {
        for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
                if (x * x + y * y <= r * r)
                {
                    int px0 = cx + x, py0 = cy + y;
                    if (px0 < 0 || py0 < 0 || px0 >= w || py0 >= h) continue;
                    px[py0 * w + px0] = blend ? Color.Lerp(px[py0 * w + px0], c, 0.5f) : c;
                }
    }
}
