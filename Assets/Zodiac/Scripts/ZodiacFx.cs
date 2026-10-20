using UnityEngine;

/// <summary>
/// Hiệu ứng hình khối tạo lúc chạy (chưa có sprite/particle thật): vòng sóng âm, bụi, vệt lướt.
/// Sprite tròn/vuông trắng tự sinh bằng Texture2D, tô màu bằng SpriteRenderer.color.
/// </summary>
public static class ZodiacFx
{
    static Sprite circle, square;

    public static Sprite Circle => circle != null ? circle : circle = MakeSprite(64, true);
    public static Sprite Square => square != null ? square : square = MakeSprite(4, false);

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

    /// <summary>Tạo một hình (tròn hoặc vuông) đơn giản.</summary>
    public static SpriteRenderer Shape(string name, bool round, Vector2 position, Vector2 size, Color color,
                                       int sortingOrder = 15, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = round ? Circle : Square;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    /// <summary>Vòng sóng lan rộng rồi mờ dần (sóng âm, sóng chấn).</summary>
    public static void Ring(Vector2 position, float radius, Color color, float duration = 0.35f)
    {
        var sr = Shape("FxRing", true, position, Vector2.one * 0.3f, color, 14);
        sr.gameObject.AddComponent<FxFade>().Init(Vector2.one * 0.3f, Vector2.one * radius * 2f, duration);
    }

    /// <summary>Đám bụi nhỏ tại chân (đáp đất, lướt).</summary>
    public static void Puff(Vector2 position, Color color, float size = 0.8f)
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
