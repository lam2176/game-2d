using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hướng dẫn lần đầu ở màn 1: dải chữ giữa trên màn hình bày từng thao tác (đi, nhảy, đánh, lăn, chiêu…) theo phím đang dùng,
/// người chơi làm đúng thì sang bước kế. Chỉ chạy một lần (PlayerPrefs) — "Xem lại hướng dẫn" trong Cài đặt bật lại.
/// Ẩn khi đang có thoại / menu, đợi thoại vào khu xong mới bắt đầu.
/// </summary>
public class TutorialGuide : MonoBehaviour
{
    const string DoneKey = "zodiac.tutorialDone";

    public static bool Done => PlayerPrefs.GetInt(DoneKey, 0) == 1;

    /// <summary>Bật lại hướng dẫn cho lần vào màn 1 kế tiếp.</summary>
    public static void ResetDone()
    {
        PlayerPrefs.DeleteKey(DoneKey);
        PlayerPrefs.Save();
    }

    /// <summary>Gọi khi vào màn: chỉ màn 1, chưa xem hướng dẫn.</summary>
    public static void TryStart(ZodiacGameManager gm)
    {
        if (gm == null || gm.isHub || gm.levelIndex != 0 || Done || gm.player == null) return;
        gm.gameObject.AddComponent<TutorialGuide>().player = gm.player;
    }

    class Step
    {
        public string text;
        public float autoAfter;   // > 0: tự qua sau chừng ấy giây (bước chỉ để đọc)
    }

    static readonly Step[] Steps =
    {
        new Step { text = "{Move} đi  ·  {Lane} đổi làn — đi thử vài bước" },
        new Step { text = "{Jump} nhảy  ·  giữ {Sprint} để chạy nhanh" },
        new Step { text = "{Attack} đánh — bấm liền 3 lần thành chuỗi, đòn cuối quật ngã" },
        new Step { text = "{Dodge} lăn né — lăn xuyên qua đòn đánh của quái" },
        new Step { text = "{Skill} tung chiêu riêng — tốn Khí, đánh trúng quái để nạp Khí" },
        new Step { text = "Đầy Khí: {Ultimate} tuyệt kỹ  ·  quái choáng: {Interact} túm rồi ném", autoAfter = 7f },
        new Step { text = "Dọn sạch quái để sang khu kế  ·  {Previous} / {Next} uống bình  ·  I / Tab: hành trang", autoAfter = 7f },
    };

    ZodiacPlayerController player;
    PlayerMeleeAttack attack;
    PlayerDodge dodge;
    PlayerSkill skill;
    GameObject banner;
    Transform canvas;
    Text text, progress;
    Image back;
    int step = -1;
    float stepTime, doneAt = -1f, moved;
    int swings;
    Vector3 lastPos;
    bool hooked;

    void Start()
    {
        attack = player.GetComponent<PlayerMeleeAttack>();
        dodge = player.GetComponent<PlayerDodge>();
        skill = player.GetComponent<PlayerSkill>();
        player.Jumped += OnJumped;
        if (attack != null) attack.Swung += OnSwung;
        if (dodge != null) dodge.Dodged += OnDodged;
        if (skill != null)
        {
            skill.Used += OnSkill;
            skill.UltimateUsed += OnUltimate;
        }
        hooked = true;
        Build();
        banner.SetActive(false);
        ZodiacInput.Changed += RefreshText;
        lastPos = player.transform.position;
    }

    void OnDestroy()
    {
        ZodiacInput.Changed -= RefreshText;
        if (!hooked || player == null) return;
        player.Jumped -= OnJumped;
        if (attack != null) attack.Swung -= OnSwung;
        if (dodge != null) dodge.Dodged -= OnDodged;
        if (skill != null)
        {
            skill.Used -= OnSkill;
            skill.UltimateUsed -= OnUltimate;
        }
    }

    void OnJumped(bool _) { if (step == 1) Complete(); }
    void OnSwung(int _, PlayerMeleeAttack.Kind __) { if (step == 2 && ++swings >= 3) Complete(); }
    void OnDodged(float _) { if (step == 3) Complete(); }
    void OnSkill(SkillType _) { if (step == 4) Complete(); }
    void OnUltimate() { if (step == 5) Complete(); }

    bool Blocked
    {
        get
        {
            var gm = ZodiacGameManager.Instance;
            return StoryUi.IsOpen || PauseMenu.IsOpen || InventoryWindow.IsOpen || SettingsWindow.IsOpen ||
                   (gm != null && gm.Current != ZodiacGameManager.State.Playing) || !player.ControlEnabled;
        }
    }

    void Update()
    {
        if (player == null) return;
        bool blocked = Blocked;
        if (banner.activeSelf == blocked) banner.SetActive(!blocked && step >= 0);
        if (blocked) return;
        if (step < 0)
        {
            Show(0);
            return;
        }

        // Đánh dấu xong: dải chữ xanh một lúc rồi sang bước kế
        if (doneAt >= 0f)
        {
            float k = (Time.time - doneAt) / 0.7f;
            back.color = Color.Lerp(new Color(0.15f, 0.45f, 0.2f, 0.9f), new Color(0.1f, 0.07f, 0.05f, 0.85f), k);
            if (k >= 1f) Show(step + 1);
            return;
        }

        var pos = player.transform.position;
        if (step == 0)
        {
            moved += new Vector2(pos.x - lastPos.x, pos.z - lastPos.z).magnitude;
            if (moved >= 3f) Complete();
        }
        lastPos = pos;
        var s = Steps[step];
        if (s.autoAfter > 0f && Time.time - stepTime >= s.autoAfter) Complete();
        float pulse = 1f + 0.03f * Mathf.Sin(Time.time * 4f);
        banner.transform.localScale = new Vector3(pulse, pulse, 1f);
    }

    void Show(int index)
    {
        doneAt = -1f;
        if (index >= Steps.Length)
        {
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
            Destroy(canvas.gameObject);
            Destroy(this);
            return;
        }
        step = index;
        stepTime = Time.time;
        banner.SetActive(true);
        banner.transform.localScale = Vector3.one;
        back.color = new Color(0.1f, 0.07f, 0.05f, 0.85f);
        RefreshText();
    }

    void Complete()
    {
        if (doneAt >= 0f || step < 0) return;
        doneAt = Time.time;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.lantern, 0.45f, 0f);
        if (text != null) text.text = "✓  " + ZodiacInput.Format(Steps[step].text);
    }

    void RefreshText()
    {
        if (text == null || step < 0 || step >= Steps.Length || doneAt >= 0f) return;
        text.text = ZodiacInput.Format(Steps[step].text);
        progress.text = $"Hướng dẫn {step + 1} / {Steps.Length}";
    }

    void Build()
    {
        canvas = ZodiacUi.Canvas(transform, "TutorialCanvas", 300);
        back = ZodiacUi.MakeImage(canvas, "Banner", new Color(0.1f, 0.07f, 0.05f, 0.85f));
        back.raycastTarget = false;
        ZodiacUi.Place(back.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1240f, 96f));
        back.gameObject.AddComponent<Outline>().effectColor = ZodiacUi.Gold;
        banner = back.gameObject;
        text = ZodiacUi.MakeText(back.transform, "", 32, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1200f, 50f));
        progress = ZodiacUi.MakeText(back.transform, "", 18, ZodiacUi.Gold, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(400f, 24f));
    }
}
