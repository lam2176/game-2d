using System;
using UnityEngine;

/// <summary>
/// Hình vẽ lúc chạy cho điều khiển cảm ứng (khỏi cần ảnh): nền nút tròn đổ bóng, vành mảnh có quầng sáng,
/// biểu tượng nhảy / túm / tạm dừng / túi. Vẽ bằng hàm khoảng cách (SDF) nên mép mịn ở mọi cỡ.
/// </summary>
public static class TouchArt
{
    const int Size = 128;
    static Sprite disc, ring, glow, jump, hand, pause, bag, target;

    /// <summary>Đĩa tròn sáng giữa, tối dần ra mép (nhân màu bằng Image.color).</summary>
    public static Sprite Disc => disc != null ? disc : disc = Make(p =>
    {
        float d = p.magnitude;
        float a = Fill(1f - d);
        float shade = Mathf.Lerp(1f, 0.55f, d * d) + 0.12f * Mathf.Clamp01(p.y);   // sáng nhẹ nửa trên như mặt kính
        return new Color(shade, shade, shade, a);
    });

    /// <summary>Vành mảnh có quầng mờ phía trong.</summary>
    public static Sprite Ring => ring != null ? ring : ring = Make(p =>
    {
        float d = p.magnitude;
        float edge = Fill(1f - d) * Fill(d - 0.9f);
        float inner = Mathf.Clamp01((d - 0.6f) / 0.3f) * 0.35f * Fill(1f - d);
        return new Color(1f, 1f, 1f, Mathf.Max(edge, inner));
    });

    /// <summary>Quầng sáng mềm (nút đang bấm, tuyệt kỹ đầy Khí).</summary>
    public static Sprite Glow => glow != null ? glow : glow = Make(p =>
    {
        float d = p.magnitude;
        return new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - d), 2f));
    });

    /// <summary>Hai mũi tên hướng lên.</summary>
    public static Sprite Jump => jump != null ? jump : jump = Make(p =>
    {
        float a = Mathf.Max(Polyline(p, 0.11f, new Vector2(-0.48f, 0.05f), new Vector2(0f, 0.5f), new Vector2(0.48f, 0.05f)),
                            Polyline(p, 0.11f, new Vector2(-0.48f, -0.4f), new Vector2(0f, 0.05f), new Vector2(0.48f, -0.4f)));
        return new Color(1f, 1f, 1f, a);
    });

    /// <summary>Bàn tay xoè (túm / tương tác).</summary>
    public static Sprite Hand => hand != null ? hand : hand = Make(p =>
    {
        float a = Fill(-RoundBox(p - new Vector2(0.05f, -0.3f), new Vector2(0.34f, 0.28f), 0.14f));
        a = Mathf.Max(a, Capsule(p, new Vector2(-0.2f, -0.1f), new Vector2(-0.2f, 0.5f), 0.09f));
        a = Mathf.Max(a, Capsule(p, new Vector2(0.02f, -0.1f), new Vector2(0.02f, 0.62f), 0.09f));
        a = Mathf.Max(a, Capsule(p, new Vector2(0.24f, -0.1f), new Vector2(0.24f, 0.55f), 0.09f));
        a = Mathf.Max(a, Capsule(p, new Vector2(0.44f, -0.15f), new Vector2(0.44f, 0.36f), 0.08f));
        a = Mathf.Max(a, Capsule(p, new Vector2(-0.3f, -0.35f), new Vector2(-0.62f, 0.02f), 0.09f));
        return new Color(1f, 1f, 1f, a);
    });

    /// <summary>Tâm ngắm: vòng tròn, bốn vạch và chấm giữa (đổi mục tiêu).</summary>
    public static Sprite Target => target != null ? target : target = Make(p =>
    {
        float d = p.magnitude;
        float a = Fill(0.08f - Mathf.Abs(d - 0.5f));
        a = Mathf.Max(a, Fill(0.13f - d));
        a = Mathf.Max(a, Capsule(p, new Vector2(0f, 0.32f), new Vector2(0f, 0.78f), 0.06f));
        a = Mathf.Max(a, Capsule(p, new Vector2(0f, -0.32f), new Vector2(0f, -0.78f), 0.06f));
        a = Mathf.Max(a, Capsule(p, new Vector2(0.32f, 0f), new Vector2(0.78f, 0f), 0.06f));
        a = Mathf.Max(a, Capsule(p, new Vector2(-0.32f, 0f), new Vector2(-0.78f, 0f), 0.06f));
        return new Color(1f, 1f, 1f, a);
    });

    /// <summary>Hai vạch đứng.</summary>
    public static Sprite Pause => pause != null ? pause : pause = Make(p =>
    {
        float a = Mathf.Max(Fill(-RoundBox(p - new Vector2(-0.22f, 0f), new Vector2(0.11f, 0.42f), 0.05f)),
                            Fill(-RoundBox(p - new Vector2(0.22f, 0f), new Vector2(0.11f, 0.42f), 0.05f)));
        return new Color(1f, 1f, 1f, a);
    });

    /// <summary>Túi vải buộc miệng.</summary>
    public static Sprite Bag => bag != null ? bag : bag = Make(p =>
    {
        float body = Fill(-(Vector2.Scale(p - new Vector2(0f, -0.22f), new Vector2(1f, 1.15f)).magnitude - 0.5f) * 1f);
        float neck = Fill(-RoundBox(p - new Vector2(0f, 0.3f), new Vector2(0.16f, 0.1f), 0.04f));
        float top = Mathf.Max(Capsule(p, new Vector2(-0.3f, 0.5f), new Vector2(-0.08f, 0.38f), 0.08f),
                              Capsule(p, new Vector2(0.3f, 0.5f), new Vector2(0.08f, 0.38f), 0.08f));
        float tie = Fill(-RoundBox(p - new Vector2(0f, 0.2f), new Vector2(0.24f, 0.035f), 0.02f));
        float a = Mathf.Max(Mathf.Max(body, neck), top);
        a = Mathf.Min(a, 1f - tie * 0.8f);   // dây buộc khắc tối
        return new Color(1f, 1f, 1f, a);
    });

    // ───────── Vẽ ─────────

    /// <summary>p trong [-1, 1]; mép mịn khoảng 1 điểm ảnh.</summary>
    static Sprite Make(Func<Vector2, Color> shade)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            var p = new Vector2((x + 0.5f) / Size * 2f - 1f, (y + 0.5f) / Size * 2f - 1f);
            pixels[y * Size + x] = shade(p);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>Khoảng cách dương = bên trong → độ phủ 0..1 (mép rộng một điểm ảnh).</summary>
    static float Fill(float inside) => Mathf.Clamp01(inside * Size * 0.5f + 0.5f);

    static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        var ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Fill(r - Vector2.Distance(p, a + ab * t));
    }

    static float Polyline(Vector2 p, float r, params Vector2[] points)
    {
        float a = 0f;
        for (int i = 0; i + 1 < points.Length; i++) a = Mathf.Max(a, Capsule(p, points[i], points[i + 1], r));
        return a;
    }

    static float RoundBox(Vector2 p, Vector2 half, float r)
    {
        var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + new Vector2(r, r);
        return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }
}
