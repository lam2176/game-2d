using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Số bay lên kiểu Ngọc Rồng Online: quái trúng đòn hiện "-123" trắng viền đen (chí mạng to, màu vàng cam),
/// người chơi trúng đòn hiện số đỏ, hồi máu số xanh lá, tiềm năng "+N TN" xanh lam. Chữ nằm trên một canvas phủ màn hình,
/// mỗi khung chiếu lại vị trí thế giới ra màn hình nên vẫn bám theo khi camera chạy. Dựng sẵn một nhóm chữ để dùng lại.
/// </summary>
public class DamageNumbers : MonoBehaviour
{
    public static readonly Color EnemyHit = Color.white;
    public static readonly Color Crit = new Color(1f, 0.72f, 0.15f);
    public static readonly Color PlayerHit = new Color(1f, 0.3f, 0.25f);
    public static readonly Color Healed = new Color(0.45f, 1f, 0.45f);
    public static readonly Color Potential = new Color(0.45f, 0.85f, 1f);

    const float Life = 0.9f;

    class Number
    {
        public Text text;
        public Vector3 world;
        public float age, scale, drift;
    }

    static DamageNumbers instance;
    readonly List<Number> live = new List<Number>();
    readonly Stack<Text> pool = new Stack<Text>();
    RectTransform root;
    Camera cam;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        Health.AnyDamaged -= OnDamaged;
        Health.AnyDamaged += OnDamaged;
        Health.AnyHealed -= OnHealed;
        Health.AnyHealed += OnHealed;
    }

    /// <summary>1234567 → "1.234.567".</summary>
    public static string Format(long value) => value.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.');

    static void OnDamaged(Health target, Hit hit)
    {
        bool player = target.GetComponent<ZodiacPlayerController>() != null;
        if (!player && target.GetComponent<EnemyBase>() == null) return;   // bình, thùng… không hiện số
        var color = player ? PlayerHit : hit.crit ? Crit : EnemyHit;
        Show(Top(target), "-" + Format(hit.amount), color, hit.crit ? 1.45f : 1f);
    }

    static void OnHealed(Health target, int amount)
    {
        if (target.GetComponent<ZodiacPlayerController>() == null) return;
        Show(Top(target), "+" + Format(amount), Healed, 1f);
    }

    static Vector3 Top(Health h)
    {
        var col = h.GetComponentInChildren<Collider>();
        return col != null && col.enabled ? new Vector3(col.bounds.center.x, col.bounds.max.y + 0.3f, col.bounds.center.z)
                                          : h.transform.position + Vector3.up * 2f;
    }

    /// <summary>Hiện một số / chữ bay lên tại vị trí thế giới.</summary>
    public static void Show(Vector3 world, string content, Color color, float scale)
    {
        if (instance == null)
        {
            instance = new GameObject("DamageNumbers").AddComponent<DamageNumbers>();
            instance.Build();
        }
        instance.Spawn(world, content, color, scale);
    }

    void Build()
    {
        root = (RectTransform)ZodiacUi.Canvas(transform, "DamageCanvas", 95);
        root.GetComponent<GraphicRaycaster>().enabled = false;
    }

    void Spawn(Vector3 world, string content, Color color, float scale)
    {
        var text = pool.Count > 0 ? pool.Pop() : MakeText();
        text.gameObject.SetActive(true);
        text.text = content;
        text.color = color;
        text.fontSize = Mathf.RoundToInt(44 * scale);
        live.Add(new Number { text = text, world = world, age = 0f, scale = scale, drift = Random.Range(-0.35f, 0.35f) });
    }

    Text MakeText()
    {
        var t = ZodiacUi.MakeText(root, "", 44, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 90f));
        t.fontStyle = FontStyle.Bold;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        var outline = t.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);
        t.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -4f);
        return t;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        for (int i = live.Count - 1; i >= 0; i--)
        {
            var n = live[i];
            n.age += Time.unscaledDeltaTime;
            if (n.age >= Life || cam == null)
            {
                n.text.gameObject.SetActive(false);
                pool.Push(n.text);
                live.RemoveAt(i);
                continue;
            }
            float k = n.age / Life;
            // Nảy to rồi co lại, bay lên chậm dần, mờ ở 40% cuối
            var pos = n.world + new Vector3(n.drift * k, 1.1f * (1f - (1f - k) * (1f - k)), 0f);
            var screen = cam.WorldToScreenPoint(pos);
            if (screen.z <= 0f) { n.text.enabled = false; continue; }
            n.text.enabled = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local);
            n.text.rectTransform.anchoredPosition = local;
            float pop = k < 0.12f ? Mathf.Lerp(1.6f, 1f, k / 0.12f) : 1f;
            n.text.rectTransform.localScale = Vector3.one * pop;
            var c = n.text.color;
            c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            n.text.color = c;
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
