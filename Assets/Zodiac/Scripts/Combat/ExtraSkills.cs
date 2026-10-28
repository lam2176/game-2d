using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Các ô chiêu dùng chung cho mọi con giáp, đủ thanh 5 chiêu kiểu Ngọc Rồng Online (Đánh · Chiêu · Tuyệt kỹ · 2 ô này)
/// cộng ô Đậu Thần:
///   • Thái Dương Hạ San (Z / LT): loé sáng — mọi quái quanh mình choáng (boss choáng ngắn hơn). Tốn Khí, hồi lâu.
///   • Tái Tạo Năng Lượng (X / D-pad ↑): hồi 30% máu và một phần Khí ngay. Hồi rất lâu.
///   • Đậu Thần (V / D-pad ↓): hồi đầy máu và Khí; 3 hạt mỗi lần vào màn, hồi ngắn.
/// Ô HUD dựng bằng code cạnh thanh chiêu của <see cref="SkillHud"/> (cùng kiểu: vành, biểu tượng, quét hồi chiêu,
/// số Khí); trên điện thoại <see cref="TouchControls"/> dời các ô này vào cụm nút cảm ứng.
/// </summary>
public class ExtraSkills : MonoBehaviour
{
    public enum Kind { SolarFlare, Regenerate, SenzuBean }

    public class Entry
    {
        public Kind kind;
        public string name, keyboard, gamepad;
        public int cost;
        public float cooldown;
        public Sprite icon;
        public Color color;
        public SkillHud.Slot slot;
        public InputAction action;
        public float readyAt;
        public float punch;
    }

    const float SolarRadius = 7f;
    const float SolarStun = 2.5f;
    const int BeansPerLevel = 3;

    public static ExtraSkills Current { get; private set; }
    public Entry[] Entries { get; private set; }
    public int Beans { get; private set; } = BeansPerLevel;

    ZodiacPlayerController controller;
    PlayerEnergy energy;
    Health health;
    Text beanCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Ensure();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure();

    /// <summary>Gắn vào người chơi của màn (nếu chưa có) và dựng ô HUD. Null nếu màn không có người chơi / thanh chiêu.</summary>
    public static ExtraSkills Ensure()
    {
        var player = ZodiacPlayerController.Current;
        if (player == null) return null;
        if (player.TryGetComponent(out ExtraSkills existing)) return existing;
        var hud = FindAnyObjectByType<SkillHud>();
        if (hud == null) return null;
        var extra = player.gameObject.AddComponent<ExtraSkills>();
        extra.Build(hud);
        return extra;
    }

    void Awake()
    {
        Current = this;
        controller = GetComponent<ZodiacPlayerController>();
        energy = GetComponent<PlayerEnergy>();
        health = GetComponent<Health>();
        Entries = new[]
        {
            new Entry { kind = Kind.SolarFlare, name = "Thái Dương Hạ San", keyboard = "<Keyboard>/z", gamepad = "<Gamepad>/leftTrigger",
                        cost = 25, cooldown = 14f, icon = TouchArt.Sun, color = new Color(1f, 0.92f, 0.45f) },
            new Entry { kind = Kind.Regenerate, name = "Tái Tạo Năng Lượng", keyboard = "<Keyboard>/x", gamepad = "<Gamepad>/dpad/up",
                        cost = 0, cooldown = 25f, icon = TouchArt.Regen, color = new Color(0.45f, 1f, 0.6f) },
            new Entry { kind = Kind.SenzuBean, name = "Đậu Thần", keyboard = "<Keyboard>/v", gamepad = "<Gamepad>/dpad/down",
                        cost = 0, cooldown = 8f, icon = TouchArt.Bean, color = new Color(0.55f, 0.9f, 0.35f) },
        };
        foreach (var e in Entries)
        {
            e.action = new InputAction(e.name, InputActionType.Button);
            e.action.AddBinding(e.keyboard);
            e.action.AddBinding(e.gamepad);
            e.action.Enable();
        }
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
        if (Entries == null) return;
        foreach (var e in Entries) e.action?.Dispose();
    }

    /// <summary>Đường dẫn nút tay cầm của một ô (nút cảm ứng giả lập nút này).</summary>
    public string PadPath(Kind kind) => Array.Find(Entries, e => e.kind == kind)?.gamepad;

    // ───────── Dùng chiêu ─────────

    void Update()
    {
        foreach (var e in Entries)
        {
            if (e.action.WasPressedThisFrame()) TryUse(e);
            UpdateSlot(e);
        }
    }

    bool CanAct => controller.ControlEnabled && !controller.IsKnockedDown && !health.IsDead && Time.timeScale > 0f;

    public bool TryUse(Entry e)
    {
        if (!CanAct || Time.time < e.readyAt) return false;
        if (e.kind == Kind.SenzuBean && Beans <= 0) return false;
        if (e.kind != Kind.SolarFlare && health.Current >= health.maxHealth && (energy == null || energy.Current >= energy.max))
        {
            DamageNumbers.Show(controller.BodyCenter + Vector3.up, "Đầy", e.color, 0.8f);   // khỏi phí đậu / hồi chiêu
            return false;
        }
        if (e.cost > 0 && (energy == null || !energy.TrySpend(e.cost))) return false;
        e.readyAt = Time.time + e.cooldown;
        e.punch = 1f;
        switch (e.kind)
        {
            case Kind.SolarFlare: SolarFlare(); break;
            case Kind.Regenerate: Restore(0.3f, 0.4f, e.color); break;
            case Kind.SenzuBean:
                Beans--;
                Restore(1f, 1f, e.color);
                break;
        }
        return true;
    }

    void SolarFlare()
    {
        Vector3 center = controller.BodyCenter;
        ZodiacFx.Ring(center, SolarRadius, new Color(1f, 0.95f, 0.6f), 0.45f);
        ZodiacFx.Ring(center, SolarRadius * 0.55f, Color.white, 0.3f);
        ZodiacParticles.Sparks(center, new Color(1f, 0.95f, 0.6f), 24);
        GameFeel.Shake(0.25f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.bossSlam, 0.6f, 0.05f, center);
        foreach (var enemy in EnemyBase.Active.ToArray())
        {
            if (enemy == null || enemy.Health == null || enemy.Health.IsDead) continue;
            var d = enemy.transform.position - transform.position;
            if (new Vector2(d.x, d.z * 1.5f).magnitude > SolarRadius) continue;
            enemy.Stun(enemy is BossBase ? SolarStun * 0.4f : SolarStun);
        }
    }

    void Restore(float healthPart, float energyPart, Color color)
    {
        health.Heal(Mathf.CeilToInt(health.maxHealth * healthPart));
        if (energy != null) energy.Gain(Mathf.CeilToInt(energy.max * energyPart));
        Vector3 center = controller.BodyCenter;
        ZodiacFx.Ring(center, 1.8f, color, 0.4f);
        ZodiacParticles.Sparks(center, color, 14);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.lantern, 0.7f, 0.05f, center);
    }

    // ───────── Ô HUD ─────────

    void Build(SkillHud hud)
    {
        var canvas = hud.transform;   // SkillHud nằm trên canvas HUD
        for (int i = 0; i < Entries.Length; i++)
        {
            var e = Entries[i];
            e.slot = MakeSlot(canvas, e, new Vector2(330f + i * 130f, 105f), 92f);
        }
        // Số hạt đậu còn lại thay chỗ số Khí
        var bean = Array.Find(Entries, x => x.kind == Kind.SenzuBean);
        beanCount = bean.slot.cost;
        bean.slot.cost.transform.parent.GetComponent<Image>().color = new Color(0.3f, 0.6f, 0.2f, 0.95f);
        RefreshKeys();
        ZodiacInput.Changed += RefreshKeys;
    }

    void OnDisable() => ZodiacInput.Changed -= RefreshKeys;

    void RefreshKeys()
    {
        if (Entries == null) return;
        foreach (var e in Entries)
        {
            if (e.slot?.key == null) continue;
            string path = ZodiacInput.UsingGamepad ? e.gamepad : e.keyboard;
            e.slot.key.text = InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
    }

    /// <summary>Ô tròn giống ô chiêu của SkillHud: vành → nền tối → biểu tượng → lớp quét hồi chiêu, phím, số Khí, tên.</summary>
    static SkillHud.Slot MakeSlot(Transform parent, Entry e, Vector2 position, float size)
    {
        var root = new GameObject(e.name, typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        ZodiacUi.Place(root, new Vector2(0.5f, 0f), position, new Vector2(size, size));
        var ring = Circle(root, "Ring", e.color, size);
        var back = Circle(root, "Back", new Color(0.08f, 0.06f, 0.1f, 0.92f), size - 12f);
        var icon = Circle(back.transform, "Icon", e.color, size * 0.62f);
        icon.sprite = e.icon;
        var cd = Circle(back.transform, "Cooldown", new Color(0f, 0f, 0f, 0.62f), size - 12f);
        cd.type = Image.Type.Filled;
        cd.fillMethod = Image.FillMethod.Radial360;
        cd.fillOrigin = (int)Image.Origin360.Top;
        cd.fillClockwise = false;
        cd.fillAmount = 0f;

        var keyBadge = Circle(root, "KeyBadge", new Color(0.12f, 0.1f, 0.16f, 0.95f), size * 0.38f);
        ZodiacUi.Place(keyBadge.rectTransform, new Vector2(0f, 0f), new Vector2(size * 0.12f, size * 0.12f), new Vector2(size * 0.38f, size * 0.38f));
        var key = ZodiacUi.MakeText(keyBadge.transform, "", Mathf.RoundToInt(size * 0.22f), Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.38f, size * 0.38f));
        key.fontStyle = FontStyle.Bold;
        key.raycastTarget = false;
        key.horizontalOverflow = HorizontalWrapMode.Overflow;

        var costBadge = Circle(root, "CostBadge", new Color(0.2f, 0.55f, 0.85f, 0.95f), size * 0.34f);
        ZodiacUi.Place(costBadge.rectTransform, new Vector2(1f, 1f), new Vector2(-size * 0.1f, -size * 0.1f), new Vector2(size * 0.34f, size * 0.34f));
        var cost = ZodiacUi.MakeText(costBadge.transform, e.cost > 0 ? e.cost.ToString() : "", Mathf.RoundToInt(size * 0.19f), Color.white,
                                     new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.34f, size * 0.34f));
        cost.raycastTarget = false;
        costBadge.gameObject.SetActive(e.cost > 0 || e.kind == Kind.SenzuBean);

        var label = ZodiacUi.MakeText(root, e.name, 17, Color.white, new Vector2(0.5f, 0f), new Vector2(0f, -26f), new Vector2(size * 1.8f, 26f));
        label.raycastTarget = false;
        label.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
        return new SkillHud.Slot { root = root, icon = icon, cooldown = cd, ring = ring, key = key, label = label, cost = cost };
    }

    static Image Circle(Transform parent, string name, Color color, float size)
    {
        var img = ZodiacUi.MakeImage(parent, name, color);
        img.sprite = TouchArt.Disc;
        img.raycastTarget = false;
        ZodiacUi.Place(img.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
        return img;
    }

    void UpdateSlot(Entry e)
    {
        var s = e.slot;
        if (s == null || s.root == null) return;
        float remaining = Mathf.Max(0f, e.readyAt - Time.time);
        s.cooldown.fillAmount = e.cooldown > 0f ? remaining / e.cooldown : 0f;
        bool affordable = e.cost <= 0 || (energy != null && energy.CanSpend(e.cost));
        bool ready = remaining <= 0f && affordable && (e.kind != Kind.SenzuBean || Beans > 0);
        s.ring.color = ready ? e.color : new Color(0.35f, 0.35f, 0.4f, 0.9f);
        s.icon.color = ready ? e.color : new Color(e.color.r * 0.5f, e.color.g * 0.5f, e.color.b * 0.5f, 1f);
        if (e.kind == Kind.SenzuBean && beanCount != null) beanCount.text = Beans.ToString();
        e.punch = Mathf.MoveTowards(e.punch, 0f, Time.unscaledDeltaTime * 5f);
        s.root.localScale = Vector3.one * (1f + Mathf.Sin(Mathf.Min(1f, e.punch) * Mathf.PI) * 0.18f);
    }
}
