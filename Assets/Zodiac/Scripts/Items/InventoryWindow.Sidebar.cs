using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Thanh bên trái của hành trang:
///   • Trong cửa sổ: cột tab dọc bám mép trái khung (Hành trang · Tu luyện · Nhiệm vụ · Sổ ghi chép · Đóng), tab đang mở màu vàng.
///   • Lúc chơi: tay nắm "»" ở mép trái màn hình — bấm để kéo ra / thu vào cột nút mở thẳng từng trang. Có chấm xanh khi có
///     nhiệm vụ chờ nhận thưởng. Trạng thái mở / thu nhớ trong PlayerPrefs. Ẩn khi đang mở cửa sổ khác, tạm dừng, xem truyện.
/// </summary>
public partial class InventoryWindow
{
    public enum Page { Bag, Training, Quests, Journal }

    static readonly string[] PageNames = { "Hành trang", "Tu luyện", "Nhiệm vụ", "Sổ ghi chép" };
    static readonly string[] PageKeys = { "I · Tab", "", "J", "" };
    static readonly Color TabRed = new Color(0.7f, 0.15f, 0.12f);
    static readonly Color TabInk = new Color(0.16f, 0.1f, 0.05f);
    const string SidebarPref = "zodiac.sidebarOpen";

    Page page = Page.Bag;
    Text pageTitle;
    readonly Image[] tabImages = new Image[4];
    readonly Text[] tabLabels = new Text[4];
    GameObject hudSidebar, hudSidebarPanel;
    Text hudHandleLabel;
    readonly Text[] hudLabels = new Text[4];
    bool sidebarOpen;

    /// <summary>Mở hành trang ở một trang bất kỳ (thanh bên, phím J, trạm trong làng).</summary>
    public static void OpenPage(Page target)
    {
        if (instance == null || IsOpen) return;
        instance.OpenAt(target);
    }

    void OpenAt(Page target)
    {
        Open();
        if (IsOpen) ShowTab(target);
    }

    GameObject PageObject(Page p) => p switch
    {
        Page.Training => trainPage,
        Page.Quests => questPage,
        Page.Journal => journalPage,
        _ => bagPage,
    };

    /// <summary>Đổi trang đang mở và tô lại cột tab.</summary>
    public void ShowTab(Page target)
    {
        bool changed = target != page;
        page = target;
        bagPage.SetActive(page == Page.Bag);
        trainPage.SetActive(page == Page.Training);
        questPage.SetActive(page == Page.Quests);
        journalPage.SetActive(page == Page.Journal);
        selected = -1;
        for (int i = 0; i < tabImages.Length; i++)
        {
            bool on = i == (int)page;
            tabImages[i].color = on ? Gold : TabRed;
            tabLabels[i].color = on ? TabInk : Color.white;
        }
        pageTitle.text = PageNames[(int)page].ToUpperInvariant();
        var res = ZodiacResources.Get;
        if (changed && res != null && (page == Page.Quests || page == Page.Journal)) ZodiacAudio.Play(res.page, 0.6f, 0.08f);
        Refresh();
        SelectFirst();
    }

    // ───────── Cột tab trong cửa sổ ─────────

    void BuildTabs(Transform panel)
    {
        pageTitle = MakeText(panel, "PageTitle", "", 34, Gold, new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(800f, 50f), TextAnchor.MiddleCenter);
        pageTitle.fontStyle = FontStyle.Bold;

        // Khung rộng 1560 giữa màn 1920 → còn 180 bên trái: cột tab nằm ngay ngoài mép khung
        var strip = MakeImage(panel, "TabStrip", new Color(0.08f, 0.06f, 0.04f, 0.96f));
        strip.raycastTarget = false;
        Place(strip.rectTransform, new Vector2(0f, 1f), new Vector2(-92f, -230f), new Vector2(180f, 460f));
        strip.gameObject.AddComponent<Outline>().effectColor = Gold;
        for (int i = 0; i < PageNames.Length; i++)
        {
            var target = (Page)i;
            string label = string.IsNullOrEmpty(PageKeys[i]) ? PageNames[i] : $"{PageNames[i]}\n<size=16>{PageKeys[i]}</size>";
            var b = MakeButton(panel, label, new Vector2(0f, 1f), new Vector2(-92f, -50f - i * 82f), new Vector2(160f, 72f), () => ShowTab(target));
            b.name = "Tab " + PageNames[i];
            tabImages[i] = b.GetComponent<Image>();
            tabLabels[i] = b.GetComponentInChildren<Text>();
            tabLabels[i].fontSize = 22;
        }
        var close = MakeButton(panel, "Đóng  ✕", new Vector2(0f, 1f), new Vector2(-92f, -50f - 4 * 82f - 16f), new Vector2(160f, 56f), Close);
        close.GetComponent<Image>().color = SlotColor;
    }

    /// <summary>Số nhiệm vụ chờ nhận thưởng lên tab Nhiệm vụ và thanh bên lúc chơi.</summary>
    void RefreshBadges(PlayerProfile profile)
    {
        int ready = ZodiacQuests.ClaimableCount(profile);
        string badge = ready > 0 ? $" <color=#9fe39a>({ready})</color>" : "";
        if (tabLabels[(int)Page.Quests] != null)
            tabLabels[(int)Page.Quests].text = $"{PageNames[(int)Page.Quests]}{badge}\n<size=16>{PageKeys[(int)Page.Quests]}</size>";
        if (hudLabels[(int)Page.Quests] != null) hudLabels[(int)Page.Quests].text = HudLabel(Page.Quests) + badge;
        if (hudHandleLabel != null)
            hudHandleLabel.text = (sidebarOpen ? "«" : "»") + (ready > 0 ? "\n<color=#9fe39a>●</color>" : "");
    }

    // ───────── Thanh bên lúc chơi ─────────

    static string HudLabel(Page p) =>
        string.IsNullOrEmpty(PageKeys[(int)p]) ? PageNames[(int)p] : $"{PageNames[(int)p]}  <size=16><color=#f2c75a>{PageKeys[(int)p]}</color></size>";

    void BuildHudSidebar(Transform root)
    {
        hudSidebar = new GameObject("HudSidebar", typeof(RectTransform));
        hudSidebar.transform.SetParent(root, false);
        Stretch((RectTransform)hudSidebar.transform);
        var bar = hudSidebar.transform;

        var handle = MakeButton(bar, "»", new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(44f, 130f), ToggleSidebar);
        handle.name = "SidebarHandle";
        handle.GetComponent<Image>().color = new Color(0.11f, 0.08f, 0.06f, 0.85f);
        handle.gameObject.AddComponent<Outline>().effectColor = Gold;
        hudHandleLabel = handle.GetComponentInChildren<Text>();
        hudHandleLabel.fontSize = 30;
        hudHandleLabel.color = Gold;
        NoNavigation(handle);

        var panel = MakeImage(bar, "SidebarPanel", new Color(0.11f, 0.08f, 0.06f, 0.9f));
        Place(panel.rectTransform, new Vector2(0f, 0.5f), new Vector2(180f, 0f), new Vector2(260f, 330f));
        panel.gameObject.AddComponent<Outline>().effectColor = Gold;
        hudSidebarPanel = panel.gameObject;
        for (int i = 0; i < PageNames.Length; i++)
        {
            var target = (Page)i;
            var b = MakeButton(panel.transform, HudLabel(target), new Vector2(0.5f, 1f), new Vector2(0f, -45f - i * 80f), new Vector2(230f, 66f),
                               () => { Deselect(); OpenPage(target); });
            hudLabels[i] = b.GetComponentInChildren<Text>();
            hudLabels[i].fontSize = 22;
            NoNavigation(b);
        }

        sidebarOpen = PlayerPrefs.GetInt(SidebarPref, 0) == 1;
        hudSidebarPanel.SetActive(sidebarOpen);
    }

    void ToggleSidebar()
    {
        Deselect();
        sidebarOpen = !sidebarOpen;
        hudSidebarPanel.SetActive(sidebarOpen);
        PlayerPrefs.SetInt(SidebarPref, sidebarOpen ? 1 : 0);
        RefreshBadges(PlayerProfile.Current);
    }

    /// <summary>Chỉ hiện thanh bên khi đang chơi thật (không mở cửa sổ / menu / truyện nào).</summary>
    void UpdateHudSidebar()
    {
        if (hudSidebar == null) return;
        var gm = ZodiacGameManager.Instance;
        bool show = !IsOpen && !PauseMenu.IsOpen && !VillageWindow.IsOpen && !StoryUi.IsOpen && !SettingsWindow.IsOpen &&
                    (gm == null || gm.Current == ZodiacGameManager.State.Playing);
        if (hudSidebar.activeSelf != show) hudSidebar.SetActive(show);
    }

    /// <summary>Bỏ chọn nút vừa bấm — không thì phím Nhảy (Space) sẽ "bấm" lại nút đó.</summary>
    static void Deselect()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    static void NoNavigation(Button b)
    {
        var nav = b.navigation;
        nav.mode = Navigation.Mode.None;
        b.navigation = nav;
    }
}
