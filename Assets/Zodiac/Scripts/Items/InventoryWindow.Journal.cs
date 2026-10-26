using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Trang "Sổ ghi chép" của cửa sổ hành trang: bên trái danh sách quái đã gặp (chưa hạ con nào thì hiện bóng đen "???"),
/// bên phải ảnh, truyện, mẹo đánh và số đã hạ của quái đang chọn; dưới là mười hai con giáp (cấp, đã phá đảo chưa)
/// và tổng kết hành trình. Nội dung lấy từ <see cref="ZodiacJournal"/> và <see cref="PlayerProfile"/>.
/// </summary>
public partial class InventoryWindow
{
    GameObject journalPage;
    readonly Image[] beastIcons = new Image[ZodiacJournal.Beasts.Length];
    readonly Text[] beastNames = new Text[ZodiacJournal.Beasts.Length];
    readonly Image[] zodiacPortraits = new Image[12];
    readonly Image[] zodiacFrames = new Image[12];
    readonly Text[] zodiacLabels = new Text[12];
    Image beastArt;
    Text beastTitle, beastInfo, beastLore, beastTip, journalSummary;
    int beastSelected;

    static readonly Color Unknown = new Color(0.05f, 0.04f, 0.03f, 0.9f);

    /// <summary>Mở trang Sổ ghi chép.</summary>
    public void ShowJournal() => ShowTab(Page.Journal);

    void BuildJournal(Transform page)
    {
        var tl = new Vector2(0f, 1f);
        MakeText(page, "BeastHeader", "QUÁI VẬT ĐÃ GẶP", 28, Gold, tl, new Vector2(280f, -100f), new Vector2(460f, 40f), TextAnchor.MiddleCenter);
        for (int i = 0; i < ZodiacJournal.Beasts.Length; i++)
        {
            int index = i;
            var row = MakeButton(page, "", tl, new Vector2(280f, -150f - i * 58f), new Vector2(460f, 52f), () => SelectBeast(index));   // 10 loại vừa một cột
            row.GetComponent<Image>().color = SlotColor;
            beastIcons[i] = MakeImage(row.transform, "Icon", Color.white);
            beastIcons[i].preserveAspect = true;
            beastIcons[i].raycastTarget = false;
            Place(beastIcons[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(46f, 46f));
            beastNames[i] = row.GetComponentInChildren<Text>();
            beastNames[i].alignment = TextAnchor.MiddleLeft;
            beastNames[i].rectTransform.offsetMin = new Vector2(70f, 0f);
            beastNames[i].fontSize = 24;
        }

        beastArt = MakeImage(page, "BeastArt", Color.white);
        beastArt.preserveAspect = true;
        beastArt.raycastTarget = false;
        Place(beastArt.rectTransform, tl, new Vector2(690f, -250f), new Vector2(230f, 230f));
        beastTitle = MakeText(page, "BeastTitle", "", 40, Gold, tl, new Vector2(1130f, -150f), new Vector2(620f, 52f), TextAnchor.MiddleLeft);
        beastTitle.fontStyle = FontStyle.Bold;
        beastInfo = MakeText(page, "BeastInfo", "", 22, new Color(0.8f, 0.72f, 0.6f), tl, new Vector2(1130f, -200f), new Vector2(620f, 34f), TextAnchor.MiddleLeft);
        beastLore = MakeText(page, "BeastLore", "", 23, Ink, tl, new Vector2(1130f, -305f), new Vector2(620f, 170f), TextAnchor.UpperLeft);
        beastLore.horizontalOverflow = HorizontalWrapMode.Wrap;
        beastTip = MakeText(page, "BeastTip", "", 23, new Color(1f, 0.85f, 0.5f), tl, new Vector2(1045f, -450f), new Vector2(920f, 70f), TextAnchor.UpperLeft);
        beastTip.horizontalOverflow = HorizontalWrapMode.Wrap;

        MakeText(page, "ZodiacHeader", "MƯỜI HAI CON GIÁP", 28, Gold, tl, new Vector2(1045f, -530f), new Vector2(920f, 40f), TextAnchor.MiddleCenter);
        for (int i = 0; i < 12; i++)
        {
            var pos = new Vector2(690f + (i % 6) * 142f, -605f - (i / 6) * 118f);
            zodiacFrames[i] = MakeImage(page, "ZodiacFrame", new Color(0.4f, 0.32f, 0.25f));
            zodiacFrames[i].raycastTarget = false;
            Place(zodiacFrames[i].rectTransform, tl, pos, new Vector2(78f, 78f));
            var back = MakeImage(zodiacFrames[i].transform, "Back", SlotColor);
            back.raycastTarget = false;
            Stretch(back.rectTransform);
            back.rectTransform.offsetMin = new Vector2(4f, 4f);
            back.rectTransform.offsetMax = new Vector2(-4f, -4f);
            zodiacPortraits[i] = MakeImage(zodiacFrames[i].transform, "Portrait", Color.white);
            zodiacPortraits[i].preserveAspect = true;
            zodiacPortraits[i].raycastTarget = false;
            Stretch(zodiacPortraits[i].rectTransform);
            zodiacPortraits[i].rectTransform.offsetMin = new Vector2(6f, 6f);
            zodiacPortraits[i].rectTransform.offsetMax = new Vector2(-6f, -6f);
            zodiacLabels[i] = MakeText(page, "ZodiacLabel", "", 18, Ink, tl, pos + new Vector2(0f, -54f), new Vector2(140f, 26f), TextAnchor.MiddleCenter);
        }

        journalSummary = MakeText(page, "Summary", "", 22, new Color(0.8f, 0.72f, 0.6f), tl, new Vector2(280f, -770f), new Vector2(480f, 90f), TextAnchor.UpperCenter);
        journalSummary.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    void SelectBeast(int index)
    {
        beastSelected = index;
        RefreshJournal(PlayerProfile.Current);
    }

    void RefreshJournal(PlayerProfile profile)
    {
        if (journalPage == null || !journalPage.activeSelf) return;
        var beasts = ZodiacJournal.Beasts;
        for (int i = 0; i < beasts.Length; i++)
        {
            bool known = profile.KillsOf(beasts[i].id) > 0;
            var art = ZodiacJournal.Art(beasts[i]);
            beastIcons[i].enabled = art != null;
            beastIcons[i].sprite = art;
            beastIcons[i].color = known ? Color.white : Unknown;
            beastNames[i].text = known ? beasts[i].name : "???";
            beastNames[i].color = i == beastSelected ? Gold : Color.white;
        }

        var b = beasts[Mathf.Clamp(beastSelected, 0, beasts.Length - 1)];
        int kills = profile.KillsOf(b.id);
        var sprite = ZodiacJournal.Art(b);
        beastArt.enabled = sprite != null;
        beastArt.sprite = sprite;
        beastArt.color = kills > 0 ? Color.white : Unknown;
        if (kills > 0)
        {
            beastTitle.text = b.name;
            beastInfo.text = $"Gặp ở: {b.where}   ·   Đã hạ: {kills}";
            beastLore.text = b.lore;
            beastTip.text = "Mẹo: " + ZodiacInput.Format(b.tip);
        }
        else
        {
            beastTitle.text = "???";
            beastInfo.text = $"Gặp ở: {b.where}";
            beastLore.text = "Chưa hạ được con nào — hạ một con để ghi vào sổ.";
            beastTip.text = "";
        }

        var roster = ZodiacPlayerController.Current != null ? ZodiacPlayerController.Current.roster : null;
        int endings = 0;
        for (int i = 0; i < 12; i++)
        {
            var c = roster != null ? roster.Get(i) : null;
            zodiacFrames[i].gameObject.SetActive(c != null);
            zodiacLabels[i].gameObject.SetActive(c != null);
            if (c == null) continue;
            bool done = profile.endingsSeen.Contains(c.name);
            if (done) endings++;
            zodiacPortraits[i].sprite = c.sprite;
            zodiacFrames[i].color = done ? Gold : new Color(0.4f, 0.32f, 0.25f);
            zodiacLabels[i].text = $"{c.zodiacName} · Cấp {profile.characters.Find(x => x.character == c.name)?.level ?? 1}" + (done ? " ✓" : "");
            zodiacLabels[i].color = done ? Gold : Ink;
        }
        if (roster == null) endings = profile.endingsSeen.Count;

        int known2 = 0;
        foreach (var beast in beasts) if (profile.KillsOf(beast.id) > 0) known2++;
        journalSummary.text = $"Đã hạ {profile.TotalKills} quái · biết {known2}/{beasts.Length} loại\n" +
                              $"Phá đảo {endings}/12 con giáp" + (endings >= 12 ? " — đã thấy kết thúc thật" : "") +
                              $"\nMàn đã mở: {profile.unlockedLevels}" + (profile.towerBest > 0 ? $" · Tháp: {profile.towerBest} tầng" : "");
    }
}
