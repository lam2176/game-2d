using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Phần tu luyện của cửa sổ hành trang: trang "Tu luyện" (cấp, XP, cộng điểm Sinh lực / Nội khí / Sức mạnh / Thân pháp,
/// luyện 3 mảnh Huyết / Khí Đan thành viên Đan) và nhãn cấp + thanh XP luôn hiện ở góc dưới trái.
/// Tiến trình riêng từng con giáp (<see cref="CharacterProgress"/>).
/// </summary>
public partial class InventoryWindow
{
    Text levelHudText, trainTitle, trainXp, trainPoints, pillText;
    Image levelHudFill, trainXpFill;
    readonly Text[] attributeValues = new Text[4];
    readonly Button[] attributeButtons = new Button[4];
    Button bloodButton, energyButton;

    static string CharacterKey
    {
        get
        {
            var player = ZodiacPlayerController.Current;
            return player != null && player.Character != null ? player.Character.name : "";
        }
    }

    void ShowPage(bool training)
    {
        bagPage.SetActive(!training);
        trainPage.SetActive(training);
        selected = -1;
        Refresh();
    }

    // ───────── Nhãn cấp + thanh XP (luôn hiện) ─────────

    void BuildLevelHud(Transform root)
    {
        levelHudText = MakeText(root, "LevelHud", "", 26, Gold, new Vector2(0f, 0f), new Vector2(30f, 290f), new Vector2(300f, 34f), TextAnchor.MiddleLeft);
        levelHudText.rectTransform.pivot = new Vector2(0f, 0.5f);
        var back = MakeImage(root, "XpBack", new Color(0.1f, 0.08f, 0.06f, 0.85f));
        back.raycastTarget = false;
        Place(back.rectTransform, new Vector2(0f, 0f), new Vector2(130f, 262f), new Vector2(200f, 10f));
        levelHudFill = MakeImage(back.transform, "Fill", Gold);
        levelHudFill.raycastTarget = false;
        Stretch(levelHudFill.rectTransform);
        levelHudFill.rectTransform.pivot = new Vector2(0f, 0.5f);
    }

    void RefreshLevelHud(PlayerProfile profile)
    {
        if (levelHudText == null || string.IsNullOrEmpty(CharacterKey)) return;
        var p = profile.Progress(CharacterKey);
        levelHudText.text = $"Cấp {p.level}" + (p.points > 0 ? $"  <color=#9fe39a>(+{p.points} điểm)</color>" : "");
        float k = p.XpToNext > 0 ? Mathf.Clamp01((float)p.xp / p.XpToNext) : 1f;
        levelHudFill.rectTransform.localScale = new Vector3(k, 1f, 1f);
    }

    // ───────── Trang Tu luyện ─────────

    void BuildTraining(Transform page)
    {
        trainTitle = MakeText(page, "Title", "", 40, Gold, new Vector2(0.5f, 1f), new Vector2(0f, -135f), new Vector2(1200f, 56f), TextAnchor.MiddleCenter);
        trainXp = MakeText(page, "Xp", "", 24, Ink, new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(800f, 32f), TextAnchor.MiddleCenter);
        var xpBack = MakeImage(page, "XpBack", new Color(0.05f, 0.04f, 0.03f, 1f));
        xpBack.raycastTarget = false;
        Place(xpBack.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -215f), new Vector2(600f, 14f));
        trainXpFill = MakeImage(xpBack.transform, "Fill", Gold);
        trainXpFill.raycastTarget = false;
        Stretch(trainXpFill.rectTransform);
        trainXpFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        trainPoints = MakeText(page, "Points", "", 28, new Color(0.62f, 0.9f, 0.6f), new Vector2(0f, 1f), new Vector2(380f, -270f), new Vector2(600f, 40f), TextAnchor.MiddleLeft);

        for (int i = 0; i < 4; i++)
        {
            int attribute = i;
            float y = -340f - i * 90f;
            MakeText(page, "Name", Progression.AttributeNames[i], 30, Ink, new Vector2(0f, 1f), new Vector2(180f, y), new Vector2(240f, 40f), TextAnchor.MiddleLeft);
            attributeValues[i] = MakeText(page, "Value", "", 30, Gold, new Vector2(0f, 1f), new Vector2(330f, y), new Vector2(80f, 40f), TextAnchor.MiddleCenter);
            MakeText(page, "Help", Progression.AttributeHelp[i], 22, new Color(0.8f, 0.72f, 0.6f), new Vector2(0f, 1f), new Vector2(560f, y), new Vector2(360f, 34f), TextAnchor.MiddleLeft);
            attributeButtons[i] = MakeButton(page, "+", new Vector2(0f, 1f), new Vector2(800f, y), new Vector2(64f, 56f), () => Spend(attribute));
        }

        MakeText(page, "PillTitle", "Luyện Đan", 32, Gold, new Vector2(0f, 1f), new Vector2(1180f, -320f), new Vector2(500f, 44f), TextAnchor.MiddleCenter);
        pillText = MakeText(page, "Pills", "", 22, Ink, new Vector2(0f, 1f), new Vector2(1180f, -420f), new Vector2(500f, 130f), TextAnchor.MiddleCenter);
        pillText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bloodButton = MakeButton(page, "Luyện Huyết Đan", new Vector2(0f, 1f), new Vector2(1180f, -520f), new Vector2(320f, 56f), () => Refine(true));
        energyButton = MakeButton(page, "Luyện Khí Đan", new Vector2(0f, 1f), new Vector2(1180f, -590f), new Vector2(320f, 56f), () => Refine(false));

        MakeText(page, "Hint", "Mỗi cấp được 3 điểm · cấp và Đan riêng cho từng con giáp, đồ và tiền dùng chung", 22,
                 new Color(0.8f, 0.72f, 0.6f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1300f, 34f), TextAnchor.MiddleCenter);
    }

    void RefreshTraining(PlayerProfile profile)
    {
        if (trainPage == null || !trainPage.activeSelf || string.IsNullOrEmpty(CharacterKey)) return;
        var p = profile.Progress(CharacterKey);
        var c = ZodiacPlayerController.Current.Character;
        trainTitle.text = $"{c.displayName} — Cấp {p.level}" + (p.level >= CharacterProgress.MaxLevel ? " (tối đa)" : "");
        trainXp.text = p.XpToNext > 0 ? $"XP {p.xp} / {p.XpToNext}" : "Đã đạt cấp tối đa";
        trainXpFill.rectTransform.localScale = new Vector3(p.XpToNext > 0 ? Mathf.Clamp01((float)p.xp / p.XpToNext) : 1f, 1f, 1f);
        trainPoints.text = $"Điểm chưa cộng: {p.points}";
        int[] values = { p.vitality, p.spirit, p.strength, p.agility };
        for (int i = 0; i < 4; i++)
        {
            attributeValues[i].text = values[i].ToString();
            attributeButtons[i].interactable = p.points > 0;
        }
        int bloodShards = profile.CountOf("manh_huyet_dan"), energyShards = profile.CountOf("manh_khi_dan");
        pillText.text =
            $"Mảnh Huyết Đan: <b>{bloodShards}</b> / {PlayerProfile.ShardsPerPill}   ·   đã luyện {p.bloodPills} viên (+{CharacterProgress.PillBonus * p.bloodPills} máu)\n" +
            $"Mảnh Khí Đan: <b>{energyShards}</b> / {PlayerProfile.ShardsPerPill}   ·   đã luyện {p.energyPills} viên (+{CharacterProgress.PillBonus * p.energyPills} Khí)\n" +
            "<size=20>Mảnh rơi từ boss, Thạch Sư, Lính Khiên…</size>";
        bloodButton.interactable = bloodShards >= PlayerProfile.ShardsPerPill;
        energyButton.interactable = energyShards >= PlayerProfile.ShardsPerPill;
    }

    void Spend(int attribute)
    {
        if (!PlayerProfile.Current.SpendPoint(CharacterKey, attribute)) return;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.coin, 0.5f, 0.05f);
    }

    void Refine(bool blood)
    {
        if (!PlayerProfile.Current.RefinePill(CharacterKey, blood)) return;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.lantern, 0.8f, 0f);
        PickupToast.Show(blood ? "Luyện thành Huyết Đan: +20 máu tối đa" : "Luyện thành Khí Đan: +20 Khí tối đa", new Color(1f, 0.85f, 0.3f));
    }
}
