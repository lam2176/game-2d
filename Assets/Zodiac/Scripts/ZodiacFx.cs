using UnityEngine;

/// <summary>
/// Hiệu ứng hình khối tạo lúc chạy (chưa có sprite/particle thật): vòng sóng âm, bụi, vệt lướt.
/// Sprite tròn/vuông trắng tự sinh bằng Texture2D, tô màu bằng SpriteRenderer.color.
/// Trong thế giới 3D: hình đứng quay về camera (nghiêng theo góc cúi), vòng sóng chấn có thể nằm phẳng trên mặt đất.
/// </summary>
public static class ZodiacFx
{
    static Sprite circle, square, crescent, ring, chevrons, softCircle;

    public static Sprite Circle => circle != null ? circle : circle = MakeSprite(128, true);
    /// <summary>Hình tròn mờ dần ra mép (bóng dưới chân, quầng sáng).</summary>
    public static Sprite SoftCircle => softCircle != null ? softCircle : softCircle = MakeSoftCircle(64);
    public static Sprite Square => square != null ? square : square = MakeSprite(4, false);
    /// <summary>Lưỡi liềm 1×1 unit (vệt chém), mũi nhọn hai đầu, dày ở giữa phía trước (+x).</summary>
    public static Sprite Crescent => crescent != null ? crescent : crescent = MakeCrescent(128);
    /// <summary>Hai mũi tên "»" (biểu tượng lướt trên HUD).</summary>
    public static Sprite Chevrons => chevrons != null ? chevrons : chevrons = MakeChevrons(128);

    static Sprite MakeChevrons(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float py = Mathf.Abs(y + 0.5f - size / 2f) / size;           // 0 ở giữa
            float a = 0f;
            foreach (float start in new[] { 0.18f, 0.48f })
            {
                // mũi tên ">": đỉnh ở giữa phải, hai cạnh chéo; dày 0.11
                float d = (x + 0.5f) / size - (start + (0.25f - py));
                if (py < 0.3f) a = Mathf.Max(a, Mathf.Clamp01((0.11f - Mathf.Abs(d - 0.055f) * 2f) * size * 0.5f));
            }
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    /// <summary>Vành khuyên 1×1 unit (sóng âm, sóng chấn) — rỗng giữa để không che nhân vật.</summary>
    public static Sprite RingSprite => ring != null ? ring : ring = MakeRing(128, 0.16f);

    static Sprite MakeRing(int size, float thickness)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        float r = size / 2f, w = r * thickness;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
            float edge = Mathf.Clamp01(r - d) * Mathf.Clamp01(d - (r - w));          // viền mịn hai mép
            float inner = Mathf.Clamp01((d - (r - w * 3f)) / (w * 2f)) * 0.25f;      // quầng mờ phía trong
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Max(edge, inner * Mathf.Clamp01(r - d)) * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    static Sprite MakeCrescent(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        float r = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
            float outer = r - Vector2.Distance(p, new Vector2(r, r));                  // trong hình tròn lớn
            float inner = Vector2.Distance(p, new Vector2(r - size * 0.22f, r)) - r * 0.92f;   // ngoài hình tròn lệch
            float a = Mathf.Clamp01(Mathf.Min(outer, inner) / 2f);
            float glow = Mathf.Clamp01(1f - Mathf.Abs(y + 0.5f - r) / r);               // sáng ở giữa, mờ ở hai mũi
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * Mathf.Lerp(0.35f, 1f, glow) * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    static Sprite MakeSoftCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        float r = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
            float a = Mathf.Clamp01(1f - d);
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.SmoothStep(0f, 1f, a * 1.6f) * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    static Sprite MakeSprite(int size, bool round)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = round ? FilterMode.Bilinear : FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        var pixels = new Color32[size * size];
        float r = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float a = round ? Mathf.Clamp01(r - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r))) : 1f;
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    /// <summary>Góc ngả về phía camera (theo góc cúi) để hình phẳng không bị dẹt khi nhìn chéo từ trên xuống.</summary>
    public static Quaternion FacingCamera
    {
        get
        {
            var cam = Camera.main;
            return cam != null ? Quaternion.Euler(cam.transform.eulerAngles.x, 0f, 0f) : Quaternion.identity;
        }
    }

    /// <summary>Tạo một hình (tròn hoặc vuông) đơn giản, quay về camera.</summary>
    public static SpriteRenderer Shape(string name, bool round, Vector3 position, Vector2 size, Color color,
                                       int sortingOrder = 15, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        else go.transform.rotation = FacingCamera;
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = round ? Circle : Square;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        UseUnlit(sr);
        return sr;
    }

    /// <summary>Hiệu ứng tự phát sáng: không bị ánh sáng / bóng tối làm tối.</summary>
    public static void UseUnlit(SpriteRenderer sr)
    {
        var res = ZodiacResources.Get;
        if (res != null && res.spriteUnlit != null) sr.sharedMaterial = res.spriteUnlit;
    }

    /// <summary>Vòng sóng lan rộng rồi mờ dần (sóng âm). flat = nằm phẳng trên mặt đất (sóng chấn, vùng nổ).</summary>
    public static void Ring(Vector3 position, float radius, Color color, float duration = 0.35f, bool flat = false)
    {
        var sr = Shape("FxRing", true, position, Vector2.one * 0.3f, color, 14);
        sr.sprite = RingSprite;
        if (flat) sr.transform.SetPositionAndRotation(position + Vector3.up * 0.05f, Quaternion.Euler(90f, 0f, 0f));
        sr.gameObject.AddComponent<FxFade>().Init(Vector2.one * 0.3f, Vector2.one * radius * 2f, duration);
    }

    /// <summary>Đám bụi nhỏ tại chân (đáp đất, lướt).</summary>
    public static void Puff(Vector3 position, Color color, float size = 0.8f)
    {
        var sr = Shape("FxPuff", true, position, Vector2.one * size * 0.4f, color, 14);
        sr.gameObject.AddComponent<FxFade>().Init(Vector2.one * size * 0.4f, new Vector2(size * 1.6f, size), 0.3f);
    }

    /// <summary>Bóng mờ của nhân vật để lại khi lướt.</summary>
    public static void Afterimage(SpriteRenderer source, Color color)
    {
        if (source == null || source.sprite == null) return;
        var go = new GameObject("FxAfterimage");
        go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        go.transform.localScale = source.transform.lossyScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.flipX = source.flipX;
        sr.color = color;
        sr.sortingOrder = source.sortingOrder - 1;
        UseUnlit(sr);
        go.AddComponent<FxFade>().Init(go.transform.localScale, go.transform.localScale, 0.25f);
    }
}

/// <summary>Đổi kích thước từ → tới và mờ dần, xong thì tự huỷ.</summary>
public class FxFade : MonoBehaviour
{
    Vector3 from, to;
    float duration, t;
    SpriteRenderer sr;
    Color start;

    public void Init(Vector2 fromScale, Vector2 toScale, float seconds)
    {
        sr = GetComponent<SpriteRenderer>();
        start = sr.color;
        from = new Vector3(fromScale.x, fromScale.y, 1f);
        to = new Vector3(toScale.x, toScale.y, 1f);
        duration = Mathf.Max(0.01f, seconds);
        transform.localScale = from;
    }

    void Update()
    {
        t += Time.deltaTime / duration;
        float e = 1f - (1f - t) * (1f - t);
        transform.localScale = Vector3.Lerp(from, to, e);
        sr.color = new Color(start.r, start.g, start.b, start.a * (1f - t));
        if (t >= 1f) Destroy(gameObject);
    }
}
