using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Trang "Nhiệm vụ" của cửa sổ hành trang (phím J): trên là ba nhóm Cốt truyện · Săn quái · Rèn luyện (kèm số chờ nhận thưởng),
/// bên trái danh sách nhiệm vụ của nhóm (tên, thanh tiến độ, trạng thái — xong mà chưa nhận xếp lên đầu), bên phải chi tiết,
/// phần thưởng và nút Nhận thưởng / Nhận tất cả. Dữ liệu và cách tính tiến độ ở <see cref="ZodiacQuests"/>.
/// </summary>
public partial class InventoryWindow
{
    class QuestRow
    {
        public Button button;
        public Text title, status;
        public Image fill;
    }

    static readonly Color Done = new Color(0.62f, 0.89f, 0.6f);

    GameObject questPage;
    readonly Text[] questCategoryLabels = new Text[ZodiacQuests.Categories.Length];
    readonly Image[] questCategoryImages = new Image[ZodiacQuests.Categories.Length];
    readonly List<QuestRow> questRows = new List<QuestRow>();
    readonly List<ZodiacQuests.Quest> questShown = new List<ZodiacQuests.Quest>();
    Text questTitle, questState, questText, questReward, questSummary, questProgressText;
    Image questFill;
    Button claimButton, claimAllButton;
    int questCategory;
    string questSelected = "";

    void BuildQuests(Transform page)
    {
        var tl = new Vector2(0f, 1f);
        for (int i = 0; i < ZodiacQuests.Categories.Length; i++)
        {
            int index = i;
            var b = MakeButton(page, ZodiacQuests.Categories[i], tl, new Vector2(170f + i * 245f, -115f), new Vector2(230f, 56f), () => SelectQuestCategory(index));
            questCategoryImages[i] = b.GetComponent<Image>();
            questCategoryLabels[i] = b.GetComponentInChildren<Text>();
        }

        for (int i = 0; i < ZodiacQuests.PerCategory; i++)
        {
            int index = i;
            var b = MakeButton(page, "", tl, new Vector2(415f, -185f - i * 60f), new Vector2(720f, 54f), () => SelectQuestRow(index));
            b.GetComponent<Image>().color = SlotColor;
            var title = b.GetComponentInChildren<Text>();
            title.alignment = TextAnchor.MiddleLeft;
            title.rectTransform.offsetMin = new Vector2(18f, 6f);
            title.fontSize = 23;
            var status = MakeText(b.transform, "Status", "", 21, Ink, new Vector2(1f, 0.5f), new Vector2(-14f, 3f), new Vector2(220f, 30f), TextAnchor.MiddleRight);
            status.rectTransform.pivot = new Vector2(1f, 0.5f);
            var back = MakeImage(b.transform, "Bar", new Color(0.05f, 0.04f, 0.03f, 1f));
            back.raycastTarget = false;
            Place(back.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(690f, 5f));
            var fill = MakeImage(back.transform, "Fill", Gold);
            fill.raycastTarget = false;
            Stretch(fill.rectTransform);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            questRows.Add(new QuestRow { button = b, title = title, status = status, fill = fill });
        }

        float x = 1160f;
        questTitle = MakeText(page, "QuestTitle", "", 36, Gold, tl, new Vector2(x, -150f), new Vector2(640f, 50f), TextAnchor.MiddleLeft);
        questTitle.fontStyle = FontStyle.Bold;
        questState = MakeText(page, "QuestState", "", 22, new Color(0.8f, 0.72f, 0.6f), tl, new Vector2(x, -195f), new Vector2(640f, 32f), TextAnchor.MiddleLeft);
        questText = MakeText(page, "QuestText", "", 24, Ink, tl, new Vector2(x, -280f), new Vector2(640f, 120f), TextAnchor.UpperLeft);
        questText.horizontalOverflow = HorizontalWrapMode.Wrap;

        var barBack = MakeImage(page, "ProgressBack", new Color(0.05f, 0.04f, 0.03f, 1f));
        barBack.raycastTarget = false;
        Place(barBack.rectTransform, tl, new Vector2(x, -385f), new Vector2(640f, 18f));
        questFill = MakeImage(barBack.transform, "Fill", Gold);
        questFill.raycastTarget = false;
        Stretch(questFill.rectTransform);
        questFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        questProgressText = MakeText(page, "ProgressText", "", 22, Ink, tl, new Vector2(x, -415f), new Vector2(640f, 30f), TextAnchor.MiddleRight);

        MakeText(page, "RewardHeader", "PHẦN THƯỞNG", 26, Gold, tl, new Vector2(x, -470f), new Vector2(640f, 36f), TextAnchor.MiddleLeft);
        questReward = MakeText(page, "Reward", "", 24, Ink, tl, new Vector2(x, -515f), new Vector2(640f, 60f), TextAnchor.UpperLeft);
        questReward.horizontalOverflow = HorizontalWrapMode.Wrap;
        claimButton = MakeButton(page, "Nhận thưởng", tl, new Vector2(x - 165f, -610f), new Vector2(300f, 62f), ClaimSelected);
        claimAllButton = MakeButton(page, "Nhận tất cả", tl, new Vector2(x + 165f, -610f), new Vector2(300f, 62f), ClaimAll);

        questSummary = MakeText(page, "QuestSummary", "", 22, new Color(0.8f, 0.72f, 0.6f), tl, new Vector2(x, -700f), new Vector2(640f, 60f), TextAnchor.UpperLeft);
        questSummary.horizontalOverflow = HorizontalWrapMode.Wrap;
        MakeText(page, "Hint", "Tiến độ tự cập nhật khi chơi · xong thì về đây nhận thưởng · J / Esc: đóng", 22, new Color(0.8f, 0.72f, 0.6f),
                 new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1200f, 34f), TextAnchor.MiddleCenter);
    }

    void SelectQuestCategory(int index)
    {
        questCategory = index;
        questSelected = "";
        RefreshQuests(PlayerProfile.Current);
    }

    void SelectQuestRow(int index)
    {
        if (index >= questShown.Count) return;
        questSelected = questShown[index].id;
        RefreshQuests(PlayerProfile.Current);
    }

    ZodiacQuests.Quest SelectedQuest => questShown.Find(q => q.id == questSelected);

    void ClaimSelected()
    {
        var q = SelectedQuest;
        if (q == null) return;
        bool ok = ZodiacQuests.Claim(q, PlayerProfile.Current, CharacterKey, out string message);
        if (!string.IsNullOrEmpty(message)) PickupToast.Show(message, ok ? Gold : new Color(1f, 0.5f, 0.4f));
        if (ok) PlayCoin();
        Refresh();
    }

    void ClaimAll()
    {
        var p = PlayerProfile.Current;
        int claimed = 0;
        string fail = "";
        foreach (var q in ZodiacQuests.All)
        {
            if (ZodiacQuests.StateOf(q, p) != ZodiacQuests.State.Claimable) continue;
            if (ZodiacQuests.Claim(q, p, CharacterKey, out string message)) claimed++;
            else fail = message;
        }
        if (claimed > 0)
        {
            PickupToast.Show($"Đã nhận thưởng {claimed} nhiệm vụ", Gold);
            PlayCoin();
        }
        if (!string.IsNullOrEmpty(fail)) PickupToast.Show(fail, new Color(1f, 0.5f, 0.4f));
        Refresh();
    }

    static void PlayCoin()
    {
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.coin, 0.7f, 0.05f);
    }

    void RefreshQuests(PlayerProfile profile)
    {
        if (questPage == null || !questPage.activeSelf) return;
        for (int i = 0; i < questCategoryLabels.Length; i++)
        {
            int ready = ZodiacQuests.ClaimableCount(profile, i);
            bool on = i == questCategory;
            questCategoryImages[i].color = on ? Gold : TabRed;
            questCategoryLabels[i].color = on ? TabInk : Color.white;
            questCategoryLabels[i].text = ZodiacQuests.Categories[i] + (ready > 0 ? $" <color={(on ? "#2f6b2a" : "#9fe39a")}>({ready})</color>" : "");
        }

        questShown.Clear();
        questShown.AddRange(ZodiacQuests.InCategory(questCategory, profile));
        if (SelectedQuest == null && questShown.Count > 0) questSelected = questShown[0].id;

        for (int i = 0; i < questRows.Count; i++)
        {
            var row = questRows[i];
            bool used = i < questShown.Count;
            row.button.gameObject.SetActive(used);
            if (!used) continue;
            var q = questShown[i];
            var state = ZodiacQuests.StateOf(q, profile);
            int n = ZodiacQuests.Progress(q, profile);
            row.title.text = q.title;
            row.title.color = q.id == questSelected ? Gold : state == ZodiacQuests.State.Claimed ? new Color(0.6f, 0.55f, 0.48f) : Color.white;
            row.status.text = state switch
            {
                ZodiacQuests.State.Claimable => "Nhận thưởng!",
                ZodiacQuests.State.Claimed => "✓ Xong",
                _ => q.target > 1 ? $"{n}/{q.target}" : "Đang làm",
            };
            row.status.color = state == ZodiacQuests.State.Claimable ? Done : state == ZodiacQuests.State.Claimed ? new Color(0.6f, 0.55f, 0.48f) : Ink;
            row.fill.rectTransform.localScale = new Vector3((float)n / q.target, 1f, 1f);
            row.fill.color = state == ZodiacQuests.State.InProgress ? Gold : Done;
        }

        var sel = SelectedQuest;
        if (sel != null)
        {
            var state = ZodiacQuests.StateOf(sel, profile);
            int n = ZodiacQuests.Progress(sel, profile);
            questTitle.text = sel.title;
            questState.text = ZodiacQuests.Categories[sel.category] + "   ·   " + state switch
            {
                ZodiacQuests.State.Claimable => "<color=#9fe39a>Đã xong — chờ nhận thưởng</color>",
                ZodiacQuests.State.Claimed => "Đã nhận thưởng",
                _ => "Đang làm",
            };
            questText.text = sel.text;
            questFill.rectTransform.localScale = new Vector3((float)n / sel.target, 1f, 1f);
            questFill.color = state == ZodiacQuests.State.InProgress ? Gold : Done;
            questProgressText.text = $"{n} / {sel.target}";
            questReward.text = ZodiacQuests.RewardText(sel) + (sel.xp > 0 ? "\n<size=18>XP cộng cho con giáp đang chơi</size>" : "");
            claimButton.gameObject.SetActive(state == ZodiacQuests.State.Claimable);
        }
        else
        {
            questTitle.text = questState.text = questText.text = questReward.text = questProgressText.text = "";
            claimButton.gameObject.SetActive(false);
        }
        claimAllButton.gameObject.SetActive(ZodiacQuests.ClaimableCount(profile) > 1);

        int done = 0;
        foreach (var q in ZodiacQuests.All) if (profile.questsClaimed.Contains(q.id)) done++;
        questSummary.text = $"Đã hoàn thành {done}/{ZodiacQuests.All.Length} nhiệm vụ";
    }
}
