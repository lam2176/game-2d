using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Lối đánh kiểu Ngọc Rồng Online cho người chơi:
///   • Mục tiêu: tự chọn quái gần nhất; chạm / bấm chuột vào quái để chọn; T (tay cầm: R3, nút cảm ứng ◎) đổi mục tiêu.
///     Mục tiêu có vòng sáng dưới chân, mũi tên trên đầu và bảng tên + máu ở giữa trên màn hình.
///   • Tự đánh: bấm Đánh khi có mục tiêu là nhân vật tự đánh liên tục cho tới khi quái gục hoặc người chơi tự đi.
///     Mục tiêu trong tầm chém (cùng làn) thì chém combo như cũ; ở xa / khác làn thì bắn chưởng đuổi theo mục tiêu.
///   • Sức mạnh (SM): con số tổng hợp chỉ số, hiện cạnh tên nhân vật, tăng khi lên cấp / mặc đồ.
/// Tự gắn vào người chơi ở mỗi màn (không cần sửa prefab / scene).
/// </summary>
public class PlayerTarget : MonoBehaviour
{
    const float AcquireRange = 14f;
    const float LoseRange = 18f;
    const float BlastRange = 14f;
    const float BlastCooldown = 0.42f;
    const float BlastDamage = 0.75f;   // × Công
    const float BlastSpeed = 17f;

    public static PlayerTarget Current { get; private set; }
    public EnemyBase Target { get; private set; }
    /// <summary>Đang tự đánh mục tiêu.</summary>
    public bool AutoAttacking { get; private set; }

    ZodiacPlayerController controller;
    PlayerMeleeAttack melee;
    CharacterStats stats;
    InputAction moveAction, switchAction;
    float nextBlast;

    readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    Transform ring, arrow;
    GameObject panel;
    Text panelName, panelHp;
    RectTransform panelFill;
    Text characterText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach();

    static void Attach()
    {
        var player = ZodiacPlayerController.Current;
        if (player != null && player.GetComponent<PlayerMeleeAttack>() != null && player.GetComponent<PlayerTarget>() == null)
            player.gameObject.AddComponent<PlayerTarget>();
    }

    void Awake()
    {
        Current = this;
        controller = GetComponent<ZodiacPlayerController>();
        melee = GetComponent<PlayerMeleeAttack>();
        stats = GetComponent<CharacterStats>();
        moveAction = InputSystem.actions.FindAction("Player/Move");
        switchAction = new InputAction("SwitchTarget", InputActionType.Button);
        switchAction.AddBinding("<Keyboard>/t");
        switchAction.AddBinding("<Gamepad>/rightStickPress");
        switchAction.Enable();
        melee.InterceptPress = OnAttackPressed;
        if (stats != null) stats.Changed += RefreshPower;
        BuildMarker();
        BuildPanel();
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
        switchAction?.Dispose();
        if (melee != null && melee.InterceptPress == OnAttackPressed) melee.InterceptPress = null;
        if (stats != null) stats.Changed -= RefreshPower;
        if (ring != null) Destroy(ring.gameObject);
        if (arrow != null) Destroy(arrow.gameObject);
    }

    System.Collections.IEnumerator Start()
    {
        yield return null;   // đợi SkillHud ghi tên nhân vật xong rồi mới thêm SM
        RefreshPower();
    }

    // ───────── Chọn mục tiêu ─────────

    static bool Valid(EnemyBase e) => e != null && e.isActiveAndEnabled && e.Health != null && !e.Health.IsDead;

    float Distance(EnemyBase e)
    {
        var d = e.transform.position - transform.position;
        return Mathf.Sqrt(d.x * d.x + 4f * d.z * d.z);   // lệch làn tính nặng hơn — ưu tiên quái cùng làn
    }

    EnemyBase Nearest(EnemyBase except, float range)
    {
        EnemyBase best = null;
        float bestDistance = range;
        foreach (var e in EnemyBase.Active)
        {
            if (!Valid(e) || e == except) continue;
            float d = Distance(e);
            if (d >= bestDistance) continue;
            best = e;
            bestDistance = d;
        }
        return best;
    }

    public void Select(EnemyBase enemy)
    {
        if (Target == enemy) return;
        Target = enemy;
        AutoAttacking = false;
    }

    /// <summary>Đổi sang quái gần nhất khác mục tiêu hiện tại.</summary>
    public void Cycle()
    {
        var next = Nearest(Target, LoseRange);
        if (next != null) Select(next);
    }

    /// <summary>Chạm / bấm chuột vào quái (không trúng giao diện) thì chọn quái đó.</summary>
    void PickByPointer()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var touch = Touchscreen.current;
        if (touch != null)
        {
            foreach (var t in touch.touches)
                if (t.press.wasPressedThisFrame) TryPick(cam, t.position.ReadValue(), t.touchId.ReadValue());
        }
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) TryPick(cam, mouse.position.ReadValue(), -1);
    }

    void TryPick(Camera cam, Vector2 screen, int pointerId)
    {
        // Chạm trúng nút / cửa sổ thì không chọn quái phía sau (hỏi thẳng EventSystem — cú chạm vừa bắt đầu
        // có thể chưa được module giao diện xử lý trong khung này).
        var es = EventSystem.current;
        if (es != null)
        {
            uiHits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screen, pointerId = pointerId }, uiHits);
            if (uiHits.Count > 0) return;
        }
        var hits = Physics.SphereCastAll(cam.ScreenPointToRay(screen), 0.5f, 200f, melee.targetLayers, QueryTriggerInteraction.Ignore);
        EnemyBase best = null;
        float bestDistance = float.MaxValue;
        foreach (var h in hits)
        {
            var e = h.collider.GetComponentInParent<EnemyBase>();
            if (!Valid(e) || h.distance >= bestDistance) continue;
            best = e;
            bestDistance = h.distance;
        }
        if (best != null) Select(best);
    }

    // ───────── Đánh ─────────

    bool InMeleeRange(EnemyBase e)
    {
        var d = e.transform.position - transform.position;
        return Mathf.Abs(d.x) <= melee.Reach + 0.3f && Mathf.Abs(d.z) <= melee.LaneReach;
    }

    void Face(EnemyBase e)
    {
        float dx = e.transform.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.05f) controller.SetFacing(Mathf.Sign(dx));
    }

    /// <summary>Bấm Đánh: có mục tiêu thì bật tự đánh; ở xa thì bắn chưởng (true = đã xử lý, không chém).</summary>
    bool OnAttackPressed()
    {
        if (!Valid(Target)) Target = Nearest(null, AcquireRange);
        if (!Valid(Target)) return false;
        AutoAttacking = true;
        Face(Target);
        if (InMeleeRange(Target)) return false;   // để PlayerMeleeAttack chém
        TryBlast();
        return true;
    }

    void TryBlast()
    {
        if (Time.time < nextBlast || !melee.CanSwing || Distance(Target) > BlastRange) return;
        nextBlast = Time.time + BlastCooldown;
        Face(Target);
        var character = controller.Character;
        Color color = character != null ? character.themeColor : new Color(0.5f, 0.85f, 1f);
        var targetCol = Target.GetComponentInChildren<Collider>();
        Vector3 aim = targetCol != null ? targetCol.bounds.center : Target.transform.position + Vector3.up;
        Vector3 origin = transform.position + new Vector3(controller.FacingDirection * 0.6f, 1.1f, 0f);
        int damage = Mathf.RoundToInt((stats != null ? stats.Attack : ZodiacBalance.Unit) * BlastDamage);
        var shot = Projectile.Spawn(origin, (aim - origin).normalized * BlastSpeed, 0.5f, color, melee.targetLayers, 0, damage, false);
        shot.attacker = stats;
        shot.passWalls = true;
        shot.range = BlastRange + 4f;
        shot.knockback = 0.4f;
        shot.homing = targetCol;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.fireball, 0.45f, 0.12f, origin);
    }

    void Update()
    {
        if (switchAction.WasPressedThisFrame()) Cycle();
        PickByPointer();

        if (Valid(Target) && Distance(Target) > LoseRange) Target = null;
        if (!Valid(Target))
        {
            // Mục tiêu gục / đi xa: chuyển sang quái gần nhất, đang tự đánh thì đánh tiếp con đó
            Target = Nearest(null, AcquireRange);
            if (Target == null) AutoAttacking = false;
        }

        // Tự đánh: người chơi tự đi (cần / phím) thì thôi
        if (AutoAttacking && moveAction != null && moveAction.ReadValue<Vector2>().magnitude > 0.4f) AutoAttacking = false;
        if (AutoAttacking && Valid(Target) && controller.ControlEnabled && !controller.IsKnockedDown)
        {
            if (InMeleeRange(Target))
            {
                if (melee.CanSwing)
                {
                    Face(Target);
                    melee.StartSwing();
                }
            }
            else TryBlast();
        }
    }

    void LateUpdate()
    {
        UpdateMarker();
        UpdatePanel();
    }

    // ───────── Dấu mục tiêu ─────────

    void BuildMarker()
    {
        var color = new Color(1f, 0.8f, 0.25f, 0.9f);
        ring = ZodiacFx.Shape("TargetRing", true, Vector3.zero, Vector2.one, color, 6).transform;
        ring.GetComponent<SpriteRenderer>().sprite = ZodiacFx.RingSprite;
        ring.rotation = Quaternion.Euler(90f, 0f, 0f);
        arrow = ZodiacFx.Shape("TargetArrow", true, Vector3.zero, Vector2.one * 0.55f, new Color(1f, 0.35f, 0.2f, 0.95f), 40).transform;
        arrow.GetComponent<SpriteRenderer>().sprite = ZodiacFx.Chevrons;
        arrow.rotation = Quaternion.Euler(0f, 0f, -90f);
    }

    void UpdateMarker()
    {
        bool show = Valid(Target);
        ring.gameObject.SetActive(show);
        arrow.gameObject.SetActive(show);
        if (!show) return;
        var col = Target.GetComponentInChildren<Collider>();
        var b = col != null ? col.bounds : new Bounds(Target.transform.position + Vector3.up, Vector3.one);
        float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 6f);
        float width = Mathf.Max(1.1f, b.size.x * 1.4f);
        ring.position = new Vector3(b.center.x, b.min.y + 0.05f, b.center.z);
        ring.localScale = new Vector3(width, width * 0.5f, 1f) * pulse;
        arrow.position = new Vector3(b.center.x, b.max.y + 0.55f + 0.12f * Mathf.Sin(Time.time * 5f), b.center.z);
        // Tự đánh: mũi tên đỏ; chỉ chọn: vàng
        arrow.GetComponent<SpriteRenderer>().color = AutoAttacking ? new Color(1f, 0.3f, 0.2f, 0.95f) : new Color(1f, 0.85f, 0.3f, 0.95f);
    }

    // ───────── Bảng mục tiêu + Sức mạnh ─────────

    void BuildPanel()
    {
        var canvas = ZodiacUi.Canvas(null, "TargetPanel", 96);
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        var back = ZodiacUi.MakeImage(canvas, "Panel", new Color(0.1f, 0.07f, 0.06f, 0.82f));
        back.raycastTarget = false;
        // Giữa trên, lệch phải để không đè dòng đồng xu / bảng hướng dẫn
        ZodiacUi.Place(back.rectTransform, new Vector2(0.5f, 1f), new Vector2(110f, -52f), new Vector2(440f, 78f));
        back.gameObject.AddComponent<Outline>().effectColor = new Color(0.95f, 0.78f, 0.35f, 0.8f);
        panel = back.gameObject;
        panelName = ZodiacUi.MakeText(back.transform, "", 26, ZodiacUi.Gold, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(420f, 34f));
        panelName.raycastTarget = false;
        var barBack = ZodiacUi.MakeImage(back.transform, "Bar", new Color(0f, 0f, 0f, 0.7f));
        barBack.raycastTarget = false;
        ZodiacUi.Place(barBack.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(420f, 24f));
        var fill = ZodiacUi.MakeImage(barBack.transform, "Fill", new Color(0.85f, 0.2f, 0.15f));
        fill.raycastTarget = false;
        ZodiacUi.Stretch(fill.rectTransform);
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        fill.rectTransform.offsetMin = new Vector2(2f, 2f);
        fill.rectTransform.offsetMax = new Vector2(-2f, -2f);
        panelFill = fill.rectTransform;
        panelHp = ZodiacUi.MakeText(barBack.transform, "", 18, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 24f));
        panelHp.raycastTarget = false;
        panelHp.gameObject.AddComponent<Outline>().effectColor = Color.black;
        panel.SetActive(false);
    }

    void UpdatePanel()
    {
        bool show = Valid(Target);
        if (panel.activeSelf != show) panel.SetActive(show);
        if (!show) return;
        var h = Target.Health;
        string elite = Target.Elite != null ? "<color=#ff9a3c>Tinh anh</color>  " : "";
        panelName.text = elite + Target.DisplayName;
        panelFill.localScale = new Vector3(Mathf.Clamp01(h.Normalized), 1f, 1f);
        panelHp.text = $"{DamageNumbers.Format(h.Current)} / {DamageNumbers.Format(h.maxHealth)}";
    }

    /// <summary>Sức mạnh: tổng hợp máu, Khí, công, giáp, chí mạng — số to dễ thấy tăng như SM của Ngọc Rồng.</summary>
    public static long Power(CharacterStats s)
    {
        if (s == null) return 0;
        float raw = s.MaxHealth * 2f + s.MaxEnergy * 2f + s.Attack * 30f + s.Defense * 8f;
        raw *= 1f + s.Get(StatType.CritChance) * (s.Get(StatType.CritDamage) - 1f);
        return (long)Mathf.Round(raw * 10f);
    }

    void RefreshPower()
    {
        var character = controller != null ? controller.Character : null;
        if (character == null) return;
        if (characterText == null)
        {
            var hud = FindAnyObjectByType<SkillHud>();
            characterText = hud != null ? hud.characterText : null;
            if (characterText == null) return;
            characterText.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
        characterText.text = $"{character.displayName} · Tuổi {character.zodiacName}   <color=#ffd34d><size=26>SM {DamageNumbers.Format(Power(stats))}</size></color>";
    }
}
